using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace AAEmu.Launcher
{
    internal sealed class PresentationPanel : Panel
    {
        public PresentationPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }
    }

    public partial class LauncherForm
    {
        private Timer splashTransitionTimer;
        private readonly Stopwatch splashTransitionClock = new Stopwatch();
        private Bitmap previousSplashFrame;
        private RectangleF previousGameIndicator;
        private float splashTransitionProgress = 1;
        private float presentationScale = 1F;
        private int presentationDpi = 96;
        private bool presentationDpiInitialized;

        private int Px(int value) => (int)Math.Round(value * presentationScale);

        private Rectangle SplashBounds => new Rectangle(Px(296), Px(122),
            ClientSize.Width - Px(296), ClientSize.Height - Px(122));

        private RectangleF GameIndicator
        {
            get
            {
                var tab = IsHawkSelected ? lGamePlaceholder : selectedGameId == "jw" ? lGameJasonWoW : selectedGameId == "aa30" ? lGameArcheAge30 : lGameArcheAge;
                var target = new RectangleF(tab.Left, 119, tab.Width, 3);
                float t = splashTransitionProgress;
                return new RectangleF(previousGameIndicator.X + (target.X - previousGameIndicator.X) * t, Px(119),
                    previousGameIndicator.Width + (target.Width - previousGameIndicator.Width) * t, Math.Max(1, Px(3)));
            }
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            // Initial DPI negotiation can occur while InitializeComponent/ApplyModernTheme is still
            // replacing the legacy layout. OnShown scales the completed control tree once.
            if (presentationDpiInitialized)
                ApplyPresentationDpi(e.DeviceDpiNew);
        }

        private void ApplyPresentationDpi(int dpi)
        {
            if (dpi <= 0) dpi = 96;
            if (presentationDpiInitialized && dpi == presentationDpi) return;

            float ratio = dpi / (float)presentationDpi;
            var scaledBounds = new Dictionary<Control, Rectangle>();
            CaptureScaledBounds(this, ratio, scaledBounds);

            SuspendLayout();
            ClientSize = new Size((int)Math.Round(1280 * dpi / 96F),
                (int)Math.Round(720 * dpi / 96F));
            foreach (var item in scaledBounds)
                item.Key.Bounds = item.Value;
            ResumeLayout(true);

            presentationDpi = dpi;
            presentationScale = dpi / 96F;
            presentationDpiInitialized = true;
            CenterToScreen();
            Invalidate(true);
        }

        private static void CaptureScaledBounds(Control parent, float ratio,
            IDictionary<Control, Rectangle> bounds)
        {
            foreach (Control child in parent.Controls)
            {
                var current = child.Bounds;
                bounds[child] = new Rectangle(
                    (int)Math.Round(current.X * ratio), (int)Math.Round(current.Y * ratio),
                    (int)Math.Round(current.Width * ratio), (int)Math.Round(current.Height * ratio));
                CaptureScaledBounds(child, ratio, bounds);
            }
        }

        private void ChangeGameSplash(string gameId)
        {
            // Snapshot the current blend so repeated clicks never jump back to an old frame.
            Bitmap snapshot = null;
            var indicator = GameIndicator;
            if (IsHandleCreated && gameId != selectedGameId && SystemInformation.IsMenuAnimationEnabled)
            {
                snapshot = new Bitmap(SplashBounds.Width, SplashBounds.Height);
                using (var graphics = Graphics.FromImage(snapshot))
                    PaintGameSplash(graphics, new Rectangle(Point.Empty, snapshot.Size));
            }
            previousSplashFrame?.Dispose();
            previousSplashFrame = snapshot;
            gameSplash?.Dispose();
            gameSplash = GameSplash.Load(gameId);
            previousGameIndicator = indicator;
            splashTransitionProgress = snapshot == null ? 1 : 0;
            if (snapshot == null)
            {
                splashTransitionTimer?.Stop();
                return;
            }
            if (splashTransitionTimer == null)
            {
                splashTransitionTimer = new Timer(components) { Interval = 16 };
                splashTransitionTimer.Tick += (sender, args) => AdvanceSplashTransition();
            }
            splashTransitionClock.Restart();
            splashTransitionTimer.Start();
        }

        private void AdvanceSplashTransition()
        {
            float t = Math.Min(1, splashTransitionClock.ElapsedMilliseconds / 280f);
            splashTransitionProgress = t * t * (3 - 2 * t);
            if (t >= 1)
            {
                splashTransitionTimer.Stop();
                previousSplashFrame?.Dispose();
                previousSplashFrame = null;
            }
            Invalidate(true);
        }

        private void PaintGameSplash(Graphics graphics, Rectangle bounds)
        {
            GameSplash.Paint(graphics, bounds, gameSplash, selectedGameId == "jw" || IsHawkSelected);
            if (IsHawkSelected && gameSplash == null)
            {
                float scale = bounds.Width / 984F;
                using (var title = new Font("Palatino Linotype", 34F * scale, FontStyle.Bold))
                using (var subtitle = new Font("Segoe UI", 13F * scale))
                using (var gold = new SolidBrush(WowTheme.AccentHot))
                using (var muted = new SolidBrush(WowTheme.MutedText))
                {
                    graphics.DrawString("JasonHawkSkater", title, gold, bounds.Left + 60 * scale, bounds.Top + 96 * scale);
                    graphics.DrawString("SKATE THROUGH AZEROTH", subtitle, muted, bounds.Left + 64 * scale, bounds.Top + 164 * scale);
                }
                using (var line = new Pen(Color.FromArgb(130, WowTheme.Accent), 3 * scale))
                {
                    graphics.DrawBezier(line, bounds.Left + 65 * scale, bounds.Top + 235 * scale, bounds.Left + 140 * scale, bounds.Top + 285 * scale,
                        bounds.Left + 390 * scale, bounds.Top + 285 * scale, bounds.Left + 470 * scale, bounds.Top + 235 * scale);
                    graphics.DrawEllipse(line, bounds.Left + 150 * scale, bounds.Top + 275 * scale, 18 * scale, 18 * scale);
                    graphics.DrawEllipse(line, bounds.Left + 365 * scale, bounds.Top + 275 * scale, 18 * scale, 18 * scale);
                }
            }
            if (previousSplashFrame == null) return;
            using (var attributes = new ImageAttributes())
            {
                var matrix = new ColorMatrix { Matrix33 = 1 - splashTransitionProgress };
                attributes.SetColorMatrix(matrix);
                graphics.DrawImage(previousSplashFrame, bounds, 0, 0, previousSplashFrame.Width,
                    previousSplashFrame.Height, GraphicsUnit.Pixel, attributes);
            }
        }
    }
}
