"""Authenticated private GitHub release proxy. Run behind an HTTPS reverse proxy.

HAWK_DOWNLOAD_CONFIG: JSON {repository, manifest, assets: {path: integer asset ID},
users: {user ID: SHA256 of launcher token}, cache, publicUrl}.
HAWK_GITHUB_TOKEN: server-only Contents:read credential for the one private repo.
Every request rereads users so deletion/replacement immediately revokes future access.
"""
import hashlib
import hmac
from http.server import BaseHTTPRequestHandler
from bounded_http import ThreadingHTTPServer
import json
import os
from pathlib import Path
import re
import threading
import zipfile
import urllib.request
import urllib.parse
from http.cookies import SimpleCookie
import discord_login

lock = threading.Lock()

def github_credential():
    # Production systemd copies this publisher-only credential into a protected
    # per-service directory. Environment fallback is for isolated acceptance tests.
    directory = os.environ.get('CREDENTIALS_DIRECTORY')
    if directory:
        return (Path(directory) / 'github-read').read_text(encoding='utf-8-sig').strip()
    return os.environ['HAWK_GITHUB_TOKEN']

def config():
    return json.loads(Path(os.environ['HAWK_DOWNLOAD_CONFIG']).read_text())

def fetch(c, asset_id, expected, file):
    """Fetch one allowlisted immutable release asset; repository auth stays here."""
    if file.exists():
        return
    repo = c['repository']
    if not re.fullmatch(r'[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+', repo) or not isinstance(asset_id, int) or asset_id < 1:
        raise ValueError('Invalid release repository or asset')
    request = urllib.request.Request('https://api.github.com/repos/' + repo + '/releases/assets/' + str(asset_id), headers={'Authorization': 'Bearer ' + github_credential(), 'Accept': 'application/octet-stream', 'User-Agent': 'JasonHawkSkater-broker', 'X-GitHub-Api-Version': '2022-11-28'})
    partial = file.with_suffix('.partial')
    sha, size = hashlib.sha256(), 0
    with urllib.request.build_opener(Redirect()).open(request, timeout=60) as upstream, partial.open('wb') as out:
        while block := upstream.read(1024 * 1024):
            size += len(block)
            if size > expected['size']:
                raise ValueError('Release archive exceeds manifest size')
            sha.update(block); out.write(block)
    if size != expected['size'] or sha.hexdigest() != expected['sha256']:
        partial.unlink(missing_ok=True)
        raise ValueError('Release archive checksum mismatch')
    os.replace(partial, file)

def archive_file(c, m, entry, cache, file):
    """One private release ZIP can serve thousands of manifest files, no extractall."""
    archive = c['archive']
    bundle = cache / ('archive-' + archive['sha256'])
    fetch(c, archive['asset_id'], archive, bundle)
    allowed = {e['path'] for e in m['files']} | {'manifest.json'}
    seen = set()
    with zipfile.ZipFile(bundle) as z:
        for item in z.infolist():
            if item.is_dir():
                continue
            name = item.filename
            if name not in allowed or name.casefold() in seen or '\\' in name or ':' in name or name.startswith('/') or '..' in name.split('/') or (item.external_attr >> 16) & 0o170000 == 0o120000:
                raise ValueError('Unsafe release archive')
            seen.add(name.casefold())
        info = z.getinfo(entry['path'])
        if info.file_size != entry['size']:
            raise ValueError('Archive file exceeds manifest size')
        partial = file.with_suffix('.partial')
        sha, size = hashlib.sha256(), 0
        with z.open(info) as inp, partial.open('wb') as out:
            while block := inp.read(1024 * 1024):
                size += len(block)
                if size > entry['size']:
                    raise ValueError('Archive file exceeds manifest size')
                sha.update(block); out.write(block)
        if size != entry['size'] or sha.hexdigest() != entry['sha256']:
            partial.unlink(missing_ok=True)
            raise ValueError('Archive file checksum mismatch')
        os.replace(partial, file)

class Redirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, request, fp, code, msg, headers, url):
        if not url.startswith('https://'):
            raise ValueError('Insecure storage redirect')
        redirected = super().redirect_request(request, fp, code, msg, headers, url)
        redirected.remove_header('Authorization')
        return redirected

class Handler(BaseHTTPRequestHandler):
    def log_message(self, *_):
        pass

    def authorized(self, c):
        if c.get('publicDownloads') is True:
            return True
        value = self.headers.get('Authorization', '')
        if not value.startswith('Bearer ') or not value[7:]:
            return False
        sha = hashlib.sha256(value[7:].encode()).hexdigest()
        return any(hmac.compare_digest(sha, expected) for expected in c['users'].values())

    def reply(self, code, body):
        data = json.dumps(body).encode()
        self.send_response(code)
        self.send_header('Content-Length', str(len(data)))
        self.send_header('Content-Type', 'application/json')
        self.send_header('Cache-Control', 'no-store')
        self.end_headers()
        self.wfile.write(data)

    def do_GET(self):
        started = False
        try:
            c = config()
            route = urllib.parse.urlsplit(self.path)
            if route.path in ('/v1/auth/authorize', '/v1/auth/callback'):
                args = urllib.parse.parse_qs(route.query)
                if route.path == '/v1/auth/authorize':
                    target, browser = discord_login.authorize(c, args.get('code', [''])[0])
                    self.send_response(302)
                    self.send_header('Location', target)
                    self.send_header('Set-Cookie', '__Host-hawk-login=' + browser + '; Path=/; Secure; HttpOnly; SameSite=Lax; Max-Age=300')
                    self.send_header('Cache-Control', 'no-store')
                    self.send_header('Content-Length', '0')
                    self.end_headers()
                    return
                cookie = SimpleCookie(self.headers.get('Cookie', ''))
                browser = cookie.get('__Host-hawk-login')
                discord_login.callback(c, args.get('state', [''])[0], args.get('code', [''])[0], browser.value if browser else '')
                return self.reply(200, {'message': 'Signed in. Return to Jason Games Launcher.'})
            if not self.authorized(c):
                return self.reply(403, {'error': 'Unauthorized or revoked'})
            prefix = '/v1'
            if self.path.startswith('/v1/skate/'):
                if not c.get('skateManifest') or not c.get('skateArchive'):
                    return self.reply(404, {'error': 'Skate content unavailable'})
                c = dict(c, manifest=c['skateManifest'], archive=c['skateArchive'])
                prefix = '/v1/skate'
            m = json.loads(Path(c['manifest']).read_text())
            if self.path == prefix + '/manifest':
                for entry in m['files']:
                    identifier = entry['sha256'] if c.get('archive') else str(c['assets'][entry['path']])
                    entry['url'] = c['publicUrl'].rstrip('/') + prefix + '/files/' + identifier
                return self.reply(200, m)
            route = re.fullmatch(re.escape(prefix) + r'/files/([a-f0-9]{64}|\d+)', self.path)
            entry = next((e for e in m['files'] if route and (e['sha256'] == route[1] if c.get('archive') else str(c['assets'].get(e['path'])) == route[1])), None)
            if not entry:
                return self.reply(404, {'error': 'Unknown file'})
            repo = c['repository']
            if not re.fullmatch(r'[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+', repo):
                raise ValueError('Invalid repository')
            cache = Path(c['cache'])
            cache.mkdir(mode=0o700, parents=True, exist_ok=True)
            file = cache / entry['sha256']
            with lock:
                if not file.exists():
                    if c.get('archive'):
                        archive_file(c, m, entry, cache, file)
                    else:
                        fetch(c, int(route[1]), entry, file)
            size = file.stat().st_size
            start = 0
            requested = self.headers.get('Range')
            if requested:
                match = re.fullmatch(r'bytes=(\d+)-', requested)
                if not match or int(match[1]) >= size:
                    return self.reply(416, {'error': 'Invalid byte range'})
                start = int(match[1])
            if not self.authorized(config()):
                return self.reply(403, {'error': 'Access revoked'})
            self.send_response(206 if requested else 200)
            self.send_header('Content-Length', str(size - start))
            self.send_header('Accept-Ranges', 'bytes')
            self.send_header('Cache-Control', 'no-store')
            if requested:
                self.send_header('Content-Range', f'bytes {start}-{size-1}/{size}')
            self.end_headers()
            started = True
            with file.open('rb') as inp:
                inp.seek(start)
                while block := inp.read(1024 * 1024):
                    if not self.authorized(config()):
                        self.close_connection = True
                        break
                    self.wfile.write(block)
        except Exception:
            if not started:
                self.reply(502, {'error': 'Release storage unavailable'})

    def do_POST(self):
        try:
            c = config()
            if self.path in ('/v1/attach', '/v1/heartbeat', '/v1/release') and c.get('sessionProxy') is True:
                length = int(self.headers.get('Content-Length', '0'))
                if not 0 < length <= 4096:
                    return self.reply(400, {'error': 'Invalid request'})
                # Fixed loopback target and exact attach-only allowlist. Never
                # forward to the Discord controller or expose wake/admin routes.
                request = urllib.request.Request('http://127.0.0.1:18765' + self.path, data=self.rfile.read(length), headers={'Authorization': self.headers.get('Authorization', ''), 'Content-Type': 'application/json'})
                try:
                    with urllib.request.urlopen(request, timeout=10) as response:
                        return self.reply(response.status, json.loads(response.read(65536)))
                except urllib.error.HTTPError as error:
                    with error:
                        return self.reply(error.code, json.loads(error.read(65536)))
            if self.path not in ('/v1/auth/device', '/v1/auth/token'):
                return self.reply(404, {'error': 'Unknown action'})
            if not c.get('discordClientId') or not c.get('discordGuildId'):
                return self.reply(503, {'error': 'Publisher sign-in is not configured'})
            length = int(self.headers.get('Content-Length', '0'))
            if not 0 < length <= 4096:
                return self.reply(400, {'error': 'Invalid request'})
            payload = json.loads(self.rfile.read(length))
            if self.path == '/v1/auth/device':
                return self.reply(200, discord_login.create(c))
            if self.path == '/v1/auth/token':
                return self.reply(200, discord_login.exchange(c, payload.get('device_code', '')))
            self.reply(404, {'error': 'Unknown action'})
        except ValueError:
            self.reply(403, {'error': 'Sign-in expired, denied or revoked'})
        except Exception:
            self.reply(503, {'error': 'Sign-in unavailable'})

if __name__ == '__main__':
    ThreadingHTTPServer(('127.0.0.1', int(os.environ.get('HAWK_DOWNLOAD_PORT', '18766'))), Handler).serve_forever()
