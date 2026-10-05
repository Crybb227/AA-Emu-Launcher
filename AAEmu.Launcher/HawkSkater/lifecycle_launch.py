"""WSL launcher lifecycle bridge; credentials arrive exclusively through stdin."""
import json
import os
from pathlib import Path
import subprocess
import sys
import time
import urllib.error
import urllib.request
from urllib.parse import urlsplit
import uuid


def main():
    config = json.loads(sys.stdin.readline())
    url = config['broker'].rstrip('/')
    parsed = urlsplit(url)
    if url and (parsed.username or parsed.password or not (parsed.scheme == 'https' or
            (parsed.scheme == 'http' and parsed.hostname == '127.0.0.1'))):
        raise ValueError('Session API must use HTTPS or a host-local loopback endpoint')
    token = config.get('token', '')
    if url and not token:
        raise ValueError('Launcher access token is required')
    root = Path(config['root']).resolve()
    binary = root / 'target/release/benilla'
    if not binary.is_file():
        raise ValueError('The installed client executable was not found')
    sid = str(uuid.uuid4())
    def call(action):
        if not url:
            return dict(ready=True, auth_address=config.get('host', '24.16.12.90:13724'))
        request = urllib.request.Request(url+'/v1/'+action,
            data=json.dumps(dict(session_id=sid)).encode(),
            headers={'Authorization': 'Bearer '+token, 'Content-Type': 'application/json'})
        with urllib.request.urlopen(request, timeout=10) as response:
            return json.load(response)
    try:
        status = call('attach')
        deadline = time.monotonic()+900
        while not status['ready']:
            print('Server: '+status['phase'], flush=True)
            if time.monotonic() >= deadline:
                raise TimeoutError('Server startup deadline exceeded')
            time.sleep(2)
            status = call('heartbeat')
        print('Server ready. Starting World of Skatecraft.', flush=True)
        env = dict(os.environ, WOW_HOST=status['auth_address'], WOW_USER=config.get('user',''),
            WOW_PASS=config.get('password',''), WOW_DATA=str(root/'WoW/Data'), WOW_SKATE_ASSETS=str(root/'skate-data/assets'),
            WOW_SKATE_AUDIO=str(root/'skate-audio'), VK_DRIVER_FILES='/usr/share/vulkan/icd.d/dzn_icd.json',
            WGPU_ALLOW_UNDERLYING_NONCOMPLIANT_ADAPTER='1', ALSA_CONFIG_PATH=str(root/'.setup/asound.conf'),
            PULSE_SERVER=os.environ.get('PULSE_SERVER','unix:/mnt/wslg/PulseServer'),
            DISPLAY=os.environ.get('DISPLAY',':0'), WINIT_UNIX_BACKEND='x11')
        env['LD_LIBRARY_PATH']='/usr/lib/wsl/lib:'+env.get('LD_LIBRARY_PATH','')
        env.pop('WAYLAND_DISPLAY',None)
        wrapper = root/'.setup/launch-skatecraft.sh'
        executable = wrapper if wrapper.is_file() and os.access(wrapper, os.X_OK) else binary
        child = subprocess.Popen([str(executable)], cwd=root, env=env)
        while child.poll() is None:
            for _ in range(20):
                if child.poll() is not None:
                    break
                time.sleep(1)
            if child.poll() is None:
                try:
                    call('heartbeat')
                except urllib.error.HTTPError as exc:
                    if exc.code == 409:
                        call('attach')
                    else:
                        print('Session renewal denied; connected players remain protected.', flush=True)
                except (OSError, TimeoutError):
                    print('Lifecycle API unavailable; retrying heartbeat.', flush=True)
        return child.returncode
    finally:
        try:
            call('release')
        except Exception:
            pass


if __name__ == '__main__':
    try:
        sys.exit(main())
    except Exception as exc:
        # HTTP errors contain only status, never request headers or credentials.
        print(str(exc), file=sys.stderr)
        sys.exit(1)
