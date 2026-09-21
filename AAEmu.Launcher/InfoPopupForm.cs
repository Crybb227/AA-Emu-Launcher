using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AAEmu.Launcher
{
    public partial class InfoPopupForm : Form
    {
        public InfoPopupForm()
        {
            InitializeComponent();

            // FormBorderStyle is None, so draw our own visible box outline instead of relying on the OS chrome
            BackColor = WowTheme.PanelAlt;
            lInfo.ForeColor = WowTheme.Text;
            Paint += (s, e) =>
            {
                using (var pen = new Pen(WowTheme.Accent, 2))
                    e.Graphics.DrawRectangle(pen, 1, 1, ClientSize.Width - 3, ClientSize.Height - 3);
            };
        }
    }
}
