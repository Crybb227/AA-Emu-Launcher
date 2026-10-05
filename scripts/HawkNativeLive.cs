using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using AAEmu.Launcher;
class HawkNativeLive {
 static void Main(string[] args){
  var work=args[0];var root=Path.Combine(work,"game");
  var expected=new X509Certificate2(Path.Combine(work,"cert.cer")).Thumbprint;
  ServicePointManager.ServerCertificateValidationCallback=(s,c,chain,error)=>c!=null&&string.Equals(c.GetCertHashString(),expected,StringComparison.OrdinalIgnoreCase);
  ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
  string account="",password="",character="";
  foreach(var line in File.ReadAllLines(@"\\wsl.localhost\dml-arch\home\dml\games\world-of-skatecraft\.probe-identity")){
   int equals=line.IndexOf('=');if(equals<1)continue;var key=line.Substring(0,equals);var value=line.Substring(equals+1).Trim().Trim('"','\'');
   if(key=="WOW_USER")account=value;if(key=="WOW_PASS")password=value;if(key=="WOW_CHAR")character=value;
  }
  if(account==""||password==""||character=="")throw new Exception("Declared probe identity is required; never substitute a player account");
  Environment.SetEnvironmentVariable("RUST_LOG","warn,benilla_app=info,benilla_protocol=info,bevy_render=info");
  Environment.SetEnvironmentVariable("WOW_UNATTENDED","1");Environment.SetEnvironmentVariable("WOW_NOSOUND","1");Environment.SetEnvironmentVariable("WOW_GM","off");Environment.SetEnvironmentVariable("WOW_CHAR",character);Environment.SetEnvironmentVariable("WOW_SKATE_AUTOSTART","5");Environment.SetEnvironmentVariable("WOW_SKATE_DEBUG","1");Environment.SetEnvironmentVariable("WOW_WIN","800x600");
  var url="https://localhost:"+File.ReadAllText(Path.Combine(work,"port"));var token=File.ReadAllText(Path.Combine(work,"token")).Trim();
  string lastStatus=null;
  var service=new HawkNativeService(root,args[1],args[2],new HawkDeployment{lifecycleUrl=url},(s,p)=>{if(!s.StartsWith("Verified")&&s!=lastStatus){Console.WriteLine(s);lastStatus=s;}});
  service.Install(token,CancellationToken.None).GetAwaiter().GetResult();
  if(!File.Exists(Path.Combine(root,"wow.json")))service.ImportWow(@"C:\Users\jsnmu\Downloads\Stonetavern-Enhanced-1.12.1-v1.4\Data",CancellationToken.None);
  service.ImportSkate(args[3],CancellationToken.None);
  // Close only this test-owned client; no game/server/user processes are stopped.
  var observer=Task.Run(async()=>{var deadline=DateTime.UtcNow.AddMinutes(10);while(!service.IsRunning){if(DateTime.UtcNow>deadline)throw new Exception("Client did not start");await Task.Delay(200);}await Task.Delay(75000);foreach(var process in System.Diagnostics.Process.GetProcessesByName("benilla"))try{if(process.MainModule.FileName.StartsWith(root,StringComparison.OrdinalIgnoreCase))process.CloseMainWindow();}finally{process.Dispose();}});
  service.Launch(token,account,password,CancellationToken.None).GetAwaiter().GetResult();observer.GetAwaiter().GetResult();
  var log=File.ReadAllText(Path.Combine(root,"client.log"));
  if(log.IndexOf("net: in world as",StringComparison.OrdinalIgnoreCase)<0)throw new Exception("Native live world entry was not proven; inspect client.log");
  if(log.IndexOf("skate: on the board",StringComparison.OrdinalIgnoreCase)<0)throw new Exception("Skateboard activation was not proven");
  if(log.IndexOf("panicked",StringComparison.OrdinalIgnoreCase)>=0)throw new Exception("Native client panic");
  var counts=JObject.Parse(File.ReadAllText(Path.Combine(work,"counts.json")));
  if((int)counts["attach"]<1||(int)counts["heartbeat"]<2||(int)counts["release"]<1||File.ReadAllText(Path.Combine(work,"sessions.json")).Trim()!="{}"||File.Exists(Path.Combine(root,".session.json")))throw new Exception("Session heartbeat/release not proven");
  Console.WriteLine("PASS native Windows world entry, skateboard activation, pinned-certificate HTTPS attach, heartbeats and release. Log: "+Path.Combine(root,"client.log"));
 }
}
