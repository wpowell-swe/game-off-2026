using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftJam
{
    /// <summary>Health and Ki bars plus a plane indicator, drawn in screen space (no camera). Rectangles only.</summary>
    public static class Hud
    {
        public static void Draw(SpriteBatch sb, Player player)
        {
            bool astral = player.Plane == Plane.Astral;

            if (astral && !Art.Stretch(sb, "Ui/hud_astral_edge", new Rectangle(0, 0, Metrics.ScreenWidth, Metrics.ScreenHeight)))
            {
                // Faint cold tint around the screen edges.
                var tint = new Color(60, 140, 255) * 0.12f;
                const int e = 6;
                Gfx.Fill(sb, new Rectangle(0, 0, Metrics.ScreenWidth, e), tint);
                Gfx.Fill(sb, new Rectangle(0, Metrics.ScreenHeight - e, Metrics.ScreenWidth, e), tint);
                Gfx.Fill(sb, new Rectangle(0, e, e, Metrics.ScreenHeight - 2 * e), tint);
                Gfx.Fill(sb, new Rectangle(Metrics.ScreenWidth - e, e, e, Metrics.ScreenHeight - 2 * e), tint);
            }

            Bar(sb, 8, 8, 80, 6, player.Health / (float)Math.Max(1, player.MaxHealth), Color.Crimson, "Ui/hud_health_frame");
            Bar(sb, 8, 17, 80, 6, player.Ki / Math.Max(1f, player.MaxKi), Color.DeepSkyBlue, "Ui/hud_ki_frame");

            PlaneBadge(sb, 8, 26, astral);
            int gx = 8;
            foreach (var _ in Charms.Held())
            {
                if (!Art.At(sb, "Ui/hud_charm", gx, 38))
                {
                    Gfx.Fill(sb, new Rectangle(gx, 38, 5, 5), Color.Black);
                    Gfx.Fill(sb, new Rectangle(gx + 1, 39, 3, 3), new Color(255, 210, 100));
                }
                gx += 7;
            }
        }

        // A frame PNG (w+2 x h+2) sits over the bar; its transparent middle shows the fill. Without one the bar is plain rectangles.
        private static void Bar(SpriteBatch sb, int x, int y, int w, int h, float fill, Color c, string frame)
        {
            bool art = Art.Has(frame);
            if (!art) Gfx.Fill(sb, new Rectangle(x - 1, y - 1, w + 2, h + 2), Color.Black);
            Gfx.Fill(sb, new Rectangle(x, y, w, h), new Color(40, 40, 40));
            Gfx.Fill(sb, new Rectangle(x, y, (int)(w * MathHelper.Clamp(fill, 0, 1)), h), c);
            if (art) Art.At(sb, frame, x - 1, y - 1);
        }

        /// <summary>Warm sun-like block for Material, cold diamond-ish block for Astral.</summary>
        private static void PlaneBadge(SpriteBatch sb, int x, int y, bool astral)
        {
            if (Art.At(sb, astral ? "Ui/hud_astral" : "Ui/hud_material", x - 1, y - 1)) return;
            Gfx.Fill(sb, new Rectangle(x - 1, y - 1, 26, 12), Color.Black);
            if (!astral)
            {
                Gfx.Fill(sb, new Rectangle(x, y, 24, 10), new Color(150, 80, 30));
                Gfx.Fill(sb, new Rectangle(x + 2, y + 2, 20, 6), new Color(240, 170, 60));
                // Solid centre square: grounded, solid.
                Gfx.Fill(sb, new Rectangle(x + 9, y + 1, 6, 8), new Color(255, 225, 140));
            }
            else
            {
                Gfx.Fill(sb, new Rectangle(x, y, 24, 10), new Color(30, 40, 110));
                Gfx.Fill(sb, new Rectangle(x + 2, y + 2, 20, 6), new Color(90, 190, 255));
                // Hollow centre: translucent, airy.
                Gfx.Fill(sb, new Rectangle(x + 9, y + 3, 6, 4), new Color(30, 40, 110));
            }
        }
    }
}
