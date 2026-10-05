"""WSL installer and session supervisor. JSON request on stdin; no saved secrets."""
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import shutil
import socket
import subprocess
import sys
import tempfile
import time
import urllib.request
import urllib.error
from urllib.parse import urlsplit
import uuid
import zipfile
import fcntl

REFERENCE = Path('/home/dml/games/world-of-skatecraft')
MPQS = ('dbc.MPQ', 'fonts.MPQ', 'interface.MPQ', 'misc.MPQ',
        'model.MPQ', 'sound.MPQ', 'speech.MPQ', 'terrain.MPQ', 'texture.MPQ', 'wmo.MPQ', 'patch.MPQ', 'patch-2.MPQ')

def say(message):
    print(message, flush=True)

def digest(path):
    h = hashlib.sha256()
    with open(path, 'rb') as f:
        for block in iter(lambda: f.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()

def copy_file(source, target):
    # WSL sendfile across drvfs can fail allocating large-file buffers.
    with open(source, 'rb') as inp, open(target, 'wb') as out:
        shutil.copyfileobj(inp, out, 1024 * 1024)

class BrokerRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, request, fp, code, msg, headers, new_url):
        if urlsplit(new_url).scheme != 'https' or urlsplit(new_url).netloc != urlsplit(request.full_url).netloc:
            raise ValueError('Redirect outside the authenticated broker')
        return super().redirect_request(request, fp, code, msg, headers, new_url)

def linux_path(value):
    normalized = value.replace('\\', '/')
    if normalized.lower().startswith(('//wsl.localhost/', '//wsl$/')):
        parts = normalized.split('/')
        if len(parts) < 4 or parts[3].lower() != 'dml-arch':
            raise ValueError('Unsupported WSL asset location; use distro dml-arch')
        return Path('/' + '/'.join(parts[4:]))
    if re.match(r'^[A-Za-z]:[\\/]', value):
        return Path(subprocess.check_output(['wslpath', '-a', value], text=True).strip())
    return Path(value)

def safe_path(value):
    p = PurePosixPath(value)
    if not value or '\\' in value or ':' in value or p.is_absolute() or any(x in ('', '.', '..') for x in value.split('/')):
        raise ValueError('Unsafe release path: ' + value)
    return p

def manifest_check(m):
    if m.get('schema') != 1 or m.get('platform') != 'wsl-dml-arch-x86_64':
        raise ValueError('Unsupported manifest schema or client platform')
    if not re.fullmatch(r'[A-Za-z0-9._-]+', m.get('version', '')) or m['version'] in ('.', '..'):
        raise ValueError('Invalid release version')
    seen = set()
    for f in m['files']:
        p = str(safe_path(f['path']))
        if p.split('/')[0] not in ('bin', 'runtime', 'tools', 'licenses') or p in seen:
            raise ValueError('File outside distributable allowlist or duplicate: ' + p)
        seen.add(p)
        if not re.fullmatch('[0-9a-f]{64}', f['sha256']) or type(f['size']) is not int or f['size'] < 0:
            raise ValueError('Invalid size/checksum')
    if 'bin/benilla' not in seen:
        raise ValueError('Manifest is missing the client')
    return m

def fetch(url, target, size=None, sha=None, token=''):
    """Range resume into a persistent partial. Full digest before any activation."""
    if not url.startswith('https://'):
        raise ValueError('Downloads require HTTPS')
    target.parent.mkdir(parents=True, exist_ok=True)
    offset = target.stat().st_size if target.exists() else 0
    if size is not None and offset == size and digest(target) == sha:
        return
    if size is not None and offset >= size:
        target.unlink(missing_ok=True)
        offset = 0
    headers = {'Range': f'bytes={offset}-'} if offset else {}
    if token:
        headers['Authorization'] = 'Bearer ' + token
    req = urllib.request.Request(url, headers=headers)
    with urllib.request.build_opener(BrokerRedirect()).open(req, timeout=60) as response:
        if response.status == 206:
            if not response.headers.get('Content-Range', '').startswith(f'bytes {offset}-'):
                raise ValueError('Download server returned an invalid range')
        else:
            offset = 0
        with open(target, 'ab' if offset else 'wb') as output:
            while block := response.read(1024 * 1024):
                output.write(block)
                offset += len(block)
                if size is not None and offset > size:
                    raise ValueError('Download exceeds manifest size')
                say(f'Downloading {target.name}: {offset} / {size or "unknown"} bytes')
    if size is not None and (target.stat().st_size != size or digest(target) != sha):
        target.unlink(missing_ok=True)
        raise ValueError('Download size or SHA-256 mismatch; retry installation')

def atomic_json(path, value):
    temp = path.with_suffix('.tmp')
    with open(temp, 'w') as f:
        json.dump(value, f)
        f.flush()
        os.fsync(f.fileno())
    os.replace(temp, path)

def install(c, root):
    source = c.get('source', '')
    if not source:
        raise ValueError('Choose a packaged release folder or authenticated manifest HTTPS URL')
    remote = source.startswith('https://')
    if remote:
        request = urllib.request.Request(source, headers={'Authorization': 'Bearer ' + c.get('token', '')})
        with urllib.request.build_opener(BrokerRedirect()).open(request, timeout=30) as response:
            m = manifest_check(json.load(response))
    else:
        source = linux_path(source)
        m = manifest_check(json.loads((source / 'manifest.json').read_text()))
    cache = root / '.downloads' / m['version']
    cache.mkdir(parents=True, exist_ok=True)
    releases = root / 'releases'
    releases.mkdir(exist_ok=True)
    staging = Path(tempfile.mkdtemp(prefix='.staging-', dir=releases))
    try:
        for i, f in enumerate(m['files']):
            dest = staging / f['path']
            dest.parent.mkdir(parents=True, exist_ok=True)
            cached = cache / f['sha256']
            if remote:
                # URLs must be broker URLs with short-lived authorization, never GitHub credentials.
                if urlsplit(f['url']).netloc != urlsplit(source).netloc:
                    raise ValueError('Release files must be served by the authenticated manifest broker')
                fetch(f['url'], cached, f['size'], f['sha256'], c.get('token', ''))
            else:
                candidate = source / f['path']
                if candidate.is_symlink() or not candidate.resolve().is_relative_to(source.resolve()):
                    raise ValueError('Release symlinks are forbidden')
                copy_file(candidate, cached)
            if cached.stat().st_size != f['size'] or digest(cached) != f['sha256']:
                raise ValueError('Corrupt release file: ' + f['path'])
            copy_file(cached, dest)
            dest.chmod(0o755 if f.get('executable') else 0o644)
            say(f'Verified {i + 1}/{len(m["files"])}: {f["path"]}')
        (staging / 'manifest.json').write_text(json.dumps(m))
        # A fresh immutable generation allows repair without modifying the running release.
        generation = m['version'] + '-' + staging.name.removeprefix('.staging-')
        final = releases / generation
        os.rename(staging, final)
        atomic_json(root / 'current.json', {'generation': generation, 'version': m['version']})
        say('Installed ' + m['version'])
    finally:
        if staging.exists():
            shutil.rmtree(staging)

def verify_wow(folder):
    files = {}
    for p in folder.iterdir():
        if p.is_file():
            name = p.name.lower()
            if name.endswith('.mpq') and name in files:
                raise ValueError('Duplicate MPQ name: ' + p.name)
            files[name] = p
    for name in MPQS:
        p = files.get(name.lower())
        if not p or p.stat().st_size < 32:
            raise ValueError('Missing WoW 1.12.1 asset: ' + name)
        with p.open('rb') as stream:
            if stream.read(4) != b'MPQ\x1a':
                raise ValueError('Invalid MPQ: ' + name)
    # Known English 5875 base/patch archive hashes; no WoW executable is required.
    expected = json.loads((Path(__file__).parent / 'wow-5875.json').read_text())
    for name, entry in expected.items():
        if files[name.lower()].stat().st_size != entry['size'] or digest(files[name.lower()]) != entry['sha256']:
            raise ValueError('WoW version cannot be verified as English 1.12.1 build 5875: ' + name)
    return files

def import_wow(c, root):
    source = linux_path(c['input'])
    temp = Path(tempfile.mkdtemp(prefix='.wow-', dir=root))
    try:
        if source.is_file():
            with zipfile.ZipFile(source) as archive:
                names = [i for i in archive.infolist() if not i.is_dir() and PurePosixPath(i.filename.replace('\\', '/')).name.lower() in {n.lower() for n in MPQS} and 'data' in [s.lower() for s in PurePosixPath(i.filename.replace('\\', '/')).parts[:-1]]]
                if sum(i.file_size for i in names) > 12 * 1024**3:
                    raise ValueError('WoW ZIP exceeds asset import size limit')
                seen = set()
                for i in names:
                    safe_path(i.filename.replace('\\', '/'))
                    name = PurePosixPath(i.filename.replace('\\', '/')).name
                    if name.lower() in seen or (i.external_attr >> 16) & 0o170000 == 0o120000:
                        raise ValueError('Duplicate or symbolic-link MPQ')
                    seen.add(name.lower())
                    with archive.open(i) as inp, (temp / name).open('wb') as out:
                        shutil.copyfileobj(inp, out)
            verify_wow(temp)
        else:
            if (source / 'Data').is_dir():
                source = source / 'Data'
            files = verify_wow(source)
            for name, p in files.items():
                if name in {n.lower() for n in MPQS}:
                    say('Importing ' + p.name)
                    copy_file(p, temp / p.name)
        assets = root / 'assets'
        assets.mkdir(exist_ok=True)
        generation = 'wow-' + temp.name.removeprefix('.wow-')
        os.rename(temp, assets / generation)
        atomic_json(root / 'wow.json', {'folder': generation})
        say('Imported verified WoW 1.12.1 MPQs; no executable, DLLs or addons imported')
    finally:
        if temp.exists():
            shutil.rmtree(temp)

def import_skate(c, root):
    source = linux_path(c['input'])
    if not (source / 'default.xex').is_file() or not (source / 'data').is_dir():
        raise ValueError('Select your extracted Xbox 360 Skate 3 folder with default.xex and data/')
    release = current(root)
    tools = release / 'tools'
    # Locally supplied converter checkout is supported until its distribution licence is resolved.
    engine = tools / 'engine'
    if not engine.exists():
        engine = REFERENCE / '.setup/skate-engine'
        tools = REFERENCE / 'tools'
    for name in ('skate_audio.py', 'eb_extract.py'):
        if not (tools / name).exists():
            raise ValueError('Release is missing the licensed conversion tools: ' + name)
    temp = Path(tempfile.mkdtemp(prefix='.skate-', dir=root))
    try:
        env = os.environ.copy()
        env['PATH'] = str(REFERENCE / '.setup/bin') + ':' + env['PATH']
        python = root / 'conversion-venv/bin/python'
        if not python.exists():
            subprocess.run(['python3', '-m', 'venv', str(python.parent.parent)], check=True)
        subprocess.run([str(python), '-m', 'pip', 'install', '--only-binary=:all:', 'numpy==2.5.3', 'Pillow==12.3.0'], check=True)
        subprocess.run([str(python), str(engine / 'iw4l_skate_convert.py'), '--xex', str(source / 'default.xex'), '--out', str(temp / 'skate-data')], cwd=engine, env=env, check=True)
        subprocess.run([str(python), str(tools / 'skate_audio.py'), str(source / 'data'), str(temp / 'skate-audio')], env=env, check=True)
        if not (temp / 'skate-data/assets/private/skater.glb').is_file() or not (temp / 'skate-audio/pop_1.wav').is_file():
            raise ValueError('Skate 3 conversion did not produce required files')
        integrity = []
        for file in sorted(temp.rglob('*')):
            if file.is_file():
                integrity.append({'path': str(file.relative_to(temp)), 'size': file.stat().st_size, 'sha256': digest(file)})
        (temp / '.asset-integrity.json').write_text(json.dumps(integrity))
        generation = 'skate-' + temp.name.removeprefix('.skate-')
        (root / 'assets').mkdir(exist_ok=True)
        os.rename(temp, root / 'assets' / generation)
        atomic_json(root / 'skate.json', {'folder': generation})
        say('Converted your Skate 3 files')
    finally:
        if temp.exists():
            shutil.rmtree(temp)

def current(root):
    p = root / 'current.json'
    if not p.exists():
        raise ValueError('Client is not installed')
    generation = json.loads(p.read_text())['generation']
    safe_path(generation)
    return root / 'releases' / generation

def verify_skate(folder):
    for relative in ('skate-data/assets/private/skater.glb', 'skate-data/assets/private/game.json', 'skate-data/assets/private/stock/physics-skeletons.json', 'skate-data/assets/private/stock/skater-collections.json', 'skate-audio/pop_1.wav'):
        if not (folder / relative).is_file():
            raise ValueError('Missing Skate 3 asset: ' + relative + '; reimport your own game files')
    integrity = folder / '.asset-integrity.json'
    if integrity.exists():
        for entry in json.loads(integrity.read_text()):
            path = folder / safe_path(entry['path'])
            if not path.is_file() or path.stat().st_size != entry['size'] or digest(path) != entry['sha256']:
                raise ValueError('Damaged Skate 3 asset: ' + entry['path'] + '; reimport your own game files')

def repair(c, root):
    install(c, root)
    for marker, validator in (('wow.json', verify_wow), ('skate.json', verify_skate)):
        pointer = root / marker
        if not pointer.exists():
            say('Client repaired; missing ' + marker.split('.')[0] + ' assets must be imported')
            continue
        folder = json.loads(pointer.read_text())['folder']
        safe_path(folder)
        validator(root / 'assets' / folder)
    say('Repair checks completed; user settings preserved')

def api(base, route, payload, token=''):
    if not base.startswith('https://'):
        raise ValueError('Server broker requires HTTPS')
    request = urllib.request.Request(base.rstrip('/') + route, data=json.dumps(payload).encode(), headers={'Content-Type': 'application/json', 'Authorization': 'Bearer ' + token})
    with urllib.request.build_opener(BrokerRedirect()).open(request, timeout=30) as response:
        return json.load(response)

def renew(base, session, token):
    try:
        return api(base, '/v1/heartbeat', session, token)
    except urllib.error.HTTPError as error:
        if error.code != 409:
            raise
        error.close()
        return api(base, '/v1/attach', session, token)

def ready(host, port):
    try:
        with socket.create_connection((host, port), timeout=2):
            return True
    except OSError:
        return False

def launch(c, root):
    release = current(root)
    m = manifest_check(json.loads((release / 'manifest.json').read_text()))
    for f in m['files']:
        if not (release / f['path']).is_file() or digest(release / f['path']) != f['sha256']:
            raise ValueError('Client integrity check failed; use Repair')
    wow = root / 'assets' / json.loads((root / 'wow.json').read_text())['folder'] if (root / 'wow.json').exists() else None
    skate = root / 'assets' / json.loads((root / 'skate.json').read_text())['folder'] if (root / 'skate.json').exists() else None
    if not wow or not skate:
        raise ValueError('Missing assets: import your WoW 1.12.1 and Skate 3 files first')
    verify_wow(wow)
    verify_skate(skate)
    if not Path('/usr/share/vulkan/icd.d/dzn_icd.json').exists() or not Path('/dev/dxg').exists():
        raise ValueError('Unsupported graphics: WSL2 GPU forwarding and Vulkan Dozen are required')
    with (release / 'bin/benilla').open('rb') as binary:
        header = binary.read(20)
    if header[:6] != b'\x7fELF\x02\x01' or header[18:20] != b'\x3e\x00':
        raise ValueError('Unsupported client platform: a Linux x86-64 ELF client is required')
    libraries = subprocess.check_output(['ldd', str(release / 'bin/benilla')], text=True, stderr=subprocess.STDOUT)
    missing = [line.strip() for line in libraries.splitlines() if 'not found' in line]
    if missing:
        raise ValueError('Missing WSL runtime libraries: ' + '; '.join(missing))
    session = None
    process = None
    bridge = None
    broker = c.get('broker', '')
    lease = root / '.session.json'
    try:
        if broker:
            if not c.get('token'):
                raise ValueError('Launcher access token is required for the lifecycle API')
            session = {'session_id': str(uuid.uuid4())}
            try:
                status = api(broker, '/v1/attach', session, c['token'])
            except urllib.error.HTTPError as error:
                if error.code == 409:
                    error.close()
                    raise ValueError('Server offline. Start JasonHawkSkater through the Discord helper (/wake skatecraft), then retry Launch.') from None
                raise
            host, port_text = status['auth_address'].rsplit(':', 1)
            port = int(port_text)
        else:
            host, port = '127.0.0.1', 13724
            if not ready(host, port) or not ready(host, 18085):
                raise ValueError('Server offline. Start JasonHawkSkater through the Discord helper (/wake skatecraft), then retry Launch.')
        deadline = time.monotonic() + 600
        say('Waiting for game server readiness')
        while True:
            healthy = ready(host, port)
            if broker:
                healthy = healthy and status.get('ready', False)
            else:
                healthy = healthy and ready(host, 18085)
            if healthy:
                break
            if time.monotonic() > deadline:
                raise ValueError('Game server unavailable after 10 minutes')
            if session:
                status = renew(broker, session, c['token'])
            time.sleep(3)
        atomic_json(lease, {'session_id': session['session_id'] if session else str(uuid.uuid4()), 'heartbeat': time.time(), 'platform': 'wsl-dml-arch-x86_64'})
        env = os.environ.copy()
        env.update(WOW_DATA=str(wow), WOW_HOST=f'{host}:{port}', WOW_SKATE_ASSETS=str(skate / 'skate-data/assets'), WOW_SKATE_AUDIO=str(skate / 'skate-audio'), BENILLA_HOME=str(root / 'benilla-config'), VK_DRIVER_FILES='/usr/share/vulkan/icd.d/dzn_icd.json', WGPU_ALLOW_UNDERLYING_NONCOMPLIANT_ADAPTER='1', LD_LIBRARY_PATH='/usr/lib/wsl/lib:' + env.get('LD_LIBRARY_PATH', ''), WINIT_UNIX_BACKEND='x11', DISPLAY=env.get('DISPLAY', ':0'), PULSE_SERVER=env.get('PULSE_SERVER', 'unix:/mnt/wslg/PulseServer'), ALSA_CONFIG_PATH=str(release / 'runtime/asound.conf'))
        env.pop('WAYLAND_DISPLAY', None)
        if c.get('user') and c.get('password'):
            env.update(WOW_USER=c['user'], WOW_PASS=c['password'])
        bridge_script = release / 'runtime/controller-bridge.py'
        if bridge_script.exists():
            forward = subprocess.check_output(['wslpath', '-w', str(release / 'runtime/controller-forward.ps1')], text=True).strip()
            bridge = subprocess.Popen(['sudo', '-n', str(REFERENCE / '.setup/venv/bin/python'), str(bridge_script), str(os.getuid()), forward], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
            time.sleep(0.2)
            if bridge.poll() is not None:
                say('XInput forwarding unavailable; provision the documented evdev helper permissions. J and K remain available.')
        say('Server ready; starting client. J: board, K: camera, XInput: skate')
        with (root / 'client.log').open('w') as log:
            process = subprocess.Popen([str(release / 'bin/benilla')], cwd=release, env=env, stdout=log, stderr=log)
            last = time.monotonic()
            while process.poll() is None:
                time.sleep(1)
                if time.monotonic() - last >= 20:
                    if session:
                        try:
                            renew(broker, session, c['token'])
                        except urllib.error.HTTPError as error:
                            if error.code in (401, 403):
                                raise ValueError('Launcher access was revoked; the game session has ended') from None
                            say('Lifecycle API unavailable; heartbeat will retry while the client remains connected')
                            error.close()
                        except (urllib.error.URLError, TimeoutError):
                            say('Lifecycle API unavailable; heartbeat will retry while the client remains connected')
                    atomic_json(lease, {'session_id': session['session_id'] if session else 'local', 'heartbeat': time.time(), 'platform': 'wsl-dml-arch-x86_64'})
                    last = time.monotonic()
                    say('Session heartbeat')
        if process.returncode:
            raise ValueError('Client exited with an error; see client.log. Check Vulkan graphics and imported assets.')
        say('Client exited; session released')
    finally:
        if process and process.poll() is None:
            process.terminate()
            try:
                process.wait(timeout=15)
            except subprocess.TimeoutExpired:
                process.kill()
                process.wait()
        if bridge:
            bridge.terminate()
            try:
                bridge.wait(timeout=5)
            except subprocess.TimeoutExpired:
                bridge.kill()
        if session:
            try:
                api(broker, '/v1/release', session, c['token'])
            except Exception:
                say('Session release could not reach the API; the server must expire the lease')
        lease.unlink(missing_ok=True)
        # Local server is shared with the reference game; release must never stop another player.

def main():
    c = json.loads(sys.stdin.readline())
    root = Path(c['root'])
    if not root.is_absolute() or str(root) in ('/', '/home', str(Path.home()), str(REFERENCE)):
        raise ValueError('Choose a separate absolute WSL installation folder')
    root.mkdir(parents=True, exist_ok=True)
    with (root / '.operation.lock').open('w') as lock:
        try:
            fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError:
            raise ValueError('Another install or game session is active')
        {'install': install, 'repair': repair, 'wow': import_wow, 'skate': import_skate, 'launch': launch, 'status': status_report}[c['op']](c, root)

def status_report(c, root):
    if (root / 'current.json').exists():
        say('Installed client: ' + json.loads((root / 'current.json').read_text())['version'])
    else:
        say('Client not installed; choose a packaged release or authenticated manifest URL')
    say('WoW assets: ' + ('imported' if (root / 'wow.json').exists() else 'missing'))
    say('Skate 3 assets: ' + ('converted' if (root / 'skate.json').exists() else 'missing'))

if __name__ == '__main__':
    try:
        main()
    except Exception as error:
        # Do not print HTTP bodies, requests, environment or credentials.
        print(str(error), file=sys.stderr)
        sys.exit(1)
