"""Publisher-only atomic runtime promotion; no game/server lifecycle operations."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import tempfile
import zipfile

p = argparse.ArgumentParser()
p.add_argument('--release', type=Path, required=True)
p.add_argument('--receipt', type=Path, required=True)
p.add_argument('--config', type=Path, default=Path('/etc/hawkskater/config.json'))
p.add_argument('--activate', action='store_true')
a = p.parse_args()
m = json.loads((a.release / 'manifest.json').read_text())
r = json.loads(a.receipt.read_text())
c = json.loads(a.config.read_text())
version = m.get('version', '')
if m.get('schema') != 1 or m.get('platform') != 'windows-x86_64' or not re.fullmatch(r'[A-Za-z0-9][A-Za-z0-9._-]{0,80}', version):
    raise SystemExit('Invalid native runtime manifest')
if r.get('repository') != 'Crybb227/skatecraft' or r.get('private') is not True or c.get('repository') != r['repository'] or r.get('tag') != 'client-windows-' + version:
    raise SystemExit('Private repository/version receipt mismatch')
if not isinstance(r.get('asset_id'), int) or r['asset_id'] <= 0:
    raise SystemExit('Invalid GitHub asset ID')
archive = Path(str(a.release) + '.zip')
with archive.open('rb') as stream:
    if archive.stat().st_size != r['size'] or hashlib.file_digest(stream, 'sha256').hexdigest() != r['sha256']:
        raise SystemExit('GitHub receipt/archive hash mismatch')
allowed = {'manifest.json'}
for entry in m['files']:
    name = entry['path']
    if not name.startswith(('bin/', 'runtime/', 'tools/', 'licenses/')) or '\\' in name or ':' in name or any(part in ('', '.', '..') for part in name.split('/')):
        raise SystemExit('Unsafe runtime path')
    if name in allowed or any(name.casefold() == prior.casefold() for prior in allowed):
        raise SystemExit('Duplicate runtime path')
    allowed.add(name)
    with (a.release / name).open('rb') as stream:
        if (a.release / name).stat().st_size != entry['size'] or hashlib.file_digest(stream, 'sha256').hexdigest() != entry['sha256']:
            raise SystemExit('Runtime file size/hash mismatch')
with zipfile.ZipFile(archive) as z:
    names = [i.filename for i in z.infolist() if not i.is_dir()]
    if len(names) != len(set(names)) or set(names) != allowed or json.loads(z.read('manifest.json')) != m:
        raise SystemExit('Archive payload differs from audited manifest')
print('Verified asset-free native runtime:', version, 'files:', len(m['files']))
if not a.activate:
    raise SystemExit(0)
if os.geteuid() != 0 or a.config.resolve() != Path('/etc/hawkskater/config.json'):
    raise SystemExit('Activation requires root and the explicitly reviewed publisher config')
generation = Path('/var/lib/hawkskater') / ('release-' + version)
generation.mkdir(mode=0o750, exist_ok=True)
stat = a.config.stat()
os.chown(generation, stat.st_uid, stat.st_gid)
manifest = generation / 'manifest.json'
text = json.dumps(m, indent=2) + '\n'
if manifest.exists() and manifest.read_text() != text:
    raise SystemExit('Refusing to overwrite an immutable manifest generation')

def atomic(path, content):
    fd, temporary = tempfile.mkstemp(prefix=path.name + '.', dir=path.parent)
    try:
        os.fchmod(fd, 0o640)
        os.fchown(fd, stat.st_uid, stat.st_gid)
        with os.fdopen(fd, 'w') as stream:
            stream.write(content)
            stream.flush()
            os.fsync(stream.fileno())
        os.replace(temporary, path)
    finally:
        Path(temporary).unlink(missing_ok=True)

if not manifest.exists(): atomic(manifest, text)
backup = a.config.with_name('config.before-' + version + '.json')
if not backup.exists(): atomic(backup, a.config.read_text())
c['manifest'] = str(manifest)
c['archive'] = {key: r[key] for key in ('asset_id', 'size', 'sha256')}
atomic(a.config, json.dumps(c, indent=2) + '\n')
print('Activated runtime manifest/archive together; settings, credentials, assets and Discord startup unchanged.')
