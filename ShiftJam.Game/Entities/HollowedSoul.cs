using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftJam
{
    /// <summary>
    /// The Astral half of a Hollowed. Only hittable while its body's guard is broken.
    /// Created by the Hollowed itself; add nothing to the world manually.
    /// </summary>
    public sealed class HollowedSoul : IEntity, IHittable
    {
        // ---- Tuning (private to this class) ----
        private const int StartHealth = 24;       // three light hits (8 damage each)
        private const float BobAmplitude = 2f;
        private const float BobSpeed = 6f;
        private const int BoxHeightDivisor = 2;   // hurtbox covers the body's upper half

        private readonly Hollowed _owner;
        public int Health = StartHealth;
        public int MaxHealth = StartHealth;
        private float _flash;

        public HollowedSoul(Hollowed owner) { _owner = owner; }

        public Plane Plane => Plane.Astral;
        public bool IsAlive => _owner.IsAlive;
        public bool Exposed => _owner.IsAlive && _owner.GuardBroken && Health > 0;

        public Rectangle Hurtbox
        {
            get
            {
                if (!Exposed) return Rectangle.Empty;
                var b = _owner.Hurtbox;
                return new Rectangle(b.X - 2, b.Y, b.Width + 4, b.Height / BoxHeightDivisor + 4);
            }
        }

        public void Update(float dt, GameWorld world)
        {
            if (_flash > 0) _flash -= dt;
        }

        public void TakeHit(in Hit hit)
        {
            if (!Exposed) return;
            Health = Math.Max(0, Health - hit.Damage);   // the body notices Health <= 0 in its Update
            _flash = 0.1f;
        }

        public void Draw(SpriteBatch sb)
        {
            if (!_owner.IsAlive) return;
            var b = _owner.Hurtbox;
            float bob = MathF.Sin(_owner.Clock * BobSpeed) * BobAmplitude;
            int cx = b.Center.X;
            int top = b.Y - 4 + (int)MathF.Round(bob);

            if (_owner.IsFreed)
            {
                // Relieved: the soul drifts up and fades.
                float t = _owner.FreedFraction;
                top -= (int)(t * 18f);
                DrawSoul(sb, cx, top, 1f - t, false);
                return;
            }
            if (!Exposed)
            {
                DrawSoul(sb, cx, top, 0.10f, false);   // barely visible when hidden
                return;
            }
            DrawSoul(sb, cx, top, 1f, _flash > 0);
        }

        private void DrawSoul(SpriteBatch sb, int cx, int top, float a, bool flash)
        {
            if (a <= 0f) return;
            Color core = (flash ? Color.White : new Color(190, 250, 255)) * a;
            Color rim = new Color(70, 190, 255) * a;
            Gfx.Fill(sb, new Rectangle(cx - 6, top + 3, 12, 14), rim);     // glow
            Gfx.Fill(sb, new Rectangle(cx - 4, top, 8, 8), core);          // head
            Gfx.Fill(sb, new Rectangle(cx - 3, top + 8, 6, 10), core);     // body
            Gfx.Fill(sb, new Rectangle(cx - 1, top + 18, 2, 4), rim);      // wisp tail
            Gfx.Fill(sb, new Rectangle(cx - 2, top + 3, 1, 2), Color.Black * a);
            Gfx.Fill(sb, new Rectangle(cx + 1, top + 3, 1, 2), Color.Black * a);
        }
    }
}
