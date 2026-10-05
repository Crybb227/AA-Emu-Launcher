"""Publisher-only live private GitHub -> TLS broker -> native installer acceptance."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import secrets
import ssl
import subprocess
import sys
import tempfile
import threading

p=argparse.ArgumentParser()
p.add_argument('--app',type=Path,required=True)
p.add_argument('--receipt',type=Path,required=True)
a=p.parse_args()
r=json.loads(a.receipt.read_text())
if r['repository']!='Crybb227/skatecraft' or not r['private']:raise SystemExit('Refusing repository outside scope')
result=subprocess.run(['git','credential','fill'],input='protocol=https\nhost=github.com\npath=Crybb227/skatecraft\n\n',capture_output=True,text=True,env=dict(os.environ,GIT_TERMINAL_PROMPT='0',GCM_INTERACTIVE='never'))
values=dict(line.split('=',1) for line in result.stdout.splitlines() if '=' in line)
if result.returncode or not values.get('password'):raise SystemExit('Publisher credential unavailable')
sys.path.insert(0,str(Path(__file__).resolve().parent.parent/'backend'))
spec=importlib.util.spec_from_file_location('broker',Path(__file__).resolve().parent.parent/'backend/download_broker.py')
b=importlib.util.module_from_spec(spec);spec.loader.exec_module(b)
work=Path(tempfile.mkdtemp(prefix='HawkPrivateInstall-'))
openssl=r'C:\Program Files\Git\usr\bin\openssl.exe'
subprocess.run([openssl,'req','-x509','-newkey','rsa:2048','-nodes','-days','1','-keyout',str(work/'key.pem'),'-out',str(work/'cert.pem'),'-subj','/CN=localhost','-addext','subjectAltName=DNS:localhost'],check=True,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
subprocess.run([openssl,'x509','-in',str(work/'cert.pem'),'-outform','DER','-out',str(work/'cert.cer')],check=True)
app=a.app.resolve();compiler=Path(os.environ['WINDIR'])/'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
import shutil
for f in app.iterdir():
    if f.is_file() and f.suffix in ('.dll','.exe'):shutil.copyfile(f,work/f.name)
shutil.copyfile(app/'AAEmu.Launcher.exe.config',work/'Check.exe.config')
subprocess.run([str(compiler),'/nologo','/target:exe','/out:'+str(work/'Check.exe'),'/r:'+str(work/'AAEmu.Launcher.exe'),'/r:'+str(work/'Newtonsoft.Json.dll'),'/r:System.Net.Http.dll',str(Path(__file__).with_name('HawkPublishedPackSmoke.cs'))],check=True)
token=secrets.token_urlsafe(32)
c={'repository':r['repository'],'manifest':str(app/'HawkSkater/Release/manifest.json'),'skateManifest':str(app/'HawkSkater/SkateContent/manifest.json'),'skateArchive':r,'cache':str(work/'cache'),'users':{'acceptance':hashlib.sha256(token.encode()).hexdigest()}}
server=b.ThreadingHTTPServer(('127.0.0.1',0),b.Handler)
tls=ssl.SSLContext(ssl.PROTOCOL_TLS_SERVER);tls.load_cert_chain(work/'cert.pem',work/'key.pem');server.socket=tls.wrap_socket(server.socket,server_side=True)
c['publicUrl']='https://localhost:'+str(server.server_port)
config=work/'config.json';config.write_text(json.dumps(c));os.environ['HAWK_DOWNLOAD_CONFIG']=str(config);os.environ['HAWK_GITHUB_TOKEN']=values['password']
threading.Thread(target=server.serve_forever,daemon=True).start()
try:
    subprocess.run([str(work/'Check.exe'),str(app),c['publicUrl']+'/v1/skate/manifest',token,str(work/'cert.cer')],check=True)
    print('PASS actual private release -> authenticated TLS broker -> clean native asset install and repair. Test evidence:',work)
finally:
    server.shutdown();server.server_close();os.environ.pop('HAWK_GITHUB_TOKEN',None)
