"""Publisher-only readback acceptance; credentials remain transient in memory."""
import argparse
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import sys

p = argparse.ArgumentParser()
p.add_argument('--receipt', type=Path, required=True)
p.add_argument('--output', type=Path, required=True)
p.add_argument('--token-file', type=Path)
a = p.parse_args()
r = json.loads(a.receipt.read_text())
if r['repository'] != 'Crybb227/skatecraft' or not r['private']:
    raise SystemExit('Refusing unapproved repository')
if a.token_file:
    token = a.token_file.read_text(encoding='utf-8-sig').strip()
else:
    result = subprocess.run(['git', 'credential', 'fill'], input='protocol=https\nhost=github.com\npath=Crybb227/skatecraft\n\n', capture_output=True, text=True, env=dict(os.environ, GIT_TERMINAL_PROMPT='0', GCM_INTERACTIVE='never'))
    values = dict(line.split('=', 1) for line in result.stdout.splitlines() if '=' in line)
    if result.returncode or not values.get('password'): raise SystemExit('Publisher credential unavailable')
    token = values['password']
sys.path.insert(0, str(Path(__file__).resolve().parent.parent / 'backend'))
spec = importlib.util.spec_from_file_location('broker', Path(__file__).resolve().parent.parent / 'backend/download_broker.py')
b = importlib.util.module_from_spec(spec); spec.loader.exec_module(b)
os.environ['HAWK_GITHUB_TOKEN'] = token
try:
    if a.output.exists(): raise SystemExit('Use a fresh readback path')
    a.output.parent.mkdir(parents=True, exist_ok=True)
    b.fetch({'repository': r['repository']}, r['asset_id'], r, a.output)
    if a.output.stat().st_size != r['size'] or __import__('hashlib').sha256(a.output.read_bytes()).hexdigest() != r['sha256']: raise SystemExit('Readback verification failed')
    print('PASS private GitHub release readback through backend download code; exact size and SHA-256 verified.')
finally:
    os.environ.pop('HAWK_GITHUB_TOKEN', None)
