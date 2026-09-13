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
            BackColor = Color.FromArgb(28, 33, 43);
            lInfo.ForeColor = Color.White;
            Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(90, 130, 210), 2))
                    e.Graphics.DrawRectangle(pen, 1, 1, ClientSize.Width - 3, ClientSize.Height - 3);
            };
        }
    }
}
