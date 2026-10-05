"""Prepare an asset-free Windows release, with conversion sources and licence notices.

Run on the build machine only. This never uploads files or reads game installations.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import zipfile

p = argparse.ArgumentParser()
p.add_argument('--build', type=Path, required=True)
p.add_argument('--converter', type=Path, required=True)
p.add_argument('--output', type=Path, required=True)
p.add_argument('--version', required=True)
p.add_argument('--zip', action='store_true', help='Create an asset-free release ZIP beside the output')
a = p.parse_args()
if a.output.exists() or not re.fullmatch(r'[A-Za-z0-9][A-Za-z0-9._-]{0,80}', a.version):
    raise SystemExit('Use a fresh output directory and valid version')
archive_path = Path(str(a.output) + '.zip')
if a.zip and archive_path.exists(): raise SystemExit('Release ZIP already exists; choose a fresh version')
source = a.build / 'source'
binary = source / 'target/release/benilla.exe'
if not binary.is_file(): raise SystemExit('Build the native Windows player client first')
converter = a.converter / 'dist/skate-convert'
if not (converter / 'skate-convert.exe').is_file(): raise SystemExit('Build the standalone converter first')
repo = Path(__file__).resolve().parent.parent
a.output.mkdir(parents=True)

def copy(original, relative):
    target = a.output / relative
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(original, target)

copy(binary, 'bin/benilla.exe')
for file in converter.rglob('*'):
    if file.is_file() and '__pycache__' not in file.parts and file.suffix != '.pyc':
        copy(file, Path('tools') / file.relative_to(converter))
for name in ('LICENSE-MIT', 'LICENSE-APACHE', 'NOTICE'):
    copy(source / name, 'licenses/benilla-' + name)
copy(a.converter / 'source/LICENSE', 'licenses/converter-GPL-3.0.txt')
copy(a.converter / 'decoder/COPYING', 'licenses/vgmstream-COPYING.txt')
env = dict(os.environ, CARGO_HOME=str(a.build / 'cargo'), RUSTUP_HOME=str(a.build / 'rustup'))
env['PATH'] = str(a.build / 'cargo/bin') + os.pathsep + env.get('PATH', '')
metadata = json.loads(subprocess.check_output([str(a.build / 'cargo/bin/cargo.exe'), 'metadata', '--offline', '--locked', '--format-version', '1', '--no-default-features', '--filter-platform', 'x86_64-pc-windows-msvc'], cwd=source, env=env, text=True))
records = []
for package in metadata['packages']:
    records.append({k: package.get(k) for k in ('name', 'version', 'license', 'source')})
    directory = Path(package['manifest_path']).parent
    for file in directory.rglob('*'):
        if file.is_file() and any(term in file.name.lower() for term in ('license', 'copying', 'notice', 'ofl')) and file.suffix.lower() not in ('.rs', '.c', '.h', '.py', '.sh', '.toml', '.lock'):
            copy(file, Path('licenses/rust') / (package['name'] + '-' + package['version']) / file.relative_to(directory))
checkout = next((a.build / 'cargo/git/checkouts/2010-rust-rewrite-mashup-f18be4cebdaada4c').iterdir())
for name in ('LICENSE', 'NOTICE'): copy(checkout / name, 'licenses/mashup-' + name)
keyboard_source = repo / 'scripts/hawk-keyboard'
for file in keyboard_source.rglob('*'):
    if file.is_file(): copy(file, Path('licenses/hawk-keyboard-source/hawk-keyboard') / file.relative_to(keyboard_source))
for name in ('Build-HawkWindows.ps1', 'Test-HawkKeyboard.ps1'):
    copy(repo / 'scripts' / name, Path('licenses/hawk-keyboard-source') / name)
site = a.converter / 'venv/Lib/site-packages'
for directory in site.glob('*.dist-info'):
    for file in directory.rglob('*'):
        if file.is_file() and ('license' in file.name.lower() or 'copying' in file.name.lower()):
            copy(file, Path('licenses/python') / directory.name / file.relative_to(directory))
python_executable = Path(subprocess.check_output(['python', '-c', 'import sys; print(sys.base_prefix)'], text=True).strip())
copy(python_executable / 'LICENSE.txt', 'licenses/python/CPython-LICENSE.txt')
(a.output / 'licenses/dependency-metadata.json').write_text(json.dumps(records, indent=2))
# Exact GPL converter sources and build scripts accompany its executable. No game
# source, converted payloads, .probe-identity or player settings enter this archive.
with zipfile.ZipFile(a.output / 'licenses/converter-corresponding-source.zip', 'w', zipfile.ZIP_DEFLATED) as archive:
    for file in (a.converter / 'source/tools').rglob('*'):
        if file.is_file() and '__pycache__' not in file.parts and file.suffix != '.pyc': archive.write(file, 'engine/tools/' + file.relative_to(a.converter / 'source/tools').as_posix())
    archive.write(a.converter / 'source/LICENSE', 'engine/LICENSE')
    for file in (a.converter / 'audio').glob('*.py'): archive.write(file, 'audio/' + file.name)
    for name in ('hawk-convert.py', 'Build-HawkConverter.ps1'): archive.write(repo / 'scripts' / name, 'build/' + name)
    archive.writestr('BUILD.txt', 'Converter source: https://github.com/SK8-ENGINE/skate-3-rust-engine at 4488651c35c44365faa1ba38eed5758b6ebde714\nBuild with the accompanying PowerShell script on Windows, Python 3.14, Rust 1.98.1 and MSVC.\nThis wrapper and the engine are GPL-3.0-only; the audio scripts are MIT OR Apache-2.0. Preserve vendor notices.\nvgmstream r2117 Windows codec binaries are unmodified; see https://github.com/vgmstream/vgmstream/tree/r2117 and its build/dependency instructions.\n')
files = []
for file in sorted(a.output.rglob('*')):
    if file.is_file():
        with file.open('rb') as stream: sha = hashlib.file_digest(stream, 'sha256').hexdigest()
        files.append({'path': file.relative_to(a.output).as_posix(), 'size': file.stat().st_size, 'sha256': sha})
manifest = {'schema': 1, 'platform': 'windows-x86_64', 'version': a.version, 'files': files}
(a.output / 'manifest.json').write_text(json.dumps(manifest, indent=2))
if a.zip:
    with zipfile.ZipFile(archive_path, 'w', zipfile.ZIP_DEFLATED) as archive:
        for file in sorted(a.output.rglob('*')):
            if file.is_file(): archive.write(file, file.relative_to(a.output).as_posix())
    print('Asset-free release ZIP:', archive_path)
print('Native Windows release prepared:', a.output, 'files:', len(files))
print('No upload performed. Complete the third-party binary/source and private-access review before release.')
