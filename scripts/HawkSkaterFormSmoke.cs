using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using AAEmu.Launcher;
class HawkSkaterFormSmoke {
 [DllImport("advapi32.dll", EntryPoint="CredDeleteW", CharSet=CharSet.Unicode)] static extern bool DeleteCredential(string target,int type,int flags);
 [STAThread] static void Main(string[] args) {
  Application.EnableVisualStyles();
  var credential=typeof(HawkSkaterForm).Assembly.GetType("AAEmu.Launcher.HawkCredential");
  var target="JasonGamesLauncher/Test/Hawk-"+Guid.NewGuid().ToString("N");
  try {
   credential.GetMethod("Write").Invoke(null,new object[]{target,"Test-only secret"});
   var value=(string)credential.GetMethod("Read").Invoke(null,new object[]{target});
   if(value!="Test-only secret")throw new Exception("Credential Manager round trip failed");
   Console.WriteLine("PASS Windows Credential Manager round trip");
  } finally {DeleteCredential(target,1,0);}
  using(var f=new HawkSkaterForm(true)) {
   f.ShowInTaskbar=false; f.Opacity=0; f.Show(); f.PerformLayout();
   using(var b=new Bitmap(f.Width,f.Height)) { f.DrawToBitmap(b,f.ClientRectangle); b.Save(args[0]); }
   var flags=BindingFlags.NonPublic|BindingFlags.Instance;
   foreach(var field in new[]{"location","source","broker","lifecycleToken"})
    if(typeof(HawkSkaterForm).GetField(field,flags)!=null) throw new Exception("Developer configuration must not appear in the player form");
   var primary=(Button)typeof(HawkSkaterForm).GetField("primary",flags).GetValue(f);
   if(primary.Text!="Install / Play" && primary.Text!="Play")throw new Exception("Missing one-click player action");
   var actions=(FlowLayoutPanel)typeof(HawkSkaterForm).GetField("actions",flags).GetValue(f);
   if(actions.Controls.Count!=4) throw new Exception("Missing player action");
   Console.WriteLine("PASS one-click player form, no developer fields, and render");
  }
 }
}
