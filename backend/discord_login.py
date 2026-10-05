"""Browser-based Discord sign-in for authorized launcher users, not a game-server wake.

This is an application-specific device handoff around Discord's authorization-code
flow, not a claim that Discord implements the OAuth device grant. Secret OAuth and
per-user launcher credentials are held only by the backend. No GitHub credentials
or Discord OAuth tokens are ever returned to the player.
"""
import hashlib
import hmac
import json
import os
from pathlib import Path
import secrets
import threading
import time
import urllib.parse
import urllib.request

lock = threading.RLock()
pending = {}


def clean():
    now = time.time()
    for key in list(pending):
        if pending[key]['expires'] <= now:
            del pending[key]


def create(c):
    with lock:
        clean()
        if len(pending) >= 1000:
            raise ValueError('Too many pending sign-ins')
        device = secrets.token_urlsafe(32)
        code = secrets.token_urlsafe(24)
        pending[hashlib.sha256(device.encode()).hexdigest()] = {
            'code': code, 'expires': time.time() + 300, 'approved': None,
            'state': None, 'browser': None,
        }
        return {'device_code': device, 'expires_in': 300, 'interval': 2,
                'verification_uri_complete': c['publicUrl'].rstrip('/') + '/v1/auth/authorize?code=' + code}


def authorize(c, code):
    with lock:
        clean()
        item = next((v for v in pending.values() if hmac.compare_digest(v['code'], code)), None)
        if item is None or item['state'] is not None:
            raise ValueError('Sign-in expired or already started')
        item['state'] = secrets.token_urlsafe(32)
        browser = secrets.token_urlsafe(32)
        item['browser'] = hashlib.sha256(browser.encode()).hexdigest()
        query = urllib.parse.urlencode({'client_id': c['discordClientId'], 'response_type': 'code',
            'scope': 'identify guilds.members.read', 'state': item['state'],
            'redirect_uri': c['publicUrl'].rstrip('/') + '/v1/auth/callback', 'prompt': 'consent'})
        return 'https://discord.com/oauth2/authorize?' + query, browser


def discord_request(path, token=None, form=None):
    headers = {'User-Agent': 'JasonGamesLauncher-auth'}
    data = None
    if form is not None:
        headers['Content-Type'] = 'application/x-www-form-urlencoded'
        data = urllib.parse.urlencode(form).encode()
    if token:
        headers['Authorization'] = 'Bearer ' + token
    request = urllib.request.Request('https://discord.com/api/v10/' + path, data=data, headers=headers)
    # Discord API endpoints must not redirect credentials.
    class NoRedirect(urllib.request.HTTPRedirectHandler):
        def redirect_request(self, *_):
            raise ValueError('OAuth redirect refused')
    with urllib.request.build_opener(NoRedirect()).open(request, timeout=20) as response:
        return json.load(response)


def callback(c, state, code, browser):
    with lock:
        clean()
        item = next((v for v in pending.values() if v['state'] and hmac.compare_digest(v['state'], state)), None)
        if item is None or not browser or not hmac.compare_digest(item['browser'], hashlib.sha256(browser.encode()).hexdigest()):
            raise ValueError('Invalid sign-in state')
        item['state'] = None  # One use; failed exchanges require a new sign-in.
    result = discord_request('oauth2/token', form={'client_id': c['discordClientId'],
        'client_secret': os.environ['HAWK_DISCORD_CLIENT_SECRET'], 'grant_type': 'authorization_code',
        'code': code, 'redirect_uri': c['publicUrl'].rstrip('/') + '/v1/auth/callback'})
    token = result['access_token']
    try:
        user = discord_request('users/@me', token)
        member = discord_request('users/@me/guilds/' + c['discordGuildId'] + '/member', token)
        allowed = c.get('discordUsers', {}).get(user['id'])
        required = set(c.get('discordRequiredRoles', []))
        if not allowed or required and not required.intersection(member.get('roles', [])):
            raise ValueError('This Discord account does not have launcher access')
        if allowed['user'] not in c['users']:
            raise ValueError('Launcher access revoked')
        with lock:
            if item['expires'] <= time.time():
                raise ValueError('Sign-in expired')
            item['approved'] = user['id']
    finally:
        # Do not persist a Discord access/refresh token after the identity check.
        try:
            discord_request('oauth2/token/revoke', form={'client_id': c['discordClientId'],
                'client_secret': os.environ['HAWK_DISCORD_CLIENT_SECRET'], 'token': token})
        except Exception:
            pass


def exchange(c, device):
    if not isinstance(device, str) or len(device) > 256:
        raise ValueError('Invalid sign-in request')
    with lock:
        clean()
        key = hashlib.sha256(device.encode()).hexdigest()
        item = pending.get(key)
        if item is None:
            raise ValueError('Sign-in expired or used')
        if not item['approved']:
            return {'error': 'authorization_pending'}
        allowed = c.get('discordUsers', {}).get(item['approved'])
        if not allowed:
            raise ValueError('Launcher access revoked')
        token = Path(allowed['token_file']).read_text().strip()
        expected = c['users'].get(allowed['user'], '')
        if not expected or not hmac.compare_digest(hashlib.sha256(token.encode()).hexdigest(), expected):
            raise ValueError('Launcher access revoked')
        del pending[key]
        return {'access_token': token, 'token_type': 'Bearer'}
