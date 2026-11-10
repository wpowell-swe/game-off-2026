using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftJam
{
    /// <summary>Owns everything in the current level and runs it.</summary>
    public sealed class GameWorld
    {
        public ITileWorld Tiles { get; set; }
        public Player Player { get; set; }
        public Camera Camera { get; } = new Camera();
        public InputFrame Input;

        public readonly List<IEntity> Entities = new();
        public readonly List<IHittable> Hittables = new();

        public void Add(IEntity entity)
        {
            Entities.Add(entity);
            if (entity is IHittable h) Hittables.Add(h);
        }

        /// <summary>The level exit (a door tile). Reaching it clears a normal level; boss arenas have none.</summary>
        public Rectangle? ExitArea;

        private float _exitClock;

        /// <summary>Souls freed from Hollowed enemies and bosses this level. Entities increment it.</summary>
        public int SoulsFreed;

        /// <summary>Set by the level's boss when it is freed. The game shows a level-complete state.</summary>
        public bool LevelCleared;

        public readonly Particles Sparks = new Particles();
        public readonly ShiftFx ShiftEffect = new ShiftFx();

        // Hit-stop: while > 0 entities do not update. Counted down with real dt, which is
        // never frozen, so hit-stop always ends. Camera, Fx and this timer always advance.
        private float _hitStop;
        private bool _tracked;
        private Plane _lastPlane;
        private int _lastHealth;
        private int _playerHitDamage;   // largest hit that actually hurt the player this frame

        /// <summary>Freeze the world for at least this long.</summary>
        public void HitStop(float seconds) { if (seconds > _hitStop) _hitStop = seconds; }

        /// <summary>
        /// Applies an attack to every live target in the same plane that overlaps its area.
        /// Returns how many targets were hit. A landed hit triggers hit-stop, sparks, shake and sound.
        /// </summary>
        public int SpawnHit(in Hit hit, IHittable source = null)
        {
            int count = 0;
            bool heavy = hit.Damage >= Tuning.HeavyHitDamageThreshold;
            for (int i = 0; i < Hittables.Count; i++)
            {
                var target = Hittables[i];
                if (target == source || !target.IsAlive || target.Plane != hit.Plane) continue;
                if (!hit.Area.Intersects(target.Hurtbox)) continue;

                bool isPlayer = target == Player;
                int before = isPlayer ? Player.Health : 0;
                target.TakeHit(hit);
                // The player can ignore a hit (invulnerable); only count it if it hurt.
                if (isPlayer)
                {
                    if (Player.Health >= before) continue;
                    _playerHitDamage = Math.Max(_playerHitDamage, before - Player.Health);
                }
                count++;

                var hb = target.Hurtbox;
                var pos = new Vector2((hit.Area.Center.X + hb.Center.X) * 0.5f, (hit.Area.Center.Y + hb.Center.Y) * 0.5f);
                Sparks.Sparks(pos, hit.Plane, heavy ? Tuning.SparksHeavy : Tuning.SparksLight, hit.Facing);
                if (!isPlayer)
                {
                    Sfx.Play(heavy ? SfxId.HeavyHit : SfxId.LightHit, 1f, 0.08f);
                    if (heavy) Camera.Shake(Tuning.ShakeHeavyHit, Tuning.ShakeHeavyHitTime);
                }
            }
            if (count > 0)
            {
                float k = MathHelper.Clamp(hit.Damage / (float)Tuning.HitStopMaxDamage, 0f, 1f);
                HitStop(MathHelper.Lerp(Tuning.HitStopLight, Tuning.HitStopHeavy, k));
            }
            return count;
        }

        public void Update(float dt)
        {
            if (!_tracked && Player != null)
            {
                _tracked = true; _lastPlane = Player.Plane; _lastHealth = Player.Health;
            }
            _playerHitDamage = 0;

            if (_hitStop > 0) _hitStop -= dt;   // real time; not gated by the freeze
            else
            {
                var snapshot = Entities.ToArray();
                foreach (var e in snapshot) e.Update(dt, this);
                // Anything that stopped being alive this frame is still in Hittables until the sweep below.
                for (int i = 0; i < Hittables.Count; i++)
                    if (!Hittables[i].IsAlive && Hittables[i] != Player) Sfx.Play(SfxId.EnemyDefeat, 1f, 0.05f);
                Hittables.RemoveAll(h => !h.IsAlive);
            }

            if (Player != null)
            {
                DetectPlayerEvents();
                Camera.Follow(Player.Hurtbox, Tiles, dt);
            }
            _exitClock += dt;
            if (ExitArea.HasValue && Player != null && Player.IsAlive && !LevelCleared && Player.Hurtbox.Intersects(ExitArea.Value))
            {
                LevelCleared = true;
                Sfx.Play(SfxId.ShiftOut, 0.8f);
            }

            Camera.UpdateShake(dt);
            Sparks.Update(dt);
            ShiftEffect.Update(dt);
        }

        private void DetectPlayerEvents()
        {
            if (Player.Plane != _lastPlane)
            {
                _lastPlane = Player.Plane;
                var hb = Player.Hurtbox;
                ShiftEffect.Trigger(new Vector2(hb.Center.X, hb.Center.Y), Player.Plane);
                Camera.Shake(Tuning.ShakeShift, Tuning.ShakeShiftTime);
                HitStop(Tuning.ShiftHitStop);
                Sfx.Play(Player.Plane == Plane.Astral ? SfxId.ShiftIn : SfxId.ShiftOut);
            }
            if (Player.Health < _lastHealth)
            {
                int drop = _lastHealth - Player.Health;
                // A drop much smaller than the hit that caused it means it was blocked.
                bool blocked = _playerHitDamage > 0 && drop < _playerHitDamage * 0.5f + 0.5f && Player.Health > 0;
                Sfx.Play(blocked ? SfxId.Block : SfxId.Hurt);
                if (!blocked) Camera.Shake(Tuning.ShakeHurt, Tuning.ShakeHurtTime);
            }
            _lastHealth = Player.Health;
        }

        public void Draw(SpriteBatch sb)
        {
            if (Tiles is IDrawableTiles tiles) tiles.Draw(sb, Camera);
            DrawExit(sb);
            foreach (var e in Entities) e.Draw(sb);
            Sparks.Draw(sb);
            ShiftEffect.DrawWorld(sb);
        }

        /// <summary>A glowing doorway: two posts and a pulsing light between them.</summary>
        private void DrawExit(SpriteBatch sb)
        {
            if (!ExitArea.HasValue) return;
            var r = ExitArea.Value;
            int h = Metrics.TileSize * 3;                       // taller than the player, so it reads as a doorway
            var door = new Rectangle(r.X + 1, r.Bottom - h, r.Width - 2, h);
            float pulse = 0.55f + 0.25f * MathF.Sin(_exitClock * 4f);
            Gfx.Fill(sb, door, new Color(255, 225, 140) * (pulse * 0.45f));
            Gfx.Fill(sb, new Rectangle(door.X + 3, door.Y + 4, door.Width - 6, door.Height - 4), new Color(255, 240, 190) * pulse);
            Gfx.Fill(sb, new Rectangle(door.X - 2, door.Y, 3, door.Height), new Color(110, 80, 50));
            Gfx.Fill(sb, new Rectangle(door.Right - 1, door.Y, 3, door.Height), new Color(110, 80, 50));
            Gfx.Fill(sb, new Rectangle(door.X - 2, door.Y - 3, door.Width + 4, 3), new Color(110, 80, 50));
        }

        /// <summary>Screen-space effects (flash). Call inside a batch with no camera transform.</summary>
        public void DrawScreenFx(SpriteBatch sb) => ShiftEffect.DrawScreen(sb);
    }

    /// <summary>Optional: a tile world that can draw itself.</summary>
    public interface IDrawableTiles
    {
        void Draw(SpriteBatch sb, Camera camera);
    }
}
