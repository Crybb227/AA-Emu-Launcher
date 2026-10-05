using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using AAEmu.Launcher;
class Preview : LauncherForm {
 protected override void OnLoad(EventArgs e) { }
 protected override void OnFormClosing(FormClosingEventArgs e) { }
}
class Check {
 static BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
 static object Field(object f,string n) {return typeof(LauncherForm).GetField(n,flags).GetValue(f);}
 static void Call(object f,string n,params object[] a) {typeof(LauncherForm).GetMethod(n,flags).Invoke(f,a);}
 static void Assert(bool ok,string msg) {if(!ok)throw new Exception(msg); Console.WriteLine("PASS " + msg);}
 static void Render(Control c, Graphics g) {
  if(!c.Visible)return;
  g.SetClip(c.ClientRectangle,System.Drawing.Drawing2D.CombineMode.Intersect);
  if(c.HasChildren) {
   typeof(Control).GetMethod("OnPaintBackground",flags).Invoke(c,new object[]{new PaintEventArgs(g,c.ClientRectangle)});
   typeof(Control).GetMethod("OnPaint",flags).Invoke(c,new object[]{new PaintEventArgs(g,c.ClientRectangle)});
   for(int i=c.Controls.Count-1;i>=0;i--) {var child=c.Controls[i];var state=g.Save();g.TranslateTransform(child.Left,child.Top);Render(child,g);g.Restore(state);}
  } else if(c is Label) {
   typeof(Control).GetMethod("OnPaintBackground",flags).Invoke(c,new object[]{new PaintEventArgs(g,c.ClientRectangle)});
   typeof(Control).GetMethod("OnPaint",flags).Invoke(c,new object[]{new PaintEventArgs(g,c.ClientRectangle)});
  } else if(!(c is WebBrowser)) {using(var bmp=new Bitmap(c.Width,c.Height)){c.DrawToBitmap(bmp,c.ClientRectangle);g.DrawImageUnscaled(bmp,0,0);}}
 }
 static void Save(Control f,string path) {using(var b=new Bitmap(f.Width,f.Height)){using(var g=Graphics.FromImage(b)){Render(f,g);}b.Save(path);}}
 [STAThread] static void Main() {
  Application.EnableVisualStyles();
  using(var f=new Preview()) {
   ((Timer)Field(f,"timerGeneral")).Stop();
   Call(f,"InitDefaultLanguage");
   Assert((string)Field(f,"selectedGameId")=="jw","JasonWoW starts selected");
   var jw=(Control)Field(f,"lGameJasonWoW"); var aa=(Control)Field(f,"lGameArcheAge");
   Assert(jw.Left<aa.Left && jw.Width>aa.Width && jw.Text=="JasonWoW","JasonWoW first, named correctly, and larger");
   var hawk=(Control)Field(f,"lGamePlaceholder");
   var settings=Field(f,"Setting"); var type=settings.GetType();
   type.GetProperty("WoWPath").SetValue(settings,@"C:\test-jwow\Wow.exe");
   type.GetProperty("AAPath").SetValue(settings,@"C:\test-aa\archeage.exe");
   type.GetProperty("AA30Path").SetValue(settings,@"C:\test-aa30\archeage.exe");
   var addonTestPath=Path.Combine(Environment.CurrentDirectory,"addon-count-test");
   Directory.CreateDirectory(Path.Combine(addonTestPath,"Blizzard_AchievementUI"));
   Directory.CreateDirectory(Path.Combine(addonTestPath,"MultiPart_Core"));
   Directory.CreateDirectory(Path.Combine(addonTestPath,"MultiPart_Config"));
   Directory.CreateDirectory(Path.Combine(addonTestPath,"ManualAddon"));
   AddonManager.SaveManifest(addonTestPath,new AddonManifest { Addons=new System.Collections.Generic.List<InstalledAddon> {
    new InstalledAddon { Name="MultiPart", Repo="owner/repo", Version="1", Folders=new System.Collections.Generic.List<string>{"MultiPart_Core","MultiPart_Config"} }
   }});
   type.GetProperty("WoWAddOnsPath").SetValue(settings,addonTestPath);
   Call(f,"LoadSelectedGameFields");
   f.Show();
   Assert(hawk.Visible && hawk.Text=="JasonHawkSkater" && hawk.Left>aa.Left,"JasonHawkSkater entry is available");
   var windowCount=Application.OpenForms.Count;
   Call(f,"SelectLauncherGame","hawk",true);
   Assert((string)Field(f,"selectedGameId")=="hawk" && Application.OpenForms.Count==windowCount,"Hawk selects in-place without a separate window");
   Assert(!((Control)Field(f,"eLogin")).Visible && ((Control)Field(f,"rowAddons")).Visible,"Hawk dashboard hides legacy login and shows asset management");
   Assert(((Control)Field(f,"btnPlay")).Top==((Control)Field(f,"rowAddons")).Top-101,"Hawk uses normal sidebar Play layout");
   Assert(((Control)Field(f,"lPopulation")).Text=="Keyboard + Xbox / XInput","Hawk advertises both input methods");
   var controlsText=(Label)Field(f,"lHeroNewsBody");
   Assert(controlsText.Text.Contains("W/S: push/brake") && controlsText.Text.Contains("Space: ollie") && controlsText.Text.Contains("R: recover"),"Hawk keyboard controls are visible in the normal dashboard");
   Assert(TextRenderer.MeasureText(controlsText.Text,controlsText.Font,new Size(controlsText.Width,int.MaxValue),TextFormatFlags.WordBreak).Height<=controlsText.Height,"Hawk controls fit without clipping");
   System.Threading.Thread.Sleep(300);Call(f,"AdvanceSplashTransition");
   Save(f,Path.Combine(Environment.CurrentDirectory,"hawk-integrated.png"));
   Call(f,"SelectLauncherGame","jw",true);
   Assert(((ToolTip)Field(f,"dashboardToolTip")).GetToolTip((Control)Field(f,"lPopulation")).Contains("JasonWoW status"),"JasonWoW tooltip restored after keyboard controls");
   Assert((string)type.GetProperty("AAPath").GetValue(settings)==@"C:\test-aa\archeage.exe" && (string)type.GetProperty("WoWPath").GetValue(settings)==@"C:\test-jwow\Wow.exe","Hawk selection preserves other games' locations");
   Application.DoEvents();
   Call(f,"ApplyPresentationDpi",f.DeviceDpi);
   var dpi=(int)typeof(Control).GetProperty("DeviceDpi",flags).GetValue(f);
   Console.WriteLine("INFO presentation DPI " + dpi + ", client " + f.ClientSize.Width + "x" + f.ClientSize.Height
       + ", applied DPI " + Field(f,"presentationDpi") + ", initialized " + Field(f,"presentationDpiInitialized"));
   Assert(f.ClientSize.Width==(int)Math.Round(1280*dpi/96f),"window width follows monitor DPI");
   Assert(((Control)Field(f,"btnPlay")).Height==(int)Math.Round(52*dpi/96f),"control bounds follow monitor DPI");
   Call(f,"UpdateAddonSummary");
   var addonRow=Field(f,"rowAddons");
   var addonValue=(Label)addonRow.GetType().GetField("Value",flags).GetValue(addonRow);
   Assert(addonValue.Text.StartsWith("2 installed"),"dashboard and manager use the same logical addon count");
   var statusType=typeof(LauncherForm).Assembly.GetType("AAEmu.Launcher.RealmStatusPayload");
   var playerType=typeof(LauncherForm).Assembly.GetType("AAEmu.Launcher.RealmPlayerPayload");
   var status=Activator.CreateInstance(statusType); var player=Activator.CreateInstance(playerType);
   statusType.GetProperty("Online").SetValue(status,(bool?)true); statusType.GetProperty("Phase").SetValue(status,"Phase I");
   statusType.GetProperty("LevelCap").SetValue(status,(int?)20); statusType.GetProperty("PlayerbotCount").SetValue(status,(int?)1972);
   playerType.GetProperty("Name").SetValue(player,"Jason"); playerType.GetProperty("Race").SetValue(player,"Human");
   playerType.GetProperty("Class").SetValue(player,"Mage"); playerType.GetProperty("Level").SetValue(player,(int?)20);
   playerType.GetProperty("Location").SetValue(player,"Stormwind");
   var playerList=Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(playerType));
   playerList.GetType().GetMethod("Add").Invoke(playerList,new[]{player}); statusType.GetProperty("HumanPlayers").SetValue(status,playerList);
   Call(f,"ApplyRealmDetails",status);
   Assert(((Label)Field(f,"lRealmStatus")).Text.Contains("REALM ONLINE"),"launcher status API online state");
   Assert(((Label)Field(f,"lRealmProgression")).Text=="Phase I  •  Level Cap 20","launcher status API progression");
   Assert(((Label)Field(f,"lPopulation")).Text.Contains("1 Player Online") && ((Label)Field(f,"lPopulation")).Text.Contains("1972 Adventurers"),"launcher status API separates humans and Playerbots");
   foreach(var testDpi in new[]{120,144}) {
    Call(f,"ApplyPresentationDpi",testDpi);
    Assert(f.ClientSize.Width==(int)Math.Round(1280*testDpi/96f),testDpi+" DPI window width");
    Assert(((Control)Field(f,"btnPlay")).Height==(int)Math.Round(52*testDpi/96f),testDpi+" DPI control scaling");
    Call(f,"SelectLauncherGame","aa",true);
    Assert(((Control)Field(f,"btnPlay")).Top==(int)Math.Round(414*testDpi/96f),testDpi+" DPI AA relayout");
    Call(f,"SelectLauncherGame","jw",true);
    Assert(((Control)Field(f,"btnPlay")).Top==(int)Math.Round(333*testDpi/96f),testDpi+" DPI JasonWoW relayout after AA");
    Assert(((Control)Field(f,"rowAddons")).Top==(int)Math.Round(434*testDpi/96f),testDpi+" DPI addon relayout after AA");
   }
   Call(f,"ApplyPresentationDpi",dpi);
   foreach(var id in new[]{"jw","aa","aa30","jw"}) {
    Call(f,"SelectLauncherGame",id,true);
    Assert(((Control)Field(f,"btnPlay")).Visible,id+" play/install visible");
    Assert(((Control)Field(f,"eLogin")).Visible==(id!="jw"),id+" credentials visibility");
    Assert(((Control)Field(f,"rowAddons")).Visible==(id=="jw"),id+" addon visibility");
    var path=((Control)Field(f,"lGamePath")).Text;
    Assert(path.Contains(id=="jw"?"test-jwow":id=="aa"?"test-aa\\":"test-aa30"),id+" retains own path");
    System.Threading.Thread.Sleep(300);
    Call(f,"AdvanceSplashTransition");
    Save(f,id+".png");
   }
   for(int i=0;i<12;i++) Call(f,"SelectLauncherGame",i%2==0?"aa":"jw",true);
   System.Threading.Thread.Sleep(300);
   Call(f,"AdvanceSplashTransition");
   Assert(Field(f,"previousSplashFrame")==null,"rapid switches release transition snapshot");
   Assert((float)Field(f,"splashTransitionProgress")==1,"transition completes");
   Assert((string)Field(f,"selectedGameId")=="jw","rapid switches settle on JWoW");
   Assert(Field(f,"splashTransitionTimer")==null || !((Timer)Field(f,"splashTransitionTimer")).Enabled,"transition timer stops when finished");
   Assert(LauncherForm.urlLauncherGitHub=="https://github.com/Crybb227/AA-Emu-Launcher","Website repo destination");
   Call(f,"ShowPanelControls",LauncherForm.ShowPanelType.Settings);
   Save(f,"settings.png");
   Assert((string)type.GetProperty("AAPath").GetValue(settings)==@"C:\test-aa\archeage.exe","AA path preserved after round trip");
   f.Hide();
  }
  using(var addons=new WowAddonManagerForm(Path.Combine(Environment.CurrentDirectory,"addon-count-test"))) {
   addons.Show(); Application.DoEvents(); Save(addons,"addon-manager.png");
   var tabs=(TabControl)typeof(WowAddonManagerForm).GetField("tabs",flags).GetValue(addons);
   var addonList=(ListView)typeof(WowAddonManagerForm).GetField("lvAddons",flags).GetValue(addons);
   var initialListWidth=addonList.Width;
   Assert(tabs.TabPages[0].Text=="Installed (2)","addon manager shows the reconciled logical count");
   Assert(addons.FormBorderStyle==FormBorderStyle.Sizable && addons.MaximizeBox,"addon manager is resizable");
   addons.ClientSize=new Size(1200,800); Application.DoEvents();
   Assert(addonList.Width>initialListWidth && addonList.Height>280,"addon manager content expands with the window");
   Save(addons,"addon-manager-large.png");
   addons.Hide();
  }
  var splash=typeof(LauncherForm).Assembly.GetType("AAEmu.Launcher.GameSplash");
  var load=splash.GetMethod("Load",BindingFlags.Static|BindingFlags.NonPublic);
  foreach(var pair in new[]{new[]{"jw","jwow"},new[]{"aa","archeage-1.2"},new[]{"aa30","archeage-3.0.3"}}) {
   var path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Res","Splashes",pair[1],"splash.png");
   Directory.CreateDirectory(Path.GetDirectoryName(path));
   byte[] original=File.Exists(path)?File.ReadAllBytes(path):null;
   if(original!=null)File.Delete(path);
   Assert(load.Invoke(null,new object[]{pair[0]})==null,pair[0]+" missing art fallback");
   File.WriteAllText(path,"not an image");
   Assert(load.Invoke(null,new object[]{pair[0]})==null,pair[0]+" corrupt art fallback");
   using(var source=new Bitmap(100,80)){source.SetPixel(0,0,Color.Red);source.Save(path,System.Drawing.Imaging.ImageFormat.Png);}
   using(var loaded=(Image)load.Invoke(null,new object[]{pair[0]})) {
    Assert(loaded.Width==100 && loaded.Height==80,pair[0]+" loads correct asset");
    using(var stream=new FileStream(path,FileMode.Open,FileAccess.ReadWrite,FileShare.None)) Assert(true,pair[0]+" artwork file unlocked");
   }
   File.Delete(path);
   if(original!=null)File.WriteAllBytes(path,original);
  }
 }
}
