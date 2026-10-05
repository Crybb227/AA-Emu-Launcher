import hashlib
import os
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / 'backend'))
import discord_login as a

class LoginTests(unittest.TestCase):
    def setUp(self):
        a.pending.clear()
        self.temp = tempfile.TemporaryDirectory()
        self.token = Path(self.temp.name) / 'token'; self.token.write_text('player-secret')
        self.c = {'publicUrl': 'https://games-api.jmservers.com', 'discordClientId': '123', 'discordGuildId': '456',
                  'discordRequiredRoles': ['player-role'], 'discordUsers': {'789': {'user': 'player', 'token_file': str(self.token)}},
                  'users': {'player': hashlib.sha256(b'player-secret').hexdigest()}}
        os.environ['HAWK_DISCORD_CLIENT_SECRET'] = 'backend-only'
    def tearDown(self):
        self.temp.cleanup()
    def flow(self):
        device = a.create(self.c); code = device['verification_uri_complete'].split('code=')[1]
        url, cookie = a.authorize(self.c, code)
        from urllib.parse import parse_qs, urlsplit
        state = parse_qs(urlsplit(url).query)['state'][0]
        return device, state, cookie
    def test_browser_binding_single_use_and_no_oauth_token_leak(self):
        device, state, cookie = self.flow()
        self.assertEqual(a.exchange(self.c, device['device_code']), {'error': 'authorization_pending'})
        with self.assertRaises(ValueError):
            a.callback(self.c, state, 'code', 'wrong-browser')
        with patch.object(a, 'discord_request', side_effect=[{'access_token': 'discord-only'}, {'id': '789'}, {'roles': ['player-role']}, {}]):
            a.callback(self.c, state, 'code', cookie)
        self.assertEqual(a.exchange(self.c, device['device_code'])['access_token'], 'player-secret')
        with self.assertRaises(ValueError):
            a.exchange(self.c, device['device_code'])
        with self.assertRaises(ValueError):
            a.callback(self.c, state, 'code', cookie)
    def test_role_rejection_and_revocation_before_exchange(self):
        device, state, cookie = self.flow()
        with patch.object(a, 'discord_request', side_effect=[{'access_token': 'discord-only'}, {'id': '789'}, {'roles': []}, {}]):
            with self.assertRaises(ValueError):
                a.callback(self.c, state, 'code', cookie)
        device, state, cookie = self.flow()
        with patch.object(a, 'discord_request', side_effect=[{'access_token': 'discord-only'}, {'id': '789'}, {'roles': ['player-role']}, {}]):
            a.callback(self.c, state, 'code', cookie)
        self.c['users'].clear()
        with self.assertRaises(ValueError):
            a.exchange(self.c, device['device_code'])
    def test_expired_device(self):
        device = a.create(self.c)
        for value in a.pending.values(): value['expires'] = 0
        with self.assertRaises(ValueError): a.exchange(self.c, device['device_code'])

if __name__ == '__main__': unittest.main()
