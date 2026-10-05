"""Test-only HTTPS attach service. Cannot start or stop a game server."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import ssl
import sys
import threading

p = argparse.ArgumentParser()
p.add_argument('--work', type=Path, required=True)
a = p.parse_args()
repo = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(repo / 'backend'))
spec = importlib.util.spec_from_file_location('lifecycle', repo / 'backend/lifecycle_api.py')
api = importlib.util.module_from_spec(spec); spec.loader.exec_module(api)
c = {'users': {'probe': hashlib.sha256((a.work / 'token').read_bytes().strip()).hexdigest()},
     'state': str(a.work / 'sessions.json'), 'auth_address': '127.0.0.1:13724', 'world_address': '127.0.0.1:18085'}
api.config = lambda: c
def ready(c):
    ok = api.reachable(c['auth_address']) and api.reachable(c['world_address'])
    return {'ready': ok, 'phase': 'ready' if ok else 'offline', 'auth_address': c['auth_address'], 'world_address': c['world_address']}
api.readiness = ready
counts = {'attach': 0, 'heartbeat': 0, 'release': 0}
class Handler(api.Handler):
    def do_POST(self):
        super().do_POST()
        action = self.path.removeprefix('/v1/')
        if action in counts:
            counts[action] += 1
            (a.work / 'counts.json').write_text(json.dumps(counts))
server = api.ThreadingHTTPServer(('127.0.0.1', 0), Handler)
ctx = ssl.SSLContext(ssl.PROTOCOL_TLS_SERVER)
ctx.load_cert_chain(str(a.work / 'cert.pem'), str(a.work / 'key.pem'))
server.socket = ctx.wrap_socket(server.socket, server_side=True)
(a.work / 'port').write_text(str(server.server_port))
threading.Thread(target=api.janitor, daemon=True).start()
server.serve_forever()
