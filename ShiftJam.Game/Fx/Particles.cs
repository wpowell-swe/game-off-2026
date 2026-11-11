using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftJam
{
    /// <summary>
    /// Pooled rectangle particles drawn in world space. Fixed array, no per-frame allocation.
    /// Used for hit sparks; Update must run every frame, even during hit-stop.
    /// </summary>
    public sealed class Particles
    {
        private struct P { public Vector2 Pos, Vel; public float Life, MaxLife; public int Size; public Color Color; }

        private struct Burst { public Vector2 Pos; public float Life, MaxLife, Size; public int Facing; public Color Color; }

        private readonly P[] _pool = new P[256];
        private readonly Burst[] _bursts = new Burst[16];
        private int _nextBurst;
        private int _next;
        private uint _seed = 12345;

        private float Rand() { _seed = _seed * 1664525u + 1013904223u; return (_seed >> 8) / (float)(1 << 24); }

        public void Spawn(Vector2 pos, Vector2 vel, float life, int size, Color color)
        {
            // Ring-buffer reuse: overwrites the oldest slot when full.
            _pool[_next] = new P { Pos = pos, Vel = vel, Life = life, MaxLife = life, Size = size, Color = color };
            _next = (_next + 1) % _pool.Length;
        }

        /// <summary>Burst of sparks. Warm for Material, cyan for Astral. Biased to fly in the facing direction.</summary>
        public void Sparks(Vector2 pos, Plane plane, int count, int facing)
        {
            for (int i = 0; i < count; i++)
            {
                float a = Rand() * MathHelper.TwoPi;
                float speed = Tuning.SparkSpeed * (0.35f + 0.65f * Rand());
                var v = new Vector2(MathF.Cos(a), MathF.Sin(a) * 0.8f - 0.2f) * speed;
                v.X += facing * speed * 0.4f;
                Color c = plane == Plane.Material
                    ? (Rand() < 0.5f ? new Color(255, 220, 120) : new Color(255, 150, 60))
                    : (Rand() < 0.5f ? new Color(150, 240, 255) : new Color(70, 190, 255));
                if (i % 4 == 0) c = Color.White;
                Spawn(pos, v, Tuning.SparkLife * (0.6f + 0.4f * Rand()), Rand() < 0.45f ? 3 : 2, c);
            }
        }

        /// <summary>A bright star-shaped flash where a hit lands: a streak along the swing and a cross, white fading to the plane's colour.</summary>
        public void Impact(Vector2 pos, Plane plane, bool heavy, int facing)
        {
            float size = heavy ? Tuning.ImpactSizeHeavy : Tuning.ImpactSizeLight;
            Color c = plane == Plane.Material ? new Color(255, 190, 80) : new Color(90, 210, 255);
            _bursts[_nextBurst] = new Burst { Pos = pos, Life = Tuning.ImpactTime, MaxLife = Tuning.ImpactTime, Size = size, Facing = facing == 0 ? 1 : facing, Color = c };
            _nextBurst = (_nextBurst + 1) % _bursts.Length;
        }

        public void Update(float dt)
        {
            for (int i = 0; i < _bursts.Length; i++) if (_bursts[i].Life > 0) _bursts[i].Life -= dt;
            float drag = MathF.Max(0f, 1f - 6f * dt);
            for (int i = 0; i < _pool.Length; i++)
            {
                ref P p = ref _pool[i];
                if (p.Life <= 0) continue;
                p.Life -= dt;
                p.Pos += p.Vel * dt;
                p.Vel *= drag;
            }
        }

        public void Draw(SpriteBatch sb)
        {
            for (int i = 0; i < _bursts.Length; i++)
            {
                ref Burst b = ref _bursts[i];
                if (b.Life <= 0) continue;
                float t = b.Life / b.MaxLife;                  // 1 at the moment of impact, 0 when gone
                float grow = 0.8f + 0.4f * (1f - t);           // starts near full size and swells a little as it fades
                int len = Math.Max(4, (int)(b.Size * grow));
                int x = (int)MathF.Round(b.Pos.X), y = (int)MathF.Round(b.Pos.Y);
                Color core = Color.Lerp(b.Color, Color.White, t) * MathF.Min(1f, t * 1.6f);
                Color edge = b.Color * MathF.Min(1f, t * 1.6f);
                int thick = b.Size > 25f ? 3 : 2;
                // Streak along the direction of the hit, a shorter vertical bar, and four diagonal tips.
                Gfx.Fill(sb, new Rectangle(x - len / 2, y - thick / 2, len, thick), core);
                Gfx.Fill(sb, new Rectangle(x - thick / 2, y - len / 3, thick, len * 2 / 3), core);
                for (int k = 3; k <= len / 3 + 1; k += 2)
                {
                    Gfx.Fill(sb, new Rectangle(x + k, y + k, 2, 2), edge); Gfx.Fill(sb, new Rectangle(x - k - 1, y - k - 1, 2, 2), edge);
                    Gfx.Fill(sb, new Rectangle(x + k, y - k - 1, 2, 2), edge); Gfx.Fill(sb, new Rectangle(x - k - 1, y + k, 2, 2), edge);
                }
                Gfx.Fill(sb, new Rectangle(x - 2, y - 2, 4, 4), Color.White * MathF.Min(1f, t * 2f));   // hot centre
            }
            for (int i = 0; i < _pool.Length; i++)
            {
                ref P p = ref _pool[i];
                if (p.Life <= 0) continue;
                float t = p.Life / p.MaxLife;
                int s = t < 0.35f ? 1 : p.Size;
                Gfx.Fill(sb, new Rectangle((int)MathF.Round(p.Pos.X), (int)MathF.Round(p.Pos.Y), s, s), p.Color * MathF.Min(1f, t * 2f));
            }
        }
    }
}
