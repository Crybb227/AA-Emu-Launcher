"""Generate a metadata-only inventory; never copy or publish proprietary asset bytes."""
import argparse
import hashlib
import json
from pathlib import Path

p = argparse.ArgumentParser()
p.add_argument('--reference', type=Path, default=Path('/home/dml/games/world-of-skatecraft'))
p.add_argument('--output', type=Path, required=True)
a = p.parse_args()
report = {'schema': 1, 'purpose': 'Local runtime asset inventory only; asset bytes are not distributable', 'files': []}
for group, folder, pattern in (('wow', a.reference / 'WoW/Data', '*.MPQ'), ('skate-model-physics-animation', a.reference / 'skate-data/assets', '**/*'), ('skate-audio', a.reference / 'skate-audio', '*.wav')):
    for file in sorted(folder.glob(pattern)):
        if file.is_file():
            h = hashlib.sha256()
            with file.open('rb') as f:
                for block in iter(lambda: f.read(1024 * 1024), b''):
                    h.update(block)
            report['files'].append({'group': group, 'path': str(file.relative_to(folder)), 'size': file.stat().st_size, 'sha256': h.hexdigest()})
a.output.parent.mkdir(parents=True, exist_ok=True)
a.output.write_text(json.dumps(report, indent=2))
print(f'Inventoried {len(report["files"])} files; copied no assets')
