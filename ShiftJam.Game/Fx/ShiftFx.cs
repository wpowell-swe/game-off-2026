using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftJam
{
    /// <summary>
    /// The plane-shift "commit" moment: an expanding ring of dots in world space around the player
    /// and a fading full-screen flash (white into Material, cold blue into Astral).
    /// </summary>
    public sealed class ShiftFx
    {
        private float _flash, _ring;       // time remaining (0 = inactive)
        private Vector2 _center;
        private Plane _into;

        public void Trigger(Vector2 center, Plane into)
        {
            _center = center; _into = into;
            _flash = Tuning.ShiftFlashTime;
            _ring = Tuning.ShiftRingTime;
        }

        public void Update(float dt)
        {
            if (_flash > 0) _flash -= dt;
            if (_ring > 0) _ring -= dt;
        }

        /// <summary>World space (inside the camera transform).</summary>
        public void DrawWorld(SpriteBatch sb)
        {
            if (_ring <= 0) return;
            float t = 1f - _ring / Tuning.ShiftRingTime;          // 0 to 1
            float eased = 1f - (1f - t) * (1f - t);
            float r = Tuning.ShiftRingRadius * eased;
            float a = 1f - t;
            Color c = _into == Plane.Astral ? new Color(120, 210, 255) : new Color(255, 230, 170);
            int size = t < 0.5f ? 3 : 2;
            int n = Tuning.ShiftRingDots;
            for (int i = 0; i < n; i++)
            {
                float ang = i * MathHelper.TwoPi / n;
                int x = (int)MathF.Round(_center.X + MathF.Cos(ang) * r) - size / 2;
                int y = (int)MathF.Round(_center.Y + MathF.Sin(ang) * r) - size / 2;
                Gfx.Fill(sb, new Rectangle(x, y, size, size), c * a);
            }
        }

        /// <summary>Screen space (no camera).</summary>
        public void DrawScreen(SpriteBatch sb)
        {
            if (_flash <= 0) return;
            float a = _flash / Tuning.ShiftFlashTime;
            Color c = _into == Plane.Astral ? new Color(70, 130, 255) : Color.White;
            Gfx.Fill(sb, new Rectangle(0, 0, Metrics.ScreenWidth, Metrics.ScreenHeight), c * (a * a * Tuning.ShiftFlashAlpha));
        }
    }
}
