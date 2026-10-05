"""Publisher-only private release uploader. Credentials never enter output or assets."""
import argparse
import base64
import hashlib
import http.client
import json
import os
from pathlib import Path
import subprocess
import urllib.parse

REPOSITORY = 'Crybb227/skatecraft'
p = argparse.ArgumentParser()
p.add_argument('--check', action='store_true')
p.add_argument('--file', type=Path)
p.add_argument('--tag')
p.add_argument('--token-file', type=Path)
p.add_argument('--receipt', type=Path)
a = p.parse_args()

def credential():
    if a.token_file:
        return a.token_file.read_text(encoding='utf-8-sig').strip()
    env = dict(os.environ, GIT_TERMINAL_PROMPT='0', GCM_INTERACTIVE='never')
    result = subprocess.run(['git', 'credential', 'fill'], input='protocol=https\nhost=github.com\npath=' + REPOSITORY + '\n\n', capture_output=True, text=True, env=env)
    values = dict(line.split('=', 1) for line in result.stdout.splitlines() if '=' in line)
    if result.returncode or not values.get('password'):
        raise SystemExit('No existing publisher GitHub credential is available. Save a repository-scoped release credential locally; do not paste it into chat.')
    return values['password']

token = credential()
def api(path, method='GET', payload=None, host='api.github.com'):
    if host not in ('api.github.com', 'uploads.github.com') or not path.startswith('/repos/' + REPOSITORY + '/') and path != '/repos/' + REPOSITORY:
        raise ValueError('Repository/host outside publisher scope')
    headers = {'Authorization': 'Bearer ' + token, 'User-Agent': 'JasonGamesLauncher-publisher', 'Accept': 'application/vnd.github+json', 'X-GitHub-Api-Version': '2022-11-28'}
    connection = http.client.HTTPSConnection(host, timeout=300)
    try:
        if isinstance(payload, Path):
            size = payload.stat().st_size
            if size >= 2 * 1024**3: raise ValueError('Split release assets below 2 GiB')
            headers.update({'Content-Type': 'application/zip', 'Content-Length': str(size)})
            connection.putrequest(method, path)
            for key, value in headers.items(): connection.putheader(key, value)
            connection.endheaders()
            with payload.open('rb') as stream:
                while block := stream.read(1024 * 1024): connection.send(block)
        else:
            body = json.dumps(payload).encode() if payload is not None else None
            if body is not None: headers['Content-Type'] = 'application/json'
            connection.request(method, path, body, headers)
        response = connection.getresponse(); body = response.read()
        if not 200 <= response.status < 300:
            raise SystemExit('GitHub request failed (HTTP ' + str(response.status) + '); no credentials were printed.')
        return json.loads(body)
    finally: connection.close()

repo = api('/repos/' + REPOSITORY)
if not repo.get('private'): raise SystemExit('Refusing: release repository must remain private')
print('Verified private repository:', REPOSITORY)
if a.check: raise SystemExit(0)
if not a.file or not a.tag: raise SystemExit('Specify --file and --tag to upload an audited release asset')
if not a.file.is_file(): raise SystemExit('Release archive does not exist')
releases = api('/repos/' + REPOSITORY + '/releases?per_page=100')
if repo.get('size') == 0 and not releases:
    readme = '# JasonHawkSkater private distribution\n\nVersioned, audited Skate content release assets. Publisher confirmed redistribution rights for necessary Skate 3 content. No WoW assets, player credentials or repository secrets are included. Downloaded files remain inspectable.\n'
    api('/repos/' + REPOSITORY + '/contents/README.md', 'PUT', {'message': 'Initialize private Skatecraft distribution repository', 'content': base64.b64encode(readme.encode()).decode(), 'branch': repo['default_branch']})
release = next((r for r in releases if r['tag_name'] == a.tag), None)
if release is None:
    body = ('Private native Windows client/runtime pre-release, conversion tools, licenses and fork-extension sources. No proprietary game assets, user configuration or credentials.' if a.tag.startswith('client-windows-') else 'Private pre-release asset pack. Necessary Skate 3 content only, packaged following publisher redistribution-rights confirmation. No WoW archive or user configuration.')
    release = api('/repos/' + REPOSITORY + '/releases', 'POST', {'tag_name': a.tag, 'name': a.tag, 'draft': False, 'prerelease': True, 'body': body})
with a.file.open('rb') as stream: digest = hashlib.file_digest(stream, 'sha256').hexdigest()
assets = api('/repos/' + REPOSITORY + '/releases/' + str(release['id']) + '/assets')
asset = next((item for item in assets if item['name'] == a.file.name), None)
if asset is None:
    asset = api('/repos/' + REPOSITORY + '/releases/' + str(release['id']) + '/assets?name=' + urllib.parse.quote(a.file.name), 'POST', a.file, 'uploads.github.com')
if asset['size'] != a.file.stat().st_size or asset.get('digest') != 'sha256:' + digest:
    raise SystemExit('Remote asset size/hash did not match; refuse to activate it')
if not api('/repos/' + REPOSITORY).get('private'): raise SystemExit('Repository privacy changed; refuse activation')
receipt = {'repository': REPOSITORY, 'private': True, 'tag': a.tag, 'release_url': release['html_url'], 'asset_id': asset['id'], 'size': asset['size'], 'sha256': digest, 'name': asset['name']}
if a.receipt:
    a.receipt.parent.mkdir(parents=True, exist_ok=True)
    a.receipt.write_text(json.dumps(receipt, indent=2))
print(json.dumps(receipt, indent=2))
