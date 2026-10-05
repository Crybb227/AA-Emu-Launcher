using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace AAEmu.Launcher
{
    // Per-game home/splash backgrounds. No final artwork is bundled yet.
    internal static class GameSplash
    {
        internal static Image Load(string gameId)
        {
            string folder;
            switch (gameId)
            {
                case "jw": folder = "jwow"; break;
                case "aa": folder = "archeage-1.2"; break;
                case "aa30": folder = "archeage-3.0.3"; break;
                case "hawk": folder = "hawkskater"; break;
                default: throw new ArgumentOutOfRangeException(nameof(gameId));
            }
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Res", "Splashes", folder, "splash.png");
            try
            {
                if (!File.Exists(path)) return null;
                using (var source = Image.FromFile(path))
                    return new Bitmap(source); // Release the file so artwork can be replaced.
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException ||
                                       ex is ArgumentException || ex is OutOfMemoryException)
            {
                Trace.WriteLine("Cannot load splash " + path + ": " + ex.Message);
                return null;
            }
        }

        internal static void Paint(Graphics graphics, Rectangle bounds, Image image, bool allowUpscale)
        {
            using (var brush = new LinearGradientBrush(bounds, Color.FromArgb(18, 22, 52),
                Color.FromArgb(58, 36, 64), 90F))
                graphics.FillRectangle(brush, bounds);
            if (image == null) return;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            if (allowUpscale)
            {
                // Full-bleed cover for artwork with enough source resolution.
                float scale = Math.Max((float)bounds.Width / image.Width, (float)bounds.Height / image.Height);
                float width = bounds.Width / scale, height = bounds.Height / scale;
                graphics.DrawImage(image, bounds, (image.Width - width) / 2, (image.Height - height) / 2,
                    width, height, GraphicsUnit.Pixel);
            }
            else
            {
                // The legacy ArcheAge art is 573x243. Keep it at native size so Windows never magnifies it.
                float scale = Math.Min(1F, Math.Min((float)bounds.Width / image.Width, (float)bounds.Height / image.Height));
                var target = new RectangleF(
                    bounds.Left + (bounds.Width - image.Width * scale) / 2F,
                    bounds.Top + (bounds.Height - image.Height * scale) / 2F,
                    image.Width * scale,
                    image.Height * scale);
                graphics.DrawImage(image, target);
            }
        }
    }
}
