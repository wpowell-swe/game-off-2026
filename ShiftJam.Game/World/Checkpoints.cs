using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftJam
{
    /// <summary>
    /// Mid-level checkpoints. A lantern shrine ('c' in a level file) lights when the player touches it; after that,
    /// dying (or pressing R) puts the player back at the shrine instead of the start, with the foes and pickups
    /// that were behind it gone. Resets when the stage changes or the player restarts the stage from the pause menu.
    /// </summary>
    public static class Checkpoints
    {
        public static string Stage;
        public static Vector2 Position;
        public static int Souls;

        public static bool Has(string stageId) => Stage != null && Stage == stageId;
        public static void Reset() { Stage = null; Souls = 0; }

        /// <summary>After a stage is built: if a shrine was reached before, start there and clear what lies behind it.</summary>
        public static void Restore(GameWorld world, string stageId)
        {
            world.StageId = stageId;
            if (!Has(stageId) || world.Player == null) return;
            world.Player.Position = Position;
            world.SoulsFreed = Souls;
            for (int i = world.Entities.Count - 1; i >= 0; i--)
            {
                var e = world.Entities[i];
                if (e == world.Player) continue;
                if (e is Shrine s && s.X <= Position.X + 1) s.Light(silent: true);
                else if (e is Pickup p && p.Home.X < Position.X) world.Remove(e);
                else if (e is IHittable h && h.Hurtbox.Width > 0 && h.Hurtbox.Center.X < Position.X) world.Remove(e);
            }
            world.Camera.Reset();
        }
    }

    /// <summary>A lantern post. Touch it once to light it and set the checkpoint; it also restores a little health and Ki.</summary>
    public sealed class Shrine : IEntity
    {
        private static int HealAmount = Cfg.I("Checkpoint.Heal", 30, v => HealAmount = v);
        private static int KiAmount = Cfg.I("Checkpoint.Ki", 30, v => KiAmount = v);

        private readonly Vector2 _tile;
        private bool _lit;
        private float _clock, _saved;

        public Shrine(Vector2 tile) { _tile = tile; }
        public float X => _tile.X + Metrics.TileSize / 2f;

        public void Light(bool silent)
        {
            _lit = true;
            if (!silent) _saved = 1.8f;
        }

        public void Update(float dt, GameWorld world)
        {
            _clock += dt;
            if (_saved > 0) _saved -= dt;
            var p = world.Player;
            if (_lit || p == null || !p.IsAlive) return;
            var area = new Rectangle((int)_tile.X - 4, (int)_tile.Y - 12, Metrics.TileSize + 8, Metrics.TileSize + 12);
            if (!p.Hurtbox.Intersects(area)) return;

            Light(silent: false);
            Checkpoints.Stage = world.StageId;
            Checkpoints.Position = new Vector2(_tile.X + (Metrics.TileSize - Player.Size.X) / 2f, _tile.Y + Metrics.TileSize - Player.Size.Y);
            Checkpoints.Souls = world.SoulsFreed;
            p.Health = Math.Min(p.MaxHealth, p.Health + HealAmount);
            p.Ki = Math.Min(p.MaxKi, p.Ki + KiAmount);
            Sfx.Play(SfxId.ShiftIn, 0.8f, 0.1f);
            world.Sparks.Sparks(new Vector2(X, _tile.Y + 4), p.Plane, 14, 0);
        }

        public void Draw(SpriteBatch sb)
        {
            int T = Metrics.TileSize, x = (int)_tile.X + T / 2, bottom = (int)_tile.Y + T;
            bool art = TileArt.Draw(sb, _lit ? "shrine_lit" : "shrine", (int)_tile.X / T, (int)_tile.Y / T)
                       || (_lit && TileArt.Draw(sb, "shrine", (int)_tile.X / T, (int)_tile.Y / T));
            var iron = new Color(70, 62, 78);
            if (!art) {
            Gfx.Fill(sb, new Rectangle(x - 4, bottom - 2, 8, 2), iron);          // base
            Gfx.Fill(sb, new Rectangle(x - 1, bottom - 16, 2, 14), iron);        // post
            Gfx.Fill(sb, new Rectangle(x - 4, bottom - 24, 8, 8), iron);         // lantern frame
            if (_lit)
            {
                float f = 0.8f + 0.2f * MathF.Sin(_clock * 9f);
                Gfx.Fill(sb, new Rectangle(x - 9, bottom - 29, 18, 18), new Color(255, 190, 90) * (0.18f * f));
                Gfx.Fill(sb, new Rectangle(x - 3, bottom - 23, 6, 6), new Color(255, 214, 120) * f);
                Gfx.Fill(sb, new Rectangle(x - 1, bottom - 22, 2, 3), Color.White);
            }
            else Gfx.Fill(sb, new Rectangle(x - 3, bottom - 23, 6, 6), new Color(36, 32, 44));
            }
            if (_saved > 0)
            {
                string t = Txt.S("checkpoint.saved", "CHECKPOINT");
                PixelFont.Draw(sb, t, x - PixelFont.Width(t) / 2, bottom - 36 - (int)((1.8f - _saved) * 4), new Color(255, 225, 140) * Math.Min(1f, _saved * 2f), 1);
            }
        }
    }
}
