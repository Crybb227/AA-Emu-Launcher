using System;
using System.IO;
using System.Threading;
using AAEmu.Launcher;
class HawkWowArchiveSmoke {
 static void Main(string[] args) {
  var root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"JasonGamesLauncher","Build","ClassicImport-"+Guid.NewGuid().ToString("N"));
  var game=new HawkNativeService(root,root,args[1],new HawkDeployment(),(s,p)=>Console.WriteLine(s));
  var expected=HawkNativeService.WowDownloadFile();
  if(new FileInfo(args[0]).Length!=expected.size||HawkNativeService.Hash(args[0])!=expected.sha256)throw new Exception("Unverified downloaded ZIP");
  game.ImportWow(args[0],CancellationToken.None);
  var data=game.AssetDirectory("wow");game.ValidateWow(data);
  if(Directory.GetFiles(data).Length!=12||Directory.GetDirectories(data).Length!=0)throw new Exception("Only required MPQs should be installed");
  foreach(var file in Directory.GetFiles(data))if(!file.EndsWith(".MPQ",StringComparison.OrdinalIgnoreCase))throw new Exception("Non-MPQ asset was copied");
  Console.WriteLine("PASS actual Stonetavern Classic 1.8 ZIP, published checksum, build-5875 profile, safe MPQ-only import. Test installation: "+root);
 }
}
