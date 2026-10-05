"""Clean local install, actual asset imports/conversion, unattended login and board probe.

Uses ONLY the reference checkout's declared probe identity, never a player's account.
"""
import importlib.util
import os
from pathlib import Path
import subprocess
import tempfile
import hashlib
import json
import secrets
import ssl
import threading
import time
from unittest.mock import patch

repo = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('worker', repo / 'AAEmu.Launcher/HawkSkater/worker.py')
w = importlib.util.module_from_spec(spec)
spec.loader.exec_module(w)
original_say = w.say
w.say = lambda message: original_say(message) if 'licenses/' not in message else None
root = Path(os.environ['HAWK_VERIFY_ROOT']) if os.environ.get('HAWK_VERIFY_ROOT') else Path(tempfile.mkdtemp(prefix='hawkskater-clean-', dir='/home/dml/games'))
print('Clean test installation:', root, flush=True)
if not (root / 'current.json').exists():
    w.install({'source': os.environ.get('HAWK_VERIFY_PACKAGE', '/home/dml/games/hawkskater-release-2026.10.04.1')}, root)
elif os.environ.get('HAWK_VERIFY_PACKAGE'):
    w.install({'source': os.environ['HAWK_VERIFY_PACKAGE']}, root)
if not (root / 'wow.json').exists():
    w.import_wow({'input': '/mnt/c/Users/jsnmu/Downloads/Stonetavern-Enhanced-1.12.1-v1.4/Data'}, root)
if not (root / 'skate.json').exists():
    w.import_skate({'input': str(w.REFERENCE / '.setup/skate3-extracted')}, root)
identity = {}
for line in (w.REFERENCE / '.probe-identity').read_text().splitlines():
    key, _, value = line.partition('=')
    if key in ('WOW_USER', 'WOW_PASS', 'WOW_CHAR'):
        identity[key] = value.strip().strip('"').strip("'")
if set(identity) != {'WOW_USER', 'WOW_PASS', 'WOW_CHAR'}:
    raise SystemExit('Declared probe identity incomplete')
os.environ.update(identity)
os.environ.update(WOW_UNATTENDED='1', WOW_NOSOUND='1', WOW_GM='off', WOW_SKATE_AUTOSTART='5', WOW_SKATE_DEBUG='1', WOW_PROBE_EXIT_AT='65', DISPLAY=':92')
xvfb = subprocess.Popen(['Xvfb', ':92', '-screen', '0', '1280x720x24'], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
backend_spec = importlib.util.spec_from_file_location('lifecycle', repo / 'backend/lifecycle_api.py')
backend = importlib.util.module_from_spec(backend_spec)
backend_spec.loader.exec_module(backend)
api_root = Path(tempfile.mkdtemp(prefix='hawkskater-api-test-'))
token = secrets.token_urlsafe(32)
configuration = {'users': {'probe': hashlib.sha256(token.encode()).hexdigest()}, 'compose': str(w.REFERENCE / 'server/compose.yaml'), 'project': 'world-of-skatecraft', 'auth_address': '127.0.0.1:13724', 'world_address': '127.0.0.1:18085', 'state': str(api_root / 'sessions.json')}
(api_root / 'config.json').write_text(json.dumps(configuration))
os.environ['HAWK_LIFECYCLE_CONFIG'] = str(api_root / 'config.json')
subprocess.run(['openssl', 'req', '-x509', '-newkey', 'rsa:2048', '-nodes', '-days', '1', '-keyout', str(api_root / 'key.pem'), '-out', str(api_root / 'cert.pem'), '-subj', '/CN=localhost', '-addext', 'subjectAltName=DNS:localhost'], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
os.environ['SSL_CERT_FILE'] = str(api_root / 'cert.pem')
server = backend.ThreadingHTTPServer(('127.0.0.1', 0), backend.Handler)
tls = ssl.SSLContext(ssl.PROTOCOL_TLS_SERVER)
tls.load_cert_chain(api_root / 'cert.pem', api_root / 'key.pem')
server.socket = tls.wrap_socket(server.socket, server_side=True)
threading.Thread(target=server.serve_forever, daemon=True).start()
close_spec = importlib.util.spec_from_file_location('close', repo / 'scripts/xvfb-close.py')
close = importlib.util.module_from_spec(close_spec)
close_spec.loader.exec_module(close)
actual_popen = subprocess.Popen
observers = []
def observe_client(*args, **kwargs):
    child = actual_popen(*args, **kwargs)
    if args and str(args[0][0]).endswith('/bin/benilla'):
        def end_probe():
            deadline = time.monotonic() + 70
            while child.poll() is None and time.monotonic() < deadline:
                time.sleep(1)
            if child.poll() is None:
                if xvfb.poll() is not None:
                    child.terminate()
                    return
                close.close_test_windows(':92')
                time.sleep(10)
                if child.poll() is None:
                    child.terminate()
        thread = threading.Thread(target=end_probe, daemon=True)
        observers.append(thread)
        thread.start()
    return child
try:
    if xvfb.poll() is not None:
        raise SystemExit('The dedicated verification Xvfb could not start')
    with patch.object(w.subprocess, 'Popen', side_effect=observe_client):
        w.launch({'user': identity['WOW_USER'], 'password': identity['WOW_PASS'], 'token': token, 'broker': f'https://localhost:{server.server_port}'}, root)
    log = (root / 'client.log').read_text()
    assert not json.loads((api_root / 'sessions.json').read_text()), 'Session was not released'
    assert not (root / '.session.json').exists(), 'Local session was not released'
    assert 'world entry' in log and 'skate: on the board' in log, 'Live login/board evidence missing'
    assert 'panicked at' not in log, 'Client panic detected'
    print('PASS HTTPS launch request, readiness, live world entry, board activation, heartbeats and clean session release', flush=True)
    print('Live log retained at', root / 'client.log', flush=True)
    for line in log.splitlines():
        if any(tag in line.lower() for tag in ('preflight:', 'skate: on the board', 'adapterinfo', 'logged in', 'world entry')):
            print(line)
finally:
    for thread in observers:
        thread.join(timeout=12)
    xvfb.terminate()
    xvfb.wait()
    server.shutdown()
    server.server_close()
