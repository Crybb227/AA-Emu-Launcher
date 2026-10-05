import hashlib
import importlib.util
import json
import os
from pathlib import Path
import tempfile
import threading
import unittest
import urllib.error
import urllib.request
import sys
import zipfile

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / 'backend'))

spec = importlib.util.spec_from_file_location('broker', Path(__file__).resolve().parent.parent / 'backend/download_broker.py')
b = importlib.util.module_from_spec(spec)
spec.loader.exec_module(b)

class BrokerTests(unittest.TestCase):
    def test_one_release_archive_verifies_selected_files_and_rejects_traversal(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            entry = {'path': 'bin/benilla.exe', 'size': 6, 'sha256': hashlib.sha256(b'abcdef').hexdigest()}
            m = {'files': [entry]}
            for unsafe in (False, True):
                archive = root / 'input.zip'
                with zipfile.ZipFile(archive, 'w') as z:
                    z.writestr('bin/benilla.exe', b'abcdef')
                    z.writestr('manifest.json', json.dumps(m))
                    if unsafe: z.writestr('../outside', b'bad')
                sha = hashlib.sha256(archive.read_bytes()).hexdigest()
                archive.replace(root / ('archive-' + sha))
                c = {'archive': {'asset_id': 42, 'size': (root / ('archive-' + sha)).stat().st_size, 'sha256': sha}}
                dest = root / ('file-' + str(unsafe))
                if unsafe:
                    with self.assertRaises(ValueError): b.archive_file(c, m, entry, root, dest)
                    self.assertFalse(dest.exists())
                else:
                    b.archive_file(c, m, entry, root, dest)
                    self.assertEqual(dest.read_bytes(), b'abcdef')
    def test_repository_credential_is_removed_on_storage_redirect(self):
        request = urllib.request.Request('https://api.github.com/repos/private/release/releases/assets/42', headers={'Authorization': 'Bearer server-only-secret'})
        redirected = b.Redirect().redirect_request(request, None, 302, 'Found', {}, 'https://release-assets.githubusercontent.com/file')
        self.assertIsNone(redirected.get_header('Authorization'))
        self.assertEqual(request.get_header('Authorization'), 'Bearer server-only-secret')

    def test_authentication_range_and_revocation(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            sha = hashlib.sha256(b'abcdef').hexdigest()
            (root / sha).write_bytes(b'abcdef')
            manifest = root / 'manifest.json'
            manifest.write_text(json.dumps({'files': [{'path': 'bin/benilla', 'sha256': sha, 'size': 6}]}))
            c = {'users': {'test': hashlib.sha256(b'user-secret').hexdigest()}, 'manifest': str(manifest), 'cache': str(root), 'repository': 'private/release', 'assets': {'bin/benilla': 42}, 'publicUrl': 'https://example.invalid'}
            config = root / 'config.json'
            config.write_text(json.dumps(c))
            os.environ['HAWK_DOWNLOAD_CONFIG'] = str(config)
            server = b.ThreadingHTTPServer(('127.0.0.1', 0), b.Handler)
            threading.Thread(target=server.serve_forever, daemon=True).start()
            base = f'http://127.0.0.1:{server.server_port}'
            def get(path, headers=None):
                return urllib.request.urlopen(urllib.request.Request(base + path, headers=headers or {}))
            try:
                with self.assertRaises(urllib.error.HTTPError) as error:
                    get('/v1/manifest')
                self.assertEqual(error.exception.code, 403)
                error.exception.close()
                with get('/v1/files/42', {'Authorization': 'Bearer user-secret', 'Range': 'bytes=2-'}) as response:
                    self.assertEqual(response.status, 206)
                    self.assertEqual(response.headers['Content-Range'], 'bytes 2-5/6')
                    self.assertEqual(response.read(), b'cdef')
                with self.assertRaises(urllib.error.HTTPError) as error:
                    get('/v1/files/99', {'Authorization': 'Bearer user-secret'})
                self.assertEqual(error.exception.code, 404)
                error.exception.close()
                skate = root / 'skate-manifest.json'
                skate.write_text(json.dumps({'schema': 1, 'kind': 'skate-content', 'files': [{'path': 'skate-audio/pop_1.wav', 'sha256': sha, 'size': 6}]}))
                c['skateManifest'] = str(skate)
                c['skateArchive'] = {'asset_id': 43, 'size': 6, 'sha256': sha}
                config.write_text(json.dumps(c))
                with get('/v1/skate/manifest', {'Authorization': 'Bearer user-secret'}) as response:
                    returned = json.loads(response.read())
                    self.assertEqual(returned['files'][0]['url'], 'https://example.invalid/v1/skate/files/' + sha)
                with get('/v1/skate/files/' + sha, {'Authorization': 'Bearer user-secret', 'Range': 'bytes=2-'}) as response:
                    self.assertEqual(response.status, 206)
                    self.assertEqual(response.read(), b'cdef')
                c['users'] = {}
                config.write_text(json.dumps(c))
                with self.assertRaises(urllib.error.HTTPError) as error:
                    get('/v1/files/42', {'Authorization': 'Bearer user-secret'})
                self.assertEqual(error.exception.code, 403)
                error.exception.close()
                with self.assertRaises(urllib.error.HTTPError) as error:
                    get('/v1/skate/files/' + sha, {'Authorization': 'Bearer user-secret'})
                self.assertEqual(error.exception.code, 403)
                error.exception.close()
                c['publicDownloads'] = True
                config.write_text(json.dumps(c))
                with get('/v1/skate/manifest') as response:
                    self.assertEqual(response.status, 200)
                with get('/v1/skate/files/' + sha) as response:
                    self.assertEqual(response.read(), b'abcdef')
                with self.assertRaises(urllib.error.HTTPError) as error:
                    get('/v1/wake')
                self.assertEqual(error.exception.code, 404); error.exception.close()
                c['publicDownloads'] = False
                config.write_text(json.dumps(c))
                with self.assertRaises(urllib.error.HTTPError) as error:
                    get('/v1/skate/manifest')
                self.assertEqual(error.exception.code, 403); error.exception.close()
            finally:
                server.shutdown(); server.server_close()

if __name__ == '__main__':
    unittest.main()
