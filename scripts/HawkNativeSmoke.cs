using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using AAEmu.Launcher;
class HawkNativeSmoke {
 static readonly CancellationToken None=CancellationToken.None;
 static void Assert(bool ok,string text){if(!ok)throw new Exception(text); Console.WriteLine("PASS "+text);}
 static HawkManifest Manifest(string source,byte[] data){Directory.CreateDirectory(Path.Combine(source,"bin"));File.WriteAllBytes(Path.Combine(source,"bin","benilla.exe"),data);return new HawkManifest{schema=1,platform="windows-x86_64",version="test.1",files=new List<HawkFile>{new HawkFile{path="bin/benilla.exe",size=data.Length,sha256=HawkNativeService.Hash(Path.Combine(source,"bin","benilla.exe")),url="https://publisher.invalid/client"}}};}
 static void Write(string source,HawkManifest m){File.WriteAllText(Path.Combine(source,"manifest.json"),JsonConvert.SerializeObject(m));}
 static void Reject(Action action,string text){bool failed=false;try{action();}catch{failed=true;}Assert(failed,text);}
 class Fake : HttpMessageHandler {
  public string json; public byte[] data; public bool ignoreRange,redirect,anonymous; public long seenRange=-1;
  protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancel){
   if(anonymous)Assert(request.Headers.Authorization==null,"public downloads omit saved credentials");
   if(request.RequestUri.AbsolutePath=="/manifest")return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(json)});
   if(!anonymous)Assert(request.Headers.Authorization.Parameter=="test-token","narrow user token supplied only to publisher");
   if(redirect){var r=new HttpResponseMessage(HttpStatusCode.Redirect);r.Headers.Location=new Uri("https://other.invalid/file");return Task.FromResult(r);}
   long start=request.Headers.Range==null?0:request.Headers.Range.Ranges.First().From.Value;seenRange=start;
   bool resumed=start>0&&!ignoreRange;if(!resumed)start=0;
   var response=new HttpResponseMessage(resumed?HttpStatusCode.PartialContent:HttpStatusCode.OK){Content=new ByteArrayContent(data.Skip((int)start).ToArray())};
   if(resumed)response.Content.Headers.ContentRange=new ContentRangeHeaderValue(start,data.Length-1,data.Length);
   return Task.FromResult(response);
  }
 }
 class Mirror : HttpMessageHandler {
  public byte[] data;public long range=-1;
  protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancel){
   Assert(request.Headers.Authorization==null,"private launcher token never sent to Stonetavern mirror");
   range=request.Headers.Range.Ranges.First().From.Value;
   var response=new HttpResponseMessage(HttpStatusCode.PartialContent){Content=new ByteArrayContent(data.Skip((int)range).ToArray())};
   response.Content.Headers.ContentRange=new ContentRangeHeaderValue(range,data.Length-1,data.Length);return Task.FromResult(response);
  }
 }
 static void Main(string[] args){var temp=Path.Combine(Path.GetTempPath(),"HawkNativeSmoke-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
  var source=Path.Combine(temp,"release");var root=Path.Combine(temp,"game");var m=Manifest(source,new byte[]{1,2,3,4,5,6});Write(source,m);
  var nested="licenses/"+string.Join("/",Enumerable.Repeat("dependency",12))+"/LICENSE";
  var notice=Path.Combine(source,nested.Replace('/',Path.DirectorySeparatorChar));Directory.CreateDirectory(Path.GetDirectoryName(notice));File.WriteAllText(notice,"notice");
  m.files.Add(new HawkFile{path=nested,size=new FileInfo(notice).Length,sha256=HawkNativeService.Hash(notice)});Write(source,m);
  var service=new HawkNativeService(root,source,args[0],new HawkDeployment(),(s,p)=>{});
  service.Install("",None).GetAwaiter().GetResult();Assert(service.IsInstalled,"clean Windows install without WSL");
  Assert(File.ReadAllText(@"\\?\"+Path.Combine(service.ReleaseDirectory,nested.Replace('/',Path.DirectorySeparatorChar)))=="notice","long runtime paths install without machine-wide policy changes");
  Directory.CreateDirectory(Path.Combine(root,"benilla-config"));File.WriteAllText(Path.Combine(root,"benilla-config","settings"),"player");
  File.WriteAllText(Path.Combine(root,"benilla-config","skate-keyboard.toml"),"push = \"KeyP\"\n");
  var original=File.ReadAllText(Path.Combine(root,"current.json"));m.version="test.2";Write(source,m);File.WriteAllBytes(Path.Combine(source,"bin","benilla.exe"),new byte[]{0});
  Reject(()=>service.Install("",None).GetAwaiter().GetResult(),"corrupt package rejected");Assert(File.ReadAllText(Path.Combine(root,"current.json"))==original,"failed update preserves atomic pointer");
  File.WriteAllBytes(Path.Combine(source,"bin","benilla.exe"),new byte[]{1,2,3,4,5,6});service.Install("",None).GetAwaiter().GetResult();
  Assert(File.ReadAllText(Path.Combine(root,"benilla-config","settings"))=="player","updates preserve settings");
  Assert(File.ReadAllText(Path.Combine(root,"benilla-config","skate-keyboard.toml"))=="push = \"KeyP\"\n","updates preserve custom keyboard bindings");
  Reject(()=>service.ValidateClient(),"non-PE client rejected before launch");
  foreach(var bad in new[]{"../settings","bin/../settings","bin/CON.txt","bin/foo:bar","bin/foo.","/absolute","bin\\file","bin//file"})Reject(()=>HawkNativeService.SafePath(bad),"reject unsafe path "+bad);
  m.files.Add(new HawkFile{path="BIN/BENILLA.EXE",size=6,sha256=m.files[0].sha256});Reject(()=>HawkNativeService.ValidateManifest(JsonConvert.SerializeObject(m)),"case-insensitive duplicate rejected");m.files.RemoveAt(1);
  m.platform="wsl-dml-arch-x86_64";Reject(()=>HawkNativeService.ValidateManifest(JsonConvert.SerializeObject(m)),"Linux runtime rejected");m.platform="windows-x86_64";
  Reject(()=>service.ValidateWow(source),"missing/wrong-version WoW files rejected");
  var wowSource=Path.Combine(temp,"profile-variants");Directory.CreateDirectory(wowSource);var wowFile=Path.Combine(wowSource,"patch-2.MPQ");File.WriteAllText(wowFile,"alternate");
  var wowProfile=Path.Combine(temp,"variants.json");File.WriteAllText(wowProfile,JsonConvert.SerializeObject(new Dictionary<string,HawkFile>{{"patch-2.MPQ",new HawkFile{size=1,sha256=new string('0',64),alternatives=new List<HawkFile>{new HawkFile{size=9,sha256=HawkNativeService.Hash(wowFile)}}}}}));
  var wowGame=new HawkNativeService(Path.Combine(temp,"variant-game"),source,wowProfile,new HawkDeployment(),(s,p)=>{});wowGame.ValidateWow(wowSource);Assert(true,"exact verified MPQ variant accepted");File.WriteAllText(wowFile,"incorrect");Reject(()=>wowGame.ValidateWow(wowSource),"unverified MPQ variant rejected");
  var assets=Path.Combine(temp,"owned-assets");
  foreach(var asset in new[]{"skate-data/assets/private/skater.glb","skate-data/assets/private/game.json","skate-data/assets/private/stock/physics-skeletons.json","skate-data/assets/private/stock/skater-collections.json","skate-audio/pop_1.wav","skate-data/assets/private/stock/"+string.Join("/",Enumerable.Repeat("content",10))+"/owned.bin"}){
   var file=Path.Combine(assets,asset.Replace('/',Path.DirectorySeparatorChar));Directory.CreateDirectory(Path.GetDirectoryName(file));File.WriteAllText(file,"test-only");
  }
  service.ImportSkate(assets,None);service.ValidateSkate(service.AssetDirectory("skate"));Assert(true,"deep owned-asset import and integrity validation");
  var pack=Path.Combine(temp,"SkateContent");Directory.CreateDirectory(pack);
  var entries=new List<HawkFile>();foreach(var file in Directory.GetFiles(assets,"*",SearchOption.AllDirectories)){
   var relative=file.Substring(assets.Length+1).Replace('\\','/');var target=Path.Combine(pack,relative);Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(file,target);
   entries.Add(new HawkFile{path=relative,size=new FileInfo(target).Length,sha256=HawkNativeService.Hash(target)});
  }
  var sm=new HawkManifest{schema=1,kind="skate-content",version="test-1",files=entries};File.WriteAllText(Path.Combine(pack,"manifest.json"),JsonConvert.SerializeObject(sm));
  var managed=new HawkNativeService(Path.Combine(temp,"pack-game"),source,args[0],new HawkDeployment(),(s,p)=>{});
  managed.InstallSkateContent("",None).GetAwaiter().GetResult();managed.ValidateSkate(managed.AssetDirectory("skate"));Assert(managed.HasSkate,"publisher pack installs without importing user assets");
  var first=managed.AssetDirectory("skate");File.WriteAllText(Path.Combine(first,"skate-audio","pop_1.wav"),"damaged");managed.InstallSkateContent("",None,true).GetAwaiter().GetResult();managed.ValidateSkate(managed.AssetDirectory("skate"));Assert(first!=managed.AssetDirectory("skate"),"repair atomically restores publisher assets");
  var owned=service.AssetDirectory("skate");service.InstallSkateContent("",None,true).GetAwaiter().GetResult();Assert(owned==service.AssetDirectory("skate"),"publisher updates preserve user-imported assets");
  sm.files.Add(new HawkFile{path="skate-data/../evil.exe",size=1,sha256=new string('0',64)});Reject(()=>HawkNativeService.ValidateSkateManifest(JsonConvert.SerializeObject(sm)),"unsafe Skate manifest rejected");
  sm.files.RemoveAt(sm.files.Count-1);foreach(var f in sm.files)f.url="https://publisher.invalid/content";
  var sfake=new Fake{json=JsonConvert.SerializeObject(sm),data=System.Text.Encoding.UTF8.GetBytes("test-only")};
  var remoteSkate=new HawkNativeService(Path.Combine(temp,"remote-skate"),source,args[0],new HawkDeployment{skateManifestUrl="https://publisher.invalid/manifest"},(s,p)=>{},()=>new HttpClient(sfake,false));
  remoteSkate.InstallSkateContent("test-token",None).GetAwaiter().GetResult();remoteSkate.ValidateSkate(remoteSkate.AssetDirectory("skate"));Assert(true,"authenticated Skate manifest installs required assets automatically");
  var pfake=new Fake{json=JsonConvert.SerializeObject(sm),data=System.Text.Encoding.UTF8.GetBytes("test-only"),anonymous=true};
  var publicSkate=new HawkNativeService(Path.Combine(temp,"public-skate"),source,args[0],new HawkDeployment{skateManifestUrl="https://publisher.invalid/manifest",publicDownloads=true,publicSessions=true},(s,p)=>{},()=>new HttpClient(pfake,false));
  Assert(!publicSkate.RequiresSignIn,"everyone can install without Discord sign-in");publicSkate.InstallSkateContent("old-saved-token",None).GetAwaiter().GetResult();publicSkate.ValidateSkate(publicSkate.AssetDirectory("skate"));
  m.files.RemoveAt(1);Write(source,m);
  var mirror=new Mirror{data=new byte[]{1,2,3,4,5,6}};var partial=Path.Combine(temp,"asset.part");File.WriteAllBytes(partial,new byte[]{1,2});
  var download=typeof(HawkNativeService).GetMethod("Download",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
  using(var client=new HttpClient(mirror))((Task)download.Invoke(service,new object[]{client,new HawkFile{path="asset.zip",url="https://downloads.stonetavern.app/test.zip",size=6,sha256=m.files[0].sha256},partial,"private-secret",None,true})).GetAwaiter().GetResult();
  Assert(mirror.range==2&&File.ReadAllBytes(partial).Length==6,"public asset download resumes with pinned size and checksum");
  for(int mode=0;mode<3;mode++){
   var remote=Path.Combine(temp,"remote"+mode);Directory.CreateDirectory(Path.Combine(remote,"downloads"));File.WriteAllBytes(Path.Combine(remote,"downloads",m.files[0].sha256+".part"),new byte[]{1,2});
   var fake=new Fake{json=JsonConvert.SerializeObject(m),data=new byte[]{1,2,3,4,5,6},ignoreRange=mode==1,redirect=mode==2};
   var s=new HawkNativeService(remote,source,args[0],new HawkDeployment{manifestUrl="https://publisher.invalid/manifest"},(x,p)=>{},()=>new HttpClient(fake,false));
   if(mode==2)Reject(()=>s.Install("test-token",None).GetAwaiter().GetResult(),"cross-origin redirect refused");else{s.Install("test-token",None).GetAwaiter().GetResult();Assert(fake.seenRange==2,"partial download resumed or safely restarted");}
  }
  Console.WriteLine("Retained test installation: "+temp);
 }
}
