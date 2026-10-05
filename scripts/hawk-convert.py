"""GPL-3.0-only standalone owned-disc converter. No game data is bundled.

The underlying conversion sources and their licences accompany the executable.
Called with a JSON {source, output} line on stdin; child tasks are confined to tools.
"""
import json
import os
from pathlib import Path
import runpy
import sys
import tempfile

ROOT = Path(getattr(sys, '_MEIPASS', Path(__file__).resolve().parent))
sys.path.insert(0, str(ROOT))

def main():
    if len(sys.argv) > 2 and sys.argv[1] == '--task':
        task = Path(sys.argv[2]).resolve()
        if not task.is_relative_to((ROOT / 'tools').resolve()):
            raise ValueError('Task outside conversion tools')
        sys.path.insert(0, str(task.parent))
        sys.argv = [str(task), *sys.argv[3:]]
        runpy.run_path(str(task), run_name='__main__')
        return
    c = json.loads(sys.stdin.readline())
    source, output = Path(c['source']).resolve(), Path(c['output']).resolve()
    if source == output or output.is_relative_to(source):
        raise ValueError('Conversion output must be separate from your original game')
    for path in ('default.xex', 'data/big/miscload.big', 'data/big/miscboot.big', 'data/big/db.big', 'data/content/createacharacter.big'):
        if not (source / path).is_file():
            raise ValueError('Incomplete extracted Skate 3 installation: ' + path)
    output.mkdir(parents=True, exist_ok=True)
    from tools.asset_pipeline import asset_exports as exports
    stage = output / 'skate-data'; stage.mkdir()
    with tempfile.TemporaryDirectory(prefix='work-', dir=output) as work, (output / 'conversion.log').open('w', encoding='utf-8') as log:
        report = lambda line: print(line, flush=True)
        converted = exports.core(source, stage, Path(work), report, log)
        exports.character(source, stage, Path(work), report, log, converted)
    # Only the WoW mode's audio is converted, not the rest of the original game's banks.
    sys.path.insert(0, str(ROOT / 'audio'))
    os.environ['PATH'] = str(ROOT / 'decoder') + os.pathsep + os.environ.get('PATH', '')
    import skate_audio
    skate_audio.main(str(source / 'data'), str(output / 'skate-audio'))
    print('Your Skate 3 content is ready.', flush=True)

if __name__ == '__main__':
    try:
        main()
    except Exception as error:
        print(str(error), file=sys.stderr)
        raise SystemExit(1)
