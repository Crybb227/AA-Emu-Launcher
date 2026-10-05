using System;
using System.IO;
using System.Threading;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using AAEmu.Launcher;
class HawkPublishedPackSmoke {
 static void Main(string[] args) {
  var root=Path.Combine(Path.GetTempPath(),"HawkPublishedPack-"+Guid.NewGuid().ToString("N"));
  var content=Path.Combine(args[0],"HawkSkater");
  var deployment=args.Length==1?HawkDeployment.Load(Path.Combine(content,"deployment.json")):new HawkDeployment();string token="";
  if(args.Length>1){
   deployment.skateManifestUrl=args[1];token=args[2];
   var expected=new X509Certificate2(args[3]).Thumbprint;
   ServicePointManager.ServerCertificateValidationCallback=(s,c,chain,error)=>c!=null&&string.Equals(c.GetCertHashString(),expected,StringComparison.OrdinalIgnoreCase);
   ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
  }
  var service=new HawkNativeService(root,Path.Combine(content,"Release"),Path.Combine(content,"wow-5875.json"),deployment,(s,p)=>{});
  service.Install("",CancellationToken.None).GetAwaiter().GetResult();service.ValidateClient();
  service.InstallSkateContent(token,CancellationToken.None).GetAwaiter().GetResult();service.ValidateSkate(service.AssetDirectory("skate"));
  Directory.CreateDirectory(Path.Combine(root,"benilla-config"));File.WriteAllText(Path.Combine(root,"benilla-config","settings"),"preserve");
  var prior=service.AssetDirectory("skate");
  File.WriteAllText(Path.Combine(prior,"skate-audio","pop_1.wav"),"damaged");
  service.InstallSkateContent(token,CancellationToken.None,true).GetAwaiter().GetResult();service.ValidateSkate(service.AssetDirectory("skate"));
  if(prior==service.AssetDirectory("skate")||File.ReadAllText(Path.Combine(root,"benilla-config","settings"))!="preserve")throw new Exception("Atomic repair/settings preservation failed");
  Console.WriteLine("PASS actual published native client and 67-file Skate pack clean installation, verification, atomic repair and settings preservation. Evidence: "+root);
 }
}
