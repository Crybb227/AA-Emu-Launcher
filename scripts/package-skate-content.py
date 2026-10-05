"""Package audited runtime-only Skate content after publisher rights confirmation."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import shutil
import zipfile
p=argparse.ArgumentParser()
p.add_argument('--source',type=Path,required=True)
p.add_argument('--output',type=Path,required=True)
p.add_argument('--version',required=True)
a=p.parse_args()
if a.output.exists(): raise SystemExit('Use a fresh output directory')
assets=a.source/'skate-data/assets'
paths={'private/skater.glb':'Embedded skater/board meshes and textures; derives rig and board JSON',
       'private/game.json':'Game manifest',
       'private/stock/physics-skeletons.json':'Physics skeleton/body settings',
       'private/stock/skater-collections.json':'Runtime collections for physics, controls, camera and animation',
       'private/stock/data/config/input.cfg':'Input mapping',
       'private/stock/data/script/camera/Default_cameragraph.stategraph':'Camera graph',
       'private/stock/data/camera/1.shk':'Camera shake samples',
       'private/stock/data/camera/2.shk':'Camera shake samples'}
manifest=json.loads((assets/'private/game.json').read_text())
for key in ('character_scene','action_graph','motion_graph'):paths[manifest[key]]=key
for name in ('OnBoard','OffBoard'):paths['private/stock/data/anim/'+name+'.abin']='Stock animation bank (both banks loaded by engine)'
for name in ('skater','skater90','skaterN90','skater_air','skater_fingerflip','skaterls','skaterstep'):paths['private/stock/data/joystick/'+name+'.pat']='Controller gesture recognizer'
selected=[]
for path,reason in sorted(paths.items()):
    if not path.startswith('private/') or '..' in path.split('/') or ':' in path or '\\' in path:raise SystemExit('Unsafe asset reference')
    selected.append((assets/path,'skate-data/assets/'+path,reason))
audio=a.source/'skate-audio'
for name in ('roll_concrete','roll_asphalt','grind_trucks','grind_board','wind'):selected.append((audio/(name+'.wav'),'skate-audio/'+name+'.wav','Board audio loop'))
for prefix in ('pop','land','land_hard','flip','bail','skid'):
    i=1
    while (audio/f'{prefix}_{i}.wav').is_file():
        selected.append((audio/f'{prefix}_{i}.wav',f'skate-audio/{prefix}_{i}.wav','Board audio cue'));i+=1
    if i==1:raise SystemExit('Missing board audio cue: '+prefix)
a.output.mkdir(parents=True)
files=[]
for source,path,reason in selected:
    if not source.is_file() or source.is_symlink():raise SystemExit('Missing or linked runtime asset: '+path)
    dest=a.output/path;dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source,dest)
    with dest.open('rb') as stream:sha=hashlib.file_digest(stream,'sha256').hexdigest()
    files.append({'path':path,'size':dest.stat().st_size,'sha256':sha,'reason':reason})
data={'schema':1,'kind':'skate-content','version':a.version,'redistribution':'Publisher confirmed necessary Skate 3 content redistribution rights on 2026-10-05. WoW assets excluded.','engineRevision':'2af41542901a1376ea40fa089ad99c751ceab077','files':files}
(a.output/'manifest.json').write_text(json.dumps(data,indent=2))
with zipfile.ZipFile(str(a.output)+'.zip','w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
    for entry in files:z.write(a.output/entry['path'],entry['path'])
    z.write(a.output/'manifest.json','manifest.json')
print('Necessary content:',len(files),'files;',sum(f['size'] for f in files),'bytes')
print('Excluded raw Xbox files, unused maps/character source/stock content, WoW assets, credentials and user settings.')
print('Package:',str(a.output)+'.zip')
