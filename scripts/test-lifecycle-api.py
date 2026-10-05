import hashlib
import importlib.util
import json
import os
from pathlib import Path
import tempfile
import threading
import time
import unittest
from unittest.mock import patch
import urllib.error
import urllib.request
import uuid
import sys
sys.path.insert(0, str(Path(__file__).resolve().parent.parent / 'backend'))

spec = importlib.util.spec_from_file_location('api', Path(__file__).resolve().parent.parent / 'backend/lifecycle_api.py')
a = importlib.util.module_from_spec(spec)
spec.loader.exec_module(a)

class SessionTests(unittest.TestCase):
    def test_public_sessions_are_owned_and_cannot_wake(self):
        with tempfile.TemporaryDirectory() as t:
            c={'users':{},'publicSessions':True,'state':str(Path(t)/'state.json')}
            cfg=Path(t)/'config.json';cfg.write_text(json.dumps(c));os.environ['HAWK_LIFECYCLE_CONFIG']=str(cfg)
            server=a.ThreadingHTTPServer(('127.0.0.1',0),a.Handler);threading.Thread(target=server.serve_forever,daemon=True).start()
            sid=str(uuid.uuid4())
            def post(action,secret='a'*64):
                request=urllib.request.Request(f'http://127.0.0.1:{server.server_port}/v1/{action}',data=json.dumps({'session_id':sid}).encode(),headers={'Authorization':'Bearer '+secret})
                with urllib.request.urlopen(request) as response:return json.load(response)
            try:
                with patch.object(a,'compose') as docker,patch.object(a,'readiness',return_value={'ready':True}):
                    self.assertTrue(post('attach')['ready']);self.assertTrue(post('heartbeat')['ready'])
                    with self.assertRaises(urllib.error.HTTPError) as e:post('release','b'*64)
                    self.assertEqual(e.exception.code,403);e.exception.close()
                    with self.assertRaises(urllib.error.HTTPError) as e:post('wake')
                    self.assertEqual(e.exception.code,404);e.exception.close();docker.assert_not_called()
                    self.assertTrue(post('release')['released']);self.assertEqual(a.state(c),{})
                    post('attach');c['publicSessions']=False;cfg.write_text(json.dumps(c))
                    with self.assertRaises(urllib.error.HTTPError) as e:post('heartbeat')
                    self.assertEqual(e.exception.code,403);e.exception.close();self.assertEqual(a.state(c),{})
            finally:server.shutdown();server.server_close()
    def test_release_expiry_ownership_and_revocation(self):
        with tempfile.TemporaryDirectory() as t:
            c = {'users': {'one': hashlib.sha256(b'one-secret').hexdigest(), 'two': hashlib.sha256(b'two-secret').hexdigest()}, 'state': str(Path(t) / 'state.json')}
            cfg = Path(t) / 'config.json'; cfg.write_text(json.dumps(c))
            os.environ['HAWK_LIFECYCLE_CONFIG'] = str(cfg)
            server = a.ThreadingHTTPServer(('127.0.0.1', 0), a.Handler)
            threading.Thread(target=server.serve_forever, daemon=True).start()
            sid = str(uuid.uuid4())
            def post(action, token='one-secret'):
                request = urllib.request.Request(f'http://127.0.0.1:{server.server_port}/v1/{action}', data=json.dumps({'session_id': sid}).encode(), headers={'Authorization': 'Bearer ' + token})
                with urllib.request.urlopen(request) as response:
                    return json.load(response)
            try:
                with patch.object(a, 'compose') as docker, patch.object(a, 'readiness', return_value={'ready': True}) as readiness:
                    readiness.return_value = {'ready': False}
                    with self.assertRaises(urllib.error.HTTPError) as error:
                        post('attach')
                    self.assertEqual(error.exception.code, 409); error.exception.close()
                    self.assertEqual(a.state(c), {})
                    readiness.return_value = {'ready': True}
                    self.assertTrue(post('attach')['ready'])
                    self.assertTrue(post('heartbeat')['ready'])
                    with self.assertRaises(urllib.error.HTTPError) as error:
                        post('release', 'two-secret')
                    self.assertEqual(error.exception.code, 403); error.exception.close()
                    self.assertTrue(post('release')['released'])
                    self.assertEqual(a.state(c), {})
                    docker.assert_not_called()
                    post('attach')
                    state = a.state(c); state[sid]['expires'] = time.time() - 1; a.save(c, state)
                    with self.assertRaises(urllib.error.HTTPError) as error:
                        post('heartbeat')
                    self.assertEqual(error.exception.code, 409); error.exception.close()
                    post('attach')
                    del c['users']['one']; cfg.write_text(json.dumps(c))
                    with self.assertRaises(urllib.error.HTTPError) as error:
                        post('heartbeat')
                    self.assertEqual(error.exception.code, 403); error.exception.close()
                    self.assertEqual(a.state(c), {})
            finally:
                server.shutdown(); server.server_close()

if __name__ == '__main__':
    unittest.main()
