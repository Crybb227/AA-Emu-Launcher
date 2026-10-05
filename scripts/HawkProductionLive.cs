// Test-only declared probe identity, never part of the player package.
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using AAEmu.Launcher;
class HawkProductionLive {
 static void Main(string[] args) {
  var root=Path.GetFullPath(args[1]);
  if(Path.GetDirectoryName(root).TrimEnd('\\')!=Path.GetTempPath().TrimEnd('\\')||!System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(root),"^HawkPublishedPack-[a-f0-9]{32}$"))throw new Exception("Use only a clean-install test-owned workspace");
  var content=Path.Combine(args[0],"HawkSkater");
  var deployment=HawkDeployment.Load(Path.Combine(content,"deployment.json"));
  if(!deployment.publicDownloads||!deployment.publicSessions||deployment.lifecycleUrl!="https://games-api.jmservers.com")throw new Exception("Expected reviewed public gateway configuration");
  bool sessionFailure=false;
  var service=new HawkNativeService(root,Path.Combine(content,"Release"),Path.Combine(content,"wow-5875.json"),deployment,(s,p)=>{if(s.StartsWith("Session heartbeat unavailable")||s.StartsWith("Session release unavailable"))sessionFailure=true;if(!s.StartsWith("Verified"))Console.WriteLine(s);});
  service.ImportWow(args[2],CancellationToken.None);
  string account="",password="",character="";
  foreach(var line in File.ReadAllLines(@"\\wsl.localhost\dml-arch\home\dml\games\world-of-skatecraft\.probe-identity")){
   int equals=line.IndexOf('=');if(equals<1)continue;var key=line.Substring(0,equals);var value=line.Substring(equals+1).Trim().Trim('"','\'');
   if(key=="WOW_USER")account=value;if(key=="WOW_PASS")password=value;if(key=="WOW_CHAR")character=value;
  }
  if(account==""||password==""||character=="")throw new Exception("Declared probe account required");
  Environment.SetEnvironmentVariable("RUST_LOG","warn,benilla_app=info,benilla_protocol=info,bevy_render=info");
  Environment.SetEnvironmentVariable("WOW_UNATTENDED","1");Environment.SetEnvironmentVariable("WOW_NOSOUND","1");Environment.SetEnvironmentVariable("WOW_GM","off");Environment.SetEnvironmentVariable("WOW_CHAR",character);Environment.SetEnvironmentVariable("WOW_SKATE_AUTOSTART","5");Environment.SetEnvironmentVariable("WOW_WIN","800x600");
  var observer=Task.Run(async()=>{var deadline=DateTime.UtcNow.AddMinutes(12);while(!service.IsRunning){if(DateTime.UtcNow>deadline)throw new Exception("Client failed to start");await Task.Delay(200);}await Task.Delay(75000);foreach(var process in System.Diagnostics.Process.GetProcessesByName("benilla"))try{if(process.MainModule.FileName.StartsWith(root,StringComparison.OrdinalIgnoreCase))process.CloseMainWindow();}finally{process.Dispose();}});
  service.Launch("",account,password,CancellationToken.None).GetAwaiter().GetResult();observer.GetAwaiter().GetResult();
  var log=File.ReadAllText(Path.Combine(root,"client.log"));
  if(sessionFailure||log.IndexOf("net: in world as",StringComparison.OrdinalIgnoreCase)<0||log.IndexOf("skate: on the board",StringComparison.OrdinalIgnoreCase)<0||log.IndexOf("panicked",StringComparison.OrdinalIgnoreCase)>=0||File.Exists(Path.Combine(root,".session.json")))throw new Exception("Production gameplay/cleanup not proven; inspect test log");
  Console.WriteLine("PASS public HTTPS launch, native world entry with Classic 1.8 MPQs and the reduced Skate pack, board activation and clean client lease release. Evidence: "+root);
 }
}
