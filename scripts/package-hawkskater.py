"""Run under dml-arch. Copies an explicit asset-free file allowlist, never a whole install."""
import argparse
import importlib.util
import json
from pathlib import Path
import shutil
import os
import subprocess

parser = argparse.ArgumentParser()
parser.add_argument('--reference', type=Path, default=Path('/home/dml/games/world-of-skatecraft'))
parser.add_argument('--output', type=Path, required=True)
parser.add_argument('--version', required=True)
parser.add_argument('--flavor', choices=('reference-dev', 'player'), default='reference-dev')
args = parser.parse_args()
repo = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('worker', repo / 'AAEmu.Launcher/HawkSkater/worker.py')
worker = importlib.util.module_from_spec(spec)
spec.loader.exec_module(worker)
if args.output.exists():
    raise SystemExit('Output already exists; choose a fresh package directory')
entries = {
    'bin/benilla': 'target/release/benilla',
    'runtime/asound.conf': '.setup/asound.conf',
    'runtime/controller-bridge.py': '.setup/controller-bridge.py',
    'runtime/controller-forward.ps1': '.setup/controller-forward.ps1',
    'tools/skate_audio.py': 'tools/skate_audio.py',
    'tools/eb_extract.py': 'tools/eb_extract.py',
    'licenses/LICENSE-MIT': 'LICENSE-MIT',
    'licenses/LICENSE-APACHE': 'LICENSE-APACHE',
    'licenses/NOTICE': 'NOTICE',
}
manifest = {'schema': 1, 'platform': 'wsl-dml-arch-x86_64', 'version': args.version, 'files': []}
manifest['flavor'] = args.flavor
for dest, src in entries.items():
    p = args.reference / src
    target = args.output / dest
    target.parent.mkdir(parents=True, exist_ok=True)
    worker.copy_file(p, target)
    manifest['files'].append({'path': dest, 'size': target.stat().st_size, 'sha256': worker.digest(target), 'executable': dest == 'bin/benilla'})
worker.manifest_check(manifest)
# Preserve the actual pinned mashup dependency's licence/notice separately from the
# reference's NOTICE, which describes engine ownership without repeating its licence.
checkouts = Path('/home/dml/.cargo/git/checkouts/2010-rust-rewrite-mashup-f18be4cebdaada4c')
engine_root = next((p for p in checkouts.iterdir() if p.name.startswith('2af4154')), None)
if engine_root:
    for name in ('LICENSE', 'NOTICE'):
        target = args.output / 'licenses' / ('mashup-' + name)
        worker.copy_file(engine_root / name, target)
        manifest['files'].append({'path': str(target.relative_to(args.output)), 'size': target.stat().st_size, 'sha256': worker.digest(target), 'executable': False})
env = dict(os.environ, PATH='/home/dml/.cargo/bin:/usr/bin:/bin')
result = subprocess.run(['/home/dml/.cargo/bin/cargo', 'metadata', '--offline', '--locked', '--format-version', '1', '--no-default-features', '--filter-platform', 'x86_64-unknown-linux-gnu'], cwd=args.reference, env=env, capture_output=True, text=True)
if result.returncode == 0:
    metadata = json.loads(result.stdout)
    report = []
    for package in metadata['packages']:
        report.append({key: package.get(key) for key in ('name', 'version', 'license', 'source')})
        directory = Path(package['manifest_path']).parent
        for file in directory.rglob('*'):
            if file.is_file() and any(term in file.name.lower() for term in ('license', 'copying', 'notice', 'ofl')) and file.suffix.lower() not in ('.rs', '.c', '.h', '.py', '.sh', '.toml', '.lock'):
                target = args.output / 'licenses/dependencies' / (package['name'] + '-' + package['version']) / file.relative_to(directory)
                target.parent.mkdir(parents=True, exist_ok=True)
                worker.copy_file(file, target)
                manifest['files'].append({'path': str(target.relative_to(args.output)), 'size': target.stat().st_size, 'sha256': worker.digest(target), 'executable': False})
    target = args.output / 'licenses/dependency-metadata.json'
    target.write_text(json.dumps(report, indent=2))
    manifest['files'].append({'path': str(target.relative_to(args.output)), 'size': target.stat().st_size, 'sha256': worker.digest(target), 'executable': False})
else:
    raise SystemExit('Dependency metadata unavailable offline. Package remains unapproved; resolve the Cargo cache and retry with a fresh output.')
worker.manifest_check(manifest)
(args.output / 'manifest.json').write_text(json.dumps(manifest, indent=2))
print('Local test package created. Review dependency redistribution rights before uploading.')
