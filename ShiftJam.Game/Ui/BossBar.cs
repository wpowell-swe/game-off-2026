using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftJam
{
    /// <summary>Boss health bar at the bottom of the screen. Screen space (no camera); rectangles only.</summary>
    public static class BossBar
    {
        private const int Width = 200;
        private const int Height = 6;
        private const int BottomMargin = 10;

        public static void Draw(SpriteBatch sb, IBoss boss)
        {
            if (boss == null || !boss.Engaged || boss.IsDefeated || boss.MaxHealth <= 0) return;

            int x = (Metrics.ScreenWidth - Width) / 2;
            int y = Metrics.ScreenHeight - BottomMargin - Height;
            bool astral = boss.Plane == Plane.Astral;
            Color fill = astral ? new Color(90, 200, 255) : new Color(240, 150, 50);
            Color edge = astral ? new Color(30, 40, 110) : new Color(110, 60, 20);

            float f = MathHelper.Clamp(boss.Health / (float)boss.MaxHealth, 0f, 1f);
            int w = boss.Health > 0 ? Math.Max(1, (int)(Width * f)) : 0;

            Gfx.Fill(sb, new Rectangle(x - 2, y - 2, Width + 4, Height + 4), Color.Black);
            Gfx.Fill(sb, new Rectangle(x - 1, y - 1, Width + 2, Height + 2), edge);
            Gfx.Fill(sb, new Rectangle(x, y, Width, Height), new Color(30, 30, 36));
            Gfx.Fill(sb, new Rectangle(x, y, w, Height), fill);
            Gfx.Fill(sb, new Rectangle(x, y, w, 1), Color.Lerp(fill, Color.White, 0.5f));
            // Plane badge at the left end: solid block for Material, hollow for Astral.
            Gfx.Fill(sb, new Rectangle(x - 12, y - 2, 8, Height + 4), Color.Black);
            Gfx.Fill(sb, new Rectangle(x - 11, y - 1, 6, Height + 2), fill);
            if (astral) Gfx.Fill(sb, new Rectangle(x - 10, y + 1, 4, Height - 2), edge);
        }
    }
}
