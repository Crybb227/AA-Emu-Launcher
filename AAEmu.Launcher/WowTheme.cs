using System.Drawing;

namespace AAEmu.Launcher
{
    // Shared WoW-inspired dark/gold color palette used across the launcher's forms.
    internal static class WowTheme
    {
        public static readonly Color Back = Color.FromArgb(15, 12, 8);
        public static readonly Color Panel = Color.FromArgb(35, 28, 18);
        public static readonly Color PanelAlt = Color.FromArgb(24, 19, 12);
        public static readonly Color Accent = Color.FromArgb(200, 165, 90);
        public static readonly Color AccentHot = Color.FromArgb(255, 209, 0);
        public static readonly Color Danger = Color.FromArgb(190, 40, 40);
        public static readonly Color Text = Color.FromArgb(240, 228, 200);
        public static readonly Color MutedText = Color.FromArgb(170, 150, 115);
        public static readonly Color Border = Color.FromArgb(120, 96, 55);
    }
}
