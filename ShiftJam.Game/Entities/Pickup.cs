using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftJam
{
    public enum PickupKind { Ki, Health, Shard }

    /// <summary>A bobbing orb on the floor. Walk into it to restore Ki or health; it is used up once.</summary>
    public sealed class Pickup : IEntity
    {
        private const int KiAmount = 50;
        private const int HealthAmount = 30;
        private const int Size = 10;

        private readonly PickupKind _kind;
        private readonly Vector2 _home;
        private float _clock;
        private bool _taken;

        public PickupKind Kind => _kind;

        /// <summary>position is the tile's top-left; the orb sits on the bottom of the tile.</summary>
        public Pickup(PickupKind kind, Vector2 tile)
        {
            _kind = kind;
            _home = new Vector2(tile.X + (Metrics.TileSize - Size) / 2f, tile.Y + Metrics.TileSize - Size - 2);
            _clock = tile.X * 0.05f;   // desync the bobbing between orbs
        }

        public void Update(float dt, GameWorld world)
        {
            if (_taken) return;
            _clock += dt;
            var p = world.Player;
            if (p == null || !p.IsAlive || !p.Hurtbox.Intersects(Bounds())) return;

            if (_kind == PickupKind.Ki) p.Ki = Math.Min(p.MaxKi, p.Ki + KiAmount);
            else if (_kind == PickupKind.Health) p.Health = Math.Min(p.MaxHealth, p.Health + HealthAmount);
            _taken = true;
            Sfx.Play(SfxId.ShiftIn, 0.6f, 0.2f);
            world.Sparks.Sparks(_home + new Vector2(Size / 2f), p.Plane, 8, 0);
            world.Remove(this);
        }

        private Rectangle Bounds()
        {
            int bob = (int)MathF.Round(MathF.Sin(_clock * 4f) * 2f);
            return new Rectangle((int)_home.X, (int)_home.Y + bob, Size, Size);
        }

        public void Draw(SpriteBatch sb)
        {
            if (_taken) return;
            var b = Bounds();
            if (_kind == PickupKind.Ki)
            {
                Gfx.Fill(sb, new Rectangle(b.X - 2, b.Y - 2, Size + 4, Size + 4), new Color(70, 190, 255) * 0.25f);
                // A diamond, built from stacked rows that widen to the middle and narrow again.
                var c = new Color(120, 220, 255);
                int[] widths = { 2, 4, 6, 8, 10, 10, 8, 6, 4, 2 };
                for (int i = 0; i < widths.Length; i++)
                    Gfx.Fill(sb, new Rectangle(b.X + (Size - widths[i]) / 2, b.Y + i, widths[i], 1), c);
                Gfx.Fill(sb, new Rectangle(b.X + 3, b.Y + 3, 2, 2), Color.White);
            }
            else
            {
                Gfx.Fill(sb, new Rectangle(b.X - 2, b.Y - 2, Size + 4, Size + 4), new Color(255, 90, 90) * 0.25f);
                // A pixel heart (not a cross: the red cross is a protected emblem).
                var red = new Color(235, 70, 80);
                Gfx.Fill(sb, new Rectangle(b.X, b.Y + 1, 4, 4), red);
                Gfx.Fill(sb, new Rectangle(b.X + 6, b.Y + 1, 4, 4), red);
                Gfx.Fill(sb, new Rectangle(b.X, b.Y + 3, Size, 3), red);
                Gfx.Fill(sb, new Rectangle(b.X + 2, b.Y + 6, 6, 2), red);
                Gfx.Fill(sb, new Rectangle(b.X + 4, b.Y + 8, 2, 1), red);
                Gfx.Fill(sb, new Rectangle(b.X + 1, b.Y + 2, 2, 1), new Color(255, 190, 190));
            }
        }
    }
}
