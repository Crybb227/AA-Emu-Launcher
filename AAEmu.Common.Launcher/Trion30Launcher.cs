using System;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using AAEmu.Launcher.Basic;
using AAEmu.Launcher.Trion12;

namespace AAEmu.Launcher.Trion30
{
    [AALauncher("trino_3_0", "Trion 3.0.3", "3.0", "", "20161208")]
    public class Trion_3_0_Launcher: AAEmu.Launcher.Trion12.Trion_1_2_Launcher
    {

        public override bool InitializeForLaunch()
        {
            var res = base.InitializeForLaunch();
            LaunchArguments += " -uid " + UserName;
            return res;
        }

    }
}
