"""Attach-only Compose session API. Loopback service behind HTTPS reverse proxy.

HAWK_LIFECYCLE_CONFIG JSON: users {id: sha256(token)}, compose, project,
auth_address, world_address, state. No game passwords or repository credentials.
Session expiry releases ownership; this service never stops a shared game server.
"""
import hashlib
import hmac
from http.server import BaseHTTPRequestHandler
from bounded_http import ThreadingHTTPServer
import json
import os
from pathlib import Path
import re
import socket
import subprocess
import threading
import time
import uuid

lock = threading.RLock()

def config():
    return json.loads(Path(os.environ['HAWK_LIFECYCLE_CONFIG']).read_text())

def compose(c, *args):
    return subprocess.check_output(['docker', 'compose', '-p', c['project'], '-f', c['compose'], *args], text=True, stderr=subprocess.PIPE, timeout=60)

def state(c):
    p = Path(c['state'])
    sessions = json.loads(p.read_text()) if p.exists() else {}
    return {key: value for key, value in sessions.items() if value['expires'] > time.time() and ((c.get('publicSessions') is True and value['user'].startswith('anonymous:')) or (value['user'] in c['users'] and value['credential'] == c['users'][value['user']]))}

def save(c, sessions):
    p = Path(c['state'])
    p.parent.mkdir(parents=True, exist_ok=True)
    temp = p.with_suffix('.tmp')
    temp.write_text(json.dumps(sessions))
    temp.chmod(0o600)
    os.replace(temp, p)

def reachable(address):
    host, port = address.rsplit(':', 1)
    try:
        with socket.create_connection((host, int(port)), timeout=1):
            return True
    except OSError:
        return False

def readiness(c):
    if c.get('tcpReadiness') is True:
        ready = reachable(c['auth_probe']) and reachable(c['world_probe'])
        return {'ready': ready, 'phase': 'ready' if ready else 'offline', 'auth_address': c['auth_address'], 'world_address': c['world_address']}
    output = compose(c, 'ps', '--format', 'json').strip()
    services = json.loads(output) if output.startswith('[') else [json.loads(line) for line in output.splitlines() if line]
    healthy = all(any(s['Service'] == name and s.get('Health') == 'healthy' for s in services) for name in ('database', 'mangosd'))
    ready = healthy and reachable(c['auth_address']) and reachable(c['world_address'])
    return {'ready': ready, 'phase': 'ready' if ready else 'starting', 'auth_address': c['auth_address'], 'world_address': c['world_address']}

def janitor():
    while True:
        time.sleep(10)
        try:
            with lock:
                c = config()
                save(c, state(c))
        except Exception:
            pass

class Handler(BaseHTTPRequestHandler):
    def log_message(self, *_):
        pass

    def reply(self, code, body):
        raw = json.dumps(body).encode()
        self.send_response(code)
        self.send_header('Content-Type', 'application/json')
        self.send_header('Content-Length', str(len(raw)))
        self.send_header('Cache-Control', 'no-store')
        self.end_headers()
        self.wfile.write(raw)

    def do_POST(self):
        try:
            c = config()
            token = self.headers.get('Authorization', '')
            if not token.startswith('Bearer ') or not token[7:]:
                return self.reply(403, {'error': 'Unauthorized'})
            credential = hashlib.sha256(token[7:].encode()).hexdigest()
            user = next((u for u, expected in c['users'].items() if hmac.compare_digest(credential, expected)), None)
            if user is None and c.get('publicSessions') is True and re.fullmatch(r'[a-f0-9]{64}', token[7:]):
                user = 'anonymous:' + credential
            if user is None:
                return self.reply(403, {'error': 'Unauthorized or revoked'})
            length = int(self.headers.get('Content-Length', '0'))
            if not 0 < length <= 4096:
                return self.reply(400, {'error': 'Invalid request size'})
            payload = json.loads(self.rfile.read(length))
            sid = payload.get('session_id', '')
            if str(uuid.UUID(sid)) != sid:
                return self.reply(400, {'error': 'Invalid session ID'})
            if self.path not in ('/v1/attach', '/v1/heartbeat', '/v1/release'):
                return self.reply(404, {'error': 'Unknown action'})
            with lock:
                sessions = state(c)
                if sid in sessions and sessions[sid]['user'] != user:
                    return self.reply(403, {'error': 'Session belongs to another user'})
                if self.path == '/v1/attach':
                    if sid not in sessions and len(sessions) >= c.get('maxSessions', 1000):
                        return self.reply(503, {'error': 'Session capacity reached'})
                    if not readiness(c)['ready'] and c.get('allowWaitingSessions') is not True:
                        return self.reply(409, {'error': 'Start JasonHawkSkater through the Discord helper, then retry Launch'})
                    sessions[sid] = {'user': user, 'credential': credential, 'expires': time.time() + 90}
                elif self.path == '/v1/heartbeat':
                    if sid not in sessions:
                        return self.reply(409, {'error': 'Session expired'})
                    sessions[sid]['expires'] = time.time() + 90
                else:
                    sessions.pop(sid, None)
                save(c, sessions)
            return self.reply(200, {'released': True} if self.path == '/v1/release' else readiness(c))
        except (ValueError, KeyError, TypeError):
            self.reply(400, {'error': 'Invalid request'})
        except Exception:
            self.reply(503, {'error': 'Game server unavailable'})

if __name__ == '__main__':
    threading.Thread(target=janitor, daemon=True).start()
    ThreadingHTTPServer(('127.0.0.1', int(os.environ.get('HAWK_LIFECYCLE_PORT', '18765'))), Handler).serve_forever()
