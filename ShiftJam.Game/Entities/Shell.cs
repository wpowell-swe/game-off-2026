using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftJam
{
    /// <summary>A body with no soul. Material only; cannot perceive anyone in the Astral plane.</summary>
    public sealed class Shell : IEntity, IHittable
    {
        public static readonly Point Size = new(14, 28);

        private enum State { Patrol, Chase, Windup, Recover, Stun }

        private const float Gravity = 600f;
        private const float PatrolSpeed = 18f;
        private const float ChaseSpeed = 38f;
        private const float SeeRange = 100f;
        private const float LoseRange = 140f;
        private const float AttackRange = 22f;
        private const float WindupTime = 0.45f;
        private const float RecoverTime = 0.5f;
        private const float StunTime = 0.3f;
        private const float FlashTime = 0.12f;
        private const int AttackDamage = 10;

        public Vector2 Position;
        public Vector2 Velocity;
        public int Health = 40;
        public int MaxHealth = 40;
        public int Facing = 1;

        private State _state = State.Patrol;
        private float _timer;
        private float _flash;
        private float _patrolTimer;
        private bool _struck;

        public Plane Plane => Plane.Material;
        public bool IsAlive => Health > 0;
        public Rectangle Hurtbox => new((int)Position.X, (int)Position.Y, Size.X, Size.Y);

        public Shell(Vector2 position) { Position = position; }

        public void Update(float dt, GameWorld world)
        {
            if (!IsAlive) return;
            if (_flash > 0) _flash -= dt;

            var player = world.Player;
            bool seen = player != null && player.IsAlive && player.Plane == Plane.Material;
            float dx = 0, dy = 0;
            if (player != null)
            {
                dx = player.Hurtbox.Center.X - Hurtbox.Center.X;
                dy = player.Hurtbox.Center.Y - Hurtbox.Center.Y;
            }
            float dist = MathF.Sqrt(dx * dx + dy * dy);

            float walk = 0;
            switch (_state)
            {
                case State.Patrol:
                    if (seen && dist <= SeeRange) { _state = State.Chase; break; }
                    _patrolTimer -= dt;
                    if (_patrolTimer <= 0) { _patrolTimer = 2f; Facing = -Facing; }
                    walk = Facing * PatrolSpeed;
                    break;

                case State.Chase:
                    if (!seen || dist > LoseRange) { _state = State.Patrol; _patrolTimer = 1f; break; }
                    if (MathF.Abs(dx) > 1) Facing = dx > 0 ? 1 : -1;
                    if (MathF.Abs(dx) <= AttackRange && MathF.Abs(dy) < 24)
                    {
                        _state = State.Windup; _timer = WindupTime; _struck = false; break;
                    }
                    // Stand still when close horizontally (e.g. player above or below) to avoid jitter.
                    walk = MathF.Abs(dx) > 4 ? Facing * ChaseSpeed : 0;
                    break;

                case State.Windup:
                    _timer -= dt;
                    if (_timer <= 0)
                    {
                        if (!_struck)
                        {
                            _struck = true;
                            int x = Facing > 0 ? Hurtbox.Right : Hurtbox.Left - 20;
                            var area = new Rectangle(x, Hurtbox.Top + 4, 20, 20);
                            world.SpawnHit(new Hit(area, AttackDamage, Facing, Plane.Material, 140f, false), this);
                        }
                        _state = State.Recover; _timer = RecoverTime;
                    }
                    break;

                case State.Recover:
                    _timer -= dt;
                    if (_timer <= 0) _state = State.Chase;
                    break;

                case State.Stun:
                    _timer -= dt;
                    if (_timer <= 0) _state = State.Chase;
                    break;
            }

            // Ledge/wall safety applies to self-driven walking only.
            if (walk != 0)
            {
                if (!GroundAhead(world, Facing))
                {
                    walk = 0;
                    if (_state == State.Patrol) { Facing = -Facing; _patrolTimer = 2f; }
                }
                else Velocity.X = walk;
            }
            else if (_state != State.Stun) Velocity.X = MoveToward(Velocity.X, 0, 600f * dt);

            Velocity.Y += Gravity * dt;
            Mover.Move(world.Tiles, Plane, ref Position, Size, ref Velocity, dt, out bool ground, out bool wall, out _);
            if (walk == 0 && ground) Velocity.X = MoveToward(Velocity.X, 0, 400f * dt);
            if (wall && _state == State.Patrol) { Facing = -Facing; _patrolTimer = 2f; }
        }

        /// <summary>True when there is floor just past the leading foot and no wall in the way.</summary>
        private bool GroundAhead(GameWorld world, int dir)
        {
            float x = dir > 0 ? Position.X + Size.X + 2 : Position.X - 4;
            var probeFloor = new Vector2(x, Position.Y + Size.Y + 1);
            // Only check when standing on something; mid-air we do not interfere.
            var below = new Vector2(Position.X, Position.Y + 1);
            if (!Mover.Overlaps(world.Tiles, Plane, below, Size)) return true;
            return Mover.Overlaps(world.Tiles, Plane, probeFloor, new Point(2, 2));
        }

        public void Draw(SpriteBatch sb)
        {
            if (!IsAlive) return;
            var r = new Rectangle((int)MathF.Round(Position.X), (int)MathF.Round(Position.Y), Size.X, Size.Y);
            Color body = new(110, 100, 90);
            if (_flash > 0) body = Color.White;
            else if (_state == State.Windup && ((int)(_timer * 20f) % 2 == 0)) body = new Color(255, 120, 80);
            Gfx.Fill(sb, r, body);
            // Hollow eyes: a dark strip showing facing.
            int ex = Facing > 0 ? r.X + 8 : r.X + 2;
            Gfx.Fill(sb, new Rectangle(ex, r.Y + 5, 4, 3), Color.Black);
            if (_state == State.Windup)
            {
                int ax = Facing > 0 ? r.Right : r.Left - 6;
                Gfx.Fill(sb, new Rectangle(ax, r.Y + 8, 6, 4), Color.OrangeRed);
            }
        }

        public void TakeHit(in Hit hit)
        {
            if (!IsAlive) return;
            Health -= hit.Damage;
            _flash = FlashTime;
            _state = State.Stun; _timer = StunTime;
            Velocity.X = hit.Facing * hit.Knockback;
            Velocity.Y = -60f;
        }

        private static float MoveToward(float v, float target, float step) =>
            v < target ? MathF.Min(v + step, target) : MathF.Max(v - step, target);
    }
}
