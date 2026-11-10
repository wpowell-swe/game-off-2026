using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftJam
{
    /// <summary>
    /// A townsperson with a body in the Material plane and a soul in the Astral plane (see HollowedSoul).
    /// Break the body's guard in Material, shift, then free the soul. Never killed: health floors at 1.
    /// </summary>
    public sealed class Hollowed : IEntity, IHittable
    {
        public static readonly Point Size = new(16, 30);

        private enum State { Patrol, Chase, Windup, Recover, Broken, Freed }

        // ---- Tuning (private to this class) ----
        private const float Gravity = 600f;
        private const float PatrolSpeed = 16f;
        private const float ChaseSpeed = 34f;
        private const float SeeRange = 110f;          // Material-plane player
        private const float AstralSeeFactor = 0.5f;   // sight multiplier when the player is Astral (clumsy)
        private const float LoseFactor = 1.4f;        // lose range = see range * this
        private const float AttackRange = 24f;
        private const float WindupTime = 0.5f;
        private const float RecoverTime = 0.6f;
        private const int AttackDamage = 12;
        private const int StartHealth = 50;
        private const float ChipFraction = 0.2f;      // share of damage that gets through the guard (min 1)
        private const float GuardedFlinchKnock = 25f; // tiny shove when guarded
        private const float BrokenKnock = 0.5f;       // fraction of hit knockback while guard is broken
        internal const float BrokenTime = 2.5f;       // fixed length of the guard-broken / soul-exposed window
        private const float FreedTime = 1.5f;         // relieved sit-down before both parts vanish
        private const float FlashTime = 0.1f;

        public Vector2 Position;
        public Vector2 Velocity;
        public int Health = StartHealth;
        public int MaxHealth = StartHealth;
        public int Facing = 1;

        private State _state = State.Patrol;
        private float _timer;
        private float _brokenLeft;
        private float _flash;
        private float _patrolTimer;
        private bool _struck;
        private bool _addedSoul;
        private float _clock;
        private HollowedSoul _soul;

        public Plane Plane => Plane.Material;
        public bool IsAlive => _state != State.Freed || _timer > 0;
        public Rectangle Hurtbox => new((int)Position.X, (int)Position.Y, Size.X, Size.Y);

        internal bool GuardBroken => _state == State.Broken;
        internal bool IsFreed => _state == State.Freed;
        internal float FreedFraction => _state == State.Freed ? 1f - Math.Max(0f, _timer) / FreedTime : 0f;
        internal float BrokenFraction => _state == State.Broken ? _brokenLeft / BrokenTime : 0f;
        internal float Clock => _clock;

        public Hollowed(Vector2 position) { Position = position; }

        public void Update(float dt, GameWorld world)
        {
            if (!_addedSoul)
            {
                _addedSoul = true;
                _soul = new HollowedSoul(this);
                world.Add(_soul);
            }
            if (!IsAlive) return;
            _clock += dt;
            if (_flash > 0) _flash -= dt;

            // The soul was reduced to 0 by an Astral hit: free the person.
            if (_state != State.Freed && _soul.Health <= 0)
            {
                _state = State.Freed; _timer = FreedTime;
                Velocity.X = 0;
                Health = Math.Max(Health, 1);
                world.SoulsFreed++;
                world.Sparks.Sparks(new Vector2(Hurtbox.Center.X, Position.Y + 8), Plane.Astral, 10, 0);
            }

            var player = world.Player;
            float dx = 0, dy = 0, dist = float.MaxValue;
            bool alive = player != null && player.IsAlive;
            if (alive)
            {
                dx = player.Hurtbox.Center.X - Hurtbox.Center.X;
                dy = player.Hurtbox.Center.Y - Hurtbox.Center.Y;
                dist = MathF.Sqrt(dx * dx + dy * dy);
            }
            float see = SeeRange * (alive && player.Plane == Plane.Astral ? AstralSeeFactor : 1f);
            bool seen = alive && dist <= see;
            bool keep = alive && dist <= see * LoseFactor;

            float walk = 0;
            switch (_state)
            {
                case State.Patrol:
                    if (seen) { _state = State.Chase; break; }
                    _patrolTimer -= dt;
                    if (_patrolTimer <= 0) { _patrolTimer = 2f; Facing = -Facing; }
                    walk = Facing * PatrolSpeed;
                    break;

                case State.Chase:
                    if (!keep) { _state = State.Patrol; _patrolTimer = 1f; break; }
                    if (MathF.Abs(dx) > 1) Facing = dx > 0 ? 1 : -1;
                    if (MathF.Abs(dx) <= AttackRange && MathF.Abs(dy) < 24)
                    {
                        _state = State.Windup; _timer = WindupTime; _struck = false; break;
                    }
                    walk = MathF.Abs(dx) > 4 ? Facing * ChaseSpeed : 0;
                    break;

                case State.Windup:
                    _timer -= dt;
                    if (_timer <= 0)
                    {
                        if (!_struck)
                        {
                            _struck = true;
                            int x = Facing > 0 ? Hurtbox.Right : Hurtbox.Left - 22;
                            var area = new Rectangle(x, Hurtbox.Top + 4, 22, 20);
                            Sfx.Play(SfxId.Swing, 0.5f, 0.1f);
                            world.SpawnHit(new Hit(area, AttackDamage, Facing, Plane.Material, 150f, false), this);
                        }
                        _state = State.Recover; _timer = RecoverTime;
                    }
                    break;

                case State.Recover:
                    _timer -= dt;
                    if (_timer <= 0) _state = State.Chase;
                    break;

                case State.Broken:
                    // Fixed window from the moment the guard broke; hits never extend it.
                    _brokenLeft -= dt;
                    if (_brokenLeft <= 0)
                    {
                        _brokenLeft = 0;
                        _state = State.Recover; _timer = 0.3f;   // guard is back up
                    }
                    break;

                case State.Freed:
                    _timer -= dt;   // IsAlive turns false when this reaches 0
                    break;
            }

            if (walk != 0)
            {
                if (!GroundAhead(world, Facing))
                {
                    walk = 0;
                    if (_state == State.Patrol) { Facing = -Facing; _patrolTimer = 2f; }
                }
                else Velocity.X = walk;
            }
            else Velocity.X = MoveToward(Velocity.X, 0, (_state == State.Broken ? 300f : 600f) * dt);

            Velocity.Y += Gravity * dt;
            Mover.Move(world.Tiles, Plane, ref Position, Size, ref Velocity, dt, out _, out bool wall, out _);
            if (wall && _state == State.Patrol) { Facing = -Facing; _patrolTimer = 2f; }
        }

        private bool GroundAhead(GameWorld world, int dir)
        {
            float x = dir > 0 ? Position.X + Size.X + 2 : Position.X - 4;
            var probeFloor = new Vector2(x, Position.Y + Size.Y + 1);
            var below = new Vector2(Position.X, Position.Y + 1);
            if (!Mover.Overlaps(world.Tiles, Plane, below, Size)) return true;
            return Mover.Overlaps(world.Tiles, Plane, probeFloor, new Point(2, 2));
        }

        public void TakeHit(in Hit hit)
        {
            if (!IsAlive || _state == State.Freed) return;
            _flash = FlashTime;

            if (_state == State.Broken)
            {
                // Full damage but never lethal; the window is NOT refreshed.
                Health = Math.Max(1, Health - hit.Damage);
                Velocity.X = hit.Facing * hit.Knockback * BrokenKnock;
                return;
            }

            if (hit.BreaksGuard)
            {
                Health = Math.Max(1, Health - hit.Damage);
                _state = State.Broken; _brokenLeft = BrokenTime;
                Velocity.X = hit.Facing * hit.Knockback * BrokenKnock;
                Velocity.Y = -80f;
                _flash = 0.2f;
                Sfx.Play(SfxId.HeavyHit, 1f, 0.05f);
                return;
            }

            // Guard up: chip damage, hardly flinches (windup and recover are not interrupted).
            int chip = Math.Max(1, (int)(hit.Damage * ChipFraction));
            Health = Math.Max(1, Health - chip);
            Velocity.X = hit.Facing * GuardedFlinchKnock;
        }

        public void Draw(SpriteBatch sb)
        {
            if (!IsAlive) return;
            int px = (int)MathF.Round(Position.X), py = (int)MathF.Round(Position.Y);
            bool broken = _state == State.Broken, freed = _state == State.Freed;

            // Broken: slumped (shorter, shifted down). Freed: sitting.
            int h = Size.Y, drop = 0;
            if (broken) { h = Size.Y - 6; drop = 6; }
            else if (freed) { h = Size.Y / 2; drop = Size.Y - h; }
            var r = new Rectangle(px, py + drop, Size.X, h);

            Color body = new(120, 100, 82);
            if (freed) body = new Color(150, 140, 125);
            else if (broken) body = ((int)(_clock * 14f) % 2 == 0) ? new Color(235, 225, 210) : new Color(190, 170, 150);
            else if (_flash > 0) body = new Color(200, 185, 170);
            else if (_state == State.Windup && ((int)(_timer * 20f) % 2 == 0)) body = new Color(255, 120, 80);
            Gfx.Fill(sb, r, body);
            // Head band / darker lower half for a bit of form.
            Gfx.Fill(sb, new Rectangle(r.X, r.Bottom - h / 3, r.Width, h / 3), Color.Lerp(body, Color.Black, 0.3f));
            int ex = Facing > 0 ? r.X + 9 : r.X + 3;
            Gfx.Fill(sb, new Rectangle(ex, r.Y + 5, 4, 3), freed ? new Color(60, 50, 40) : Color.Black);

            if (_state == State.Windup)
            {
                int ax = Facing > 0 ? r.Right : r.Left - 6;
                Gfx.Fill(sb, new Rectangle(ax, r.Y + 9, 6, 4), Color.OrangeRed);
            }

            // Guard bar over the head: solid while up, drains while broken.
            if (!freed)
            {
                var bar = new Rectangle(px + 1, py - 5, Size.X - 2, 3);
                Gfx.Fill(sb, new Rectangle(bar.X - 1, bar.Y - 1, bar.Width + 2, bar.Height + 2), new Color(20, 20, 20));
                if (broken)
                {
                    int w = (int)MathF.Round(bar.Width * BrokenFraction);
                    if (w > 0) Gfx.Fill(sb, new Rectangle(bar.X, bar.Y, w, bar.Height), new Color(255, 200, 80));
                }
                else Gfx.Fill(sb, bar, new Color(120, 190, 255));
            }
        }

        private static float MoveToward(float v, float target, float step) =>
            v < target ? MathF.Min(v + step, target) : MathF.Max(v - step, target);
    }
}
