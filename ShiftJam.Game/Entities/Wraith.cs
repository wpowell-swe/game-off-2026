using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftJam
{
    /// <summary>A lost soul with no body. Astral only, floats through walls, freed (not killed) when defeated.</summary>
    public sealed class Wraith : IEntity, IHittable
    {
        public static readonly Point Size = new(12, 16);

        private enum State { Drift, Windup, Lunge, Recover }

        private const float DriftSpeed = 28f;
        private const float SeeRange = 120f;
        private const float LungeRange = 44f;
        private const float WindupTime = 0.5f;
        private const float LungeTime = 0.24f;
        private const float LungeSpeed = 150f;
        private const float RecoverTime = 0.6f;
        private const float FlashTime = 0.12f;
        private const float FadeTime = 0.8f;
        private const int AttackDamage = 8;

        public Vector2 Position;
        public Vector2 Velocity;
        public int Health = 24;
        public int Facing = 1;

        private State _state = State.Drift;
        private float _timer;
        private float _flash;
        private float _fade;          // counts up once defeated
        private float _clock;
        private float _wanderTimer;
        private Vector2 _wander;
        private Vector2 _lungeDir;
        private bool _struck;
        private readonly float _seed;
        private float _homeY;
        private bool _homeSet;

        public Plane Plane => Plane.Astral;
        public bool IsAlive => Health > 0 || _fade < FadeTime;
        private bool Defeated => Health <= 0;
        public Rectangle Hurtbox => new((int)Position.X, (int)Position.Y, Size.X, Size.Y);

        public Wraith(Vector2 position)
        {
            Position = position;
            _seed = position.X * 0.13f + position.Y * 0.07f;
        }

        public void Update(float dt, GameWorld world)
        {
            if (!IsAlive) return;
            _clock += dt;
            if (_flash > 0) _flash -= dt;
            if (!_homeSet) { _homeY = Position.Y; _homeSet = true; }

            if (Defeated)
            {
                _fade += dt;
                Velocity *= MathF.Max(0, 1 - 4f * dt);
                Position += Velocity * dt;
                return;
            }

            var player = world.Player;
            bool seen = player != null && player.IsAlive && player.Plane == Plane.Astral;
            Vector2 to = Vector2.Zero;
            if (player != null)
                to = new Vector2(player.Hurtbox.Center.X - Hurtbox.Center.X, player.Hurtbox.Center.Y - Hurtbox.Center.Y);
            float dist = to.Length();

            Vector2 target = Vector2.Zero; // desired velocity
            switch (_state)
            {
                case State.Drift:
                    if (seen && dist <= SeeRange)
                    {
                        if (to.X != 0) Facing = to.X > 0 ? 1 : -1;
                        if (dist <= LungeRange) { _state = State.Windup; _timer = WindupTime; _struck = false; break; }
                        target = to / MathF.Max(dist, 0.001f) * DriftSpeed;
                    }
                    else
                    {
                        _wanderTimer -= dt;
                        if (_wanderTimer <= 0)
                        {
                            _wanderTimer = 1.2f + 0.8f * MathF.Abs(MathF.Sin(_clock + _seed));
                            float a = (_clock * 1.7f + _seed) * 2.3f;
                            _wander = new Vector2(MathF.Cos(a), MathF.Sin(a) * 0.5f) * (DriftSpeed * 0.5f);
                            // Gentle pull back toward home height so it does not wander off forever.
                            _wander.Y += MathHelper.Clamp((_homeY - Position.Y) * 0.3f, -10, 10);
                        }
                        target = _wander;
                        if (target.X != 0) Facing = target.X > 0 ? 1 : -1;
                    }
                    Velocity = Vector2.Lerp(Velocity, target, MathF.Min(1, 4f * dt));
                    break;

                case State.Windup:
                    Velocity = Vector2.Lerp(Velocity, Vector2.Zero, MathF.Min(1, 8f * dt));
                    _timer -= dt;
                    if (_timer <= 0)
                    {
                        _lungeDir = (seen && dist > 0.001f) ? to / dist : new Vector2(Facing, 0);
                        if (_lungeDir.X != 0) Facing = _lungeDir.X > 0 ? 1 : -1;
                        _state = State.Lunge; _timer = LungeTime; _struck = false;
                    }
                    break;

                case State.Lunge:
                    Velocity = _lungeDir * LungeSpeed;
                    _timer -= dt;
                    if (!_struck && _timer <= LungeTime * 0.6f)
                    {
                        _struck = true;
                        var area = Hurtbox; area.Inflate(8, 6);
                        Sfx.Play(SfxId.Swing, 0.4f, 0.2f);
                        world.SpawnHit(new Hit(area, AttackDamage, Facing, Plane.Astral, 100f, false), this);
                    }
                    if (_timer <= 0) { _state = State.Recover; _timer = RecoverTime; }
                    break;

                case State.Recover:
                    Velocity = Vector2.Lerp(Velocity, Vector2.Zero, MathF.Min(1, 6f * dt));
                    _timer -= dt;
                    if (_timer <= 0) _state = State.Drift;
                    break;
            }

            // No gravity, no tile collision: it passes through walls.
            Position += Velocity * dt;
        }

        private readonly Animator _anim = new(SpriteDefs.Wraith);

        public void Draw(SpriteBatch sb)
        {
            if (!IsAlive) return;
            float bob = Defeated ? 0 : MathF.Sin(_clock * 4f + _seed) * 1.5f;
            var r = new Rectangle((int)MathF.Round(Position.X), (int)MathF.Round(Position.Y + bob), Size.X, Size.Y);
            float alpha = Defeated ? 1f - _fade / FadeTime : 0.65f + 0.15f * MathF.Sin(_clock * 6f);

            // Wraiths always use the Astral variant of their art. The sprite already has its own opacity,
            // so only the fade-out (and a slight shimmer) is applied on top.
            bool ok = (_state == State.Lunge && !Defeated) ? _anim.Play("lunge", "float") : _anim.Play("float");
            if (ok)
            {
                float a = Defeated ? alpha : 0.9f + 0.1f * MathF.Sin(_clock * 6f);
                Color tint = _flash > 0 ? new Color(255, 255, 255) : new Color(235, 235, 255);
                _anim.Draw(sb, new Point(r.X + Size.X / 2, r.Bottom), Facing, true, tint * MathHelper.Clamp(a, 0, 1));
                return;
            }
            Color c = new(120, 220, 255);
            if (_flash > 0) c = new Color(200, 255, 255);
            else if (_state == State.Windup && ((int)(_timer * 20f) % 2 == 0)) c = Color.White;
            Gfx.Fill(sb, r, c * MathHelper.Clamp(alpha, 0, 1));
            // Tapering wisp tail below the head.
            var tail = new Rectangle(r.X + 3, r.Bottom, Size.X - 6, 4);
            Gfx.Fill(sb, tail, c * (MathHelper.Clamp(alpha, 0, 1) * 0.5f));
            int ex = Facing > 0 ? r.X + 7 : r.X + 2;
            Gfx.Fill(sb, new Rectangle(ex, r.Y + 4, 3, 3), new Color(10, 30, 60) * MathHelper.Clamp(alpha, 0, 1));
        }

        public void TakeHit(in Hit hit)
        {
            if (Defeated) return;
            Health -= hit.Damage;
            _flash = FlashTime;
            Velocity = new Vector2(hit.Facing * hit.Knockback, -20f);
            if (_state != State.Drift) { _state = State.Recover; _timer = 0.3f; }
            if (Health <= 0) _fade = 0;
        }
    }
}
