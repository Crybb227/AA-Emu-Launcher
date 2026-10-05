import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
import io
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('worker', Path(__file__).resolve().parent.parent / 'AAEmu.Launcher/HawkSkater/worker.py')
w = importlib.util.module_from_spec(spec)
spec.loader.exec_module(w)

class InstallerTests(unittest.TestCase):
    def test_expired_session_reattaches_without_waking_server(self):
        expired = w.urllib.error.HTTPError('https://api.invalid', 409, 'Expired', {}, None)
        with patch.object(w, 'api', side_effect=[expired, {'ready': True}]) as api:
            self.assertTrue(w.renew('https://api.invalid', {'session_id': 'test'}, 'token')['ready'])
            self.assertEqual([call.args[1] for call in api.call_args_list], ['/v1/heartbeat', '/v1/attach'])
    def test_windows_wsl_asset_picker_path(self):
        self.assertEqual(w.linux_path('\\\\wsl.localhost\\dml-arch\\home\\dml\\Skate3'), Path('/home/dml/Skate3'))
        with self.assertRaises(ValueError):
            w.linux_path('\\\\wsl.localhost\\another-distro\\home\\game')
    def test_user_token_cannot_follow_a_cross_origin_redirect(self):
        request = w.urllib.request.Request('https://broker.invalid/file', headers={'Authorization': 'Bearer user-secret'})
        with self.assertRaises(ValueError):
            w.BrokerRedirect().redirect_request(request, None, 302, 'Found', {}, 'https://other.invalid/file')

    def package(self, source, content=b'client'):
        (source / 'bin').mkdir(exist_ok=True)
        (source / 'bin/benilla').write_bytes(content)
        m = {'schema': 1, 'platform': 'wsl-dml-arch-x86_64', 'version': 'test.1', 'files': [{'path': 'bin/benilla', 'size': len(content), 'sha256': w.digest(source / 'bin/benilla'), 'executable': True}]}
        (source / 'manifest.json').write_text(json.dumps(m))
        return m

    def test_install_repair_and_settings_preserved(self):
        with tempfile.TemporaryDirectory() as t:
            root, source = Path(t) / 'game', Path(t) / 'package'
            root.mkdir(); source.mkdir()
            self.package(source)
            (root / 'benilla-config').mkdir()
            (root / 'benilla-config/config.toml').write_text('personal settings')
            w.install({'source': str(source)}, root)
            (w.current(root) / 'bin/benilla').write_bytes(b'damaged')
            w.install({'source': str(source)}, root)
            self.assertEqual((w.current(root) / 'bin/benilla').read_bytes(), b'client')
            self.assertEqual((root / 'benilla-config/config.toml').read_text(), 'personal settings')

    def test_corruption_never_activates(self):
        with tempfile.TemporaryDirectory() as t:
            root, source = Path(t) / 'game', Path(t) / 'package'
            root.mkdir(); source.mkdir()
            self.package(source)
            w.install({'source': str(source)}, root)
            previous = (root / 'current.json').read_bytes()
            (source / 'bin/benilla').write_bytes(b'corrupt')
            with self.assertRaises(ValueError):
                w.install({'source': str(source)}, root)
            self.assertEqual((root / 'current.json').read_bytes(), previous)

    def test_traversal_and_user_files_rejected(self):
        with tempfile.TemporaryDirectory() as t:
            m = self.package(Path(t))
            for path in ('../credentials', '/etc/passwd', 'bin/../file', 'bin\\file', 'benilla-config/config.toml', 'WoW/Data/dbc.MPQ'):
                m['files'][0]['path'] = path
                with self.assertRaises(ValueError):
                    w.manifest_check(m)

    def test_wrong_wow_and_missing_files(self):
        with tempfile.TemporaryDirectory() as t:
            with self.assertRaises(ValueError):
                w.verify_wow(Path(t))
            for name in w.MPQS:
                (Path(t) / name).write_bytes(b'MPQ\x1a' + b'x' * 100)
            with self.assertRaisesRegex(ValueError, 'version cannot be verified'):
                w.verify_wow(Path(t))

    def test_resume_and_ignored_range(self):
        class Response(io.BytesIO):
            status = 206
            headers = {'Content-Range': 'bytes 3-5/6'}
        with tempfile.TemporaryDirectory() as t:
            target = Path(t) / 'partial'
            target.write_bytes(b'abc')
            expected = __import__('hashlib').sha256(b'abcdef').hexdigest()
            with patch.object(w.urllib.request, 'build_opener') as opener:
                opener.return_value.open.return_value = Response(b'def')
                w.fetch('https://broker.invalid/file', target, 6, expected, 'user-token')
                request = opener.return_value.open.call_args.args[0]
                self.assertEqual(request.get_header('Range'), 'bytes=3-')
                self.assertEqual(target.read_bytes(), b'abcdef')
            target.write_bytes(b'abc')
            with patch.object(w.urllib.request, 'build_opener') as opener:
                response = Response(b'abcdef'); response.status = 200
                opener.return_value.open.return_value = response
                w.fetch('https://broker.invalid/file', target, 6, expected)
                self.assertEqual(target.read_bytes(), b'abcdef')

    def test_completed_corrupt_download_is_retried_from_zero(self):
        class Response(io.BytesIO):
            status = 200
            headers = {}
        with tempfile.TemporaryDirectory() as t:
            target = Path(t) / 'partial'
            target.write_bytes(b'xxxxxx')
            expected = __import__('hashlib').sha256(b'abcdef').hexdigest()
            with patch.object(w.urllib.request, 'build_opener') as opener:
                opener.return_value.open.return_value = Response(b'abcdef')
                w.fetch('https://broker.invalid/file', target, 6, expected)
                self.assertIsNone(opener.return_value.open.call_args.args[0].get_header('Range'))
                self.assertEqual(target.read_bytes(), b'abcdef')

if __name__ == '__main__':
    unittest.main()
