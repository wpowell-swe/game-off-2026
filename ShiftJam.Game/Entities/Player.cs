using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftJam
{
    /// <summary>Joseph, the monk. Platformer movement, light/heavy chain, block, and the plane shift.</summary>
    public sealed class Player : IEntity, IHittable
    {
        public static readonly Point Size = new(12, 28);

        private enum State { Free, Attack, Hurt, Stun, Dead }
        private enum Phase { Startup, Active, Recovery }

        public Vector2 Position;
        public Vector2 Velocity;
        public int Facing = 1;

        public int MaxHealth = Tuning.PlayerMaxHealth;
        public int Health = Tuning.PlayerMaxHealth;
        public float MaxKi = Tuning.MaxKi;
        public float Ki = Tuning.MaxKi;

        public Plane Plane { get; private set; } = Plane.Material;
        public bool IsAlive => Health > 0;
        public Rectangle Hurtbox => new((int)Position.X, (int)Position.Y, Size.X, Size.Y);

        private State _state = State.Free;
        private bool _onGround;
        private bool _jumping;
        private bool _blocking;
        private float _coyote;
        private float _jumpBuffer;
        private float _shiftBuffer;    // a shift pressed during an attack is remembered briefly, not dropped
        private float _stateTimer;     // Hurt / Stun
        private float _invuln;
        private float _clock;

        // Attack state
        private Phase _phase;
        private int _phaseFrames;      // frames remaining in the current phase
        private bool _heavy;
        private int _chain;            // 1..LightChainMax for light attacks
        private bool _queuedLight;
        private bool _queuedHeavy;
        private Rectangle _flash;
        private float _flashTimer;

        public Player(Vector2 position) { Position = position; }

        public void Update(float dt, GameWorld world)
        {
            _clock += dt;
            var input = world.Input;
            if (_invuln > 0) _invuln -= dt;
            if (_flashTimer > 0) _flashTimer -= dt;

            if (_state == State.Dead || !IsAlive)
            {
                _state = State.Dead;
                _blocking = false;
                Velocity.X = Approach(Velocity.X, 0, Tuning.GroundFriction * dt);
                Physics(dt, world);
                return;
            }

            UpdateKi(dt, world);

            switch (_state)
            {
                case State.Free: UpdateFree(dt, world, input); break;
                case State.Attack: UpdateAttack(dt, world, input); break;
                case State.Hurt:
                case State.Stun:
                    _blocking = false;
                    Velocity.X = Approach(Velocity.X, 0, (_onGround ? Tuning.GroundFriction : Tuning.AirFriction) * dt);
                    _stateTimer -= dt;
                    if (_stateTimer <= 0) _state = State.Free;
                    Physics(dt, world);
                    break;
            }
        }

        // ---- Ki and plane ----

        private void UpdateKi(float dt, GameWorld world)
        {
            if (Plane == Plane.Material)
            {
                Ki = Math.Min(MaxKi, Ki + Tuning.MaterialKiRegenPerSecond * dt);
                return;
            }

            Ki = Math.Max(0, Ki - Tuning.AstralKiDrainPerSecond * dt);
            // Out of Ki: pulled back as soon as it is safe (stays Astral while embedded in a wall).
            if (Ki <= 0 && CanReturnToMaterial(world))
            {
                Plane = Plane.Material;
                if (_state == State.Free || _state == State.Attack)
                {
                    _state = State.Stun;
                    _stateTimer = Tuning.KiDepletedStunTime;
                    _blocking = false;
                }
            }
        }

        private bool CanReturnToMaterial(GameWorld world) =>
            !Mover.Overlaps(world.Tiles, Plane.Material, Position, Size);

        private void TryShift(GameWorld world)
        {
            if (Plane == Plane.Material)
            {
                if (Ki < Tuning.ShiftKiCost) return;
                Ki -= Tuning.ShiftKiCost;
                Plane = Plane.Astral;
            }
            else if (CanReturnToMaterial(world))
            {
                Plane = Plane.Material;
            }
        }

        // ---- Free movement ----

        private void UpdateFree(float dt, GameWorld world, InputFrame input)
        {
            if (input.ShiftPressed || _shiftBuffer > 0) { _shiftBuffer = 0; TryShift(world); }

            if (input.JumpPressed) _jumpBuffer = Tuning.JumpBufferTime;
            else _jumpBuffer -= dt;

            _blocking = input.BlockHeld && _onGround && Plane == Plane.Material;

            if (input.LightPressed) { StartAttack(false, 1, input); return; }
            if (input.HeavyPressed) { StartAttack(true, 1, input); return; }

            float moveX = Math.Abs(input.MoveX) < Tuning.MoveDeadzone ? 0 : Math.Sign(input.MoveX);
            if (_blocking)
            {
                Velocity.X = Approach(Velocity.X, 0, Tuning.GroundFriction * dt);
            }
            else
            {
                float analog = Math.Abs(input.MoveX) < Tuning.MoveDeadzone ? 0 : MathHelper.Clamp(input.MoveX, -1, 1);
                float target = analog * Tuning.WalkSpeed;
                if (moveX != 0) Facing = (int)moveX;
                float rate = moveX == 0
                    ? (_onGround ? Tuning.GroundFriction : Tuning.AirFriction)
                    : (_onGround ? Tuning.GroundAcceleration : Tuning.AirAcceleration);
                Velocity.X = Approach(Velocity.X, target, rate * dt);

                if (_jumpBuffer > 0 && (_onGround || _coyote > 0))
                {
                    Velocity.Y = -Tuning.JumpVelocity;
                    Sfx.Play(SfxId.Jump, 0.8f, 0.05f);
                    _jumping = true;
                    _jumpBuffer = 0;
                    _coyote = 0;
                    _onGround = false;
                }
            }

            if (_jumping && !input.JumpHeld && Velocity.Y < -Tuning.JumpCutVelocity)
                Velocity.Y = -Tuning.JumpCutVelocity;

            Physics(dt, world);
        }

        // ---- Attacks ----

        private void StartAttack(bool heavy, int chain, InputFrame input)
        {
            _state = State.Attack;
            _heavy = heavy;
            _chain = chain;
            _queuedLight = _queuedHeavy = false;
            _blocking = false;
            _phase = Phase.Startup;
            _phaseFrames = heavy ? Tuning.HeavyStartupFrames : Tuning.LightStartupFrames;
            if (Math.Abs(input.MoveX) >= Tuning.MoveDeadzone) Facing = Math.Sign(input.MoveX);
            Velocity.X = Facing * (heavy ? Tuning.HeavyLunge : Tuning.LightLunge);
        }

        private void UpdateAttack(float dt, GameWorld world, InputFrame input)
        {
            _blocking = false;
            if (input.JumpPressed) _jumpBuffer = Tuning.JumpBufferTime; else _jumpBuffer -= dt;

            if (input.ShiftPressed) _shiftBuffer = Tuning.ShiftBufferTime;
            else if (_shiftBuffer > 0) _shiftBuffer -= dt;
            if (input.LightPressed) _queuedLight = true;
            if (input.HeavyPressed) _queuedHeavy = true;

            // Chain / cancel only from recovery.
            if (_phase == Phase.Recovery && (_queuedLight || _queuedHeavy))
            {
                if (_queuedHeavy) { StartAttack(true, 1, input); }
                else if (!_heavy && _chain < Tuning.LightChainMax) { StartAttack(false, _chain + 1, input); }
                else _queuedLight = _queuedHeavy = false;
            }

            // Advance in whole-frame steps so timing stays 60 fps based at any dt.
            _frameAcc += dt;
            while (_frameAcc >= Tuning.Frame && _state == State.Attack)
            {
                _frameAcc -= Tuning.Frame;
                _phaseFrames--;
                if (_phaseFrames > 0) continue;
                AdvancePhase(world);
            }

            if (_state != State.Attack) _frameAcc = 0;
            Velocity.X = Approach(Velocity.X, 0, Tuning.AttackFriction * dt);
            Physics(dt, world);
        }

        private float _frameAcc;

        private void AdvancePhase(GameWorld world)
        {
            switch (_phase)
            {
                case Phase.Startup:
                    _phase = Phase.Active;
                    _phaseFrames = _heavy ? Tuning.HeavyActiveFrames : Tuning.LightActiveFrames;
                    FireHit(world);
                    break;
                case Phase.Active:
                    _phase = Phase.Recovery;
                    _phaseFrames = _heavy ? Tuning.HeavyRecoveryFrames : Tuning.LightRecoveryFrames;
                    break;
                default:
                    _state = State.Free;
                    _queuedLight = _queuedHeavy = false;
                    break;
            }
        }

        private void FireHit(GameWorld world)
        {
            int reach = _heavy ? Tuning.HeavyReachX : Tuning.LightReachX;
            int h = _heavy ? Tuning.HeavyHeight : Tuning.LightHeight;
            int oy = _heavy ? Tuning.HeavyOffsetY : Tuning.LightOffsetY;
            var body = Hurtbox;
            int x = Facing > 0 ? body.Right : body.Left - reach;
            var area = new Rectangle(x, body.Top + oy, reach, h);
            _flash = area;
            _flashTimer = (_heavy ? Tuning.HeavyActiveFrames : Tuning.LightActiveFrames) * Tuning.Frame;

            var hit = new Hit(area,
                _heavy ? Tuning.HeavyDamage : Tuning.LightDamage,
                Facing, Plane,
                _heavy ? Tuning.HeavyKnockback : Tuning.LightKnockback,
                _heavy);
            Sfx.Play(SfxId.Swing, _heavy ? 1f : 0.7f, 0.08f);
            world.SpawnHit(hit, this);
        }

        // ---- Physics ----

        private void Physics(float dt, GameWorld world)
        {
            Velocity.Y = Math.Min(Velocity.Y + Tuning.Gravity * dt, Tuning.MaxFallSpeed);
            Mover.Move(world.Tiles, Plane, ref Position, Size, ref Velocity, dt,
                out bool ground, out _, out bool ceiling);
            _onGround = ground;
            if (ceiling) _jumping = false;
            if (ground)
            {
                _coyote = Tuning.CoyoteTime;
                _jumping = false;
            }
            else
            {
                _coyote -= dt;
                if (Velocity.Y >= 0) _jumping = false;
            }
        }

        private static float Approach(float v, float target, float step)
        {
            if (v < target) return Math.Min(v + step, target);
            return Math.Max(v - step, target);
        }

        // ---- Damage ----

        public void TakeHit(in Hit hit)
        {
            if (!IsAlive || _invuln > 0 || hit.Plane != Plane) return;

            if (_blocking && _state == State.Free && !hit.BreaksGuard)
            {
                Health -= Math.Max(1, (int)MathF.Round(hit.Damage * Tuning.BlockDamageFraction));
                Velocity.X = hit.Facing * hit.Knockback * Tuning.BlockKnockbackFraction;
                _invuln = Tuning.BlockInvulnTime;
                if (Health <= 0) Die();
                return;
            }

            Health -= hit.Damage;
            if (Health <= 0) { Die(); return; }

            _state = State.Hurt;
            _stateTimer = Tuning.HurtStunTime;
            _invuln = Tuning.HurtInvulnTime;
            _blocking = false;
            _jumping = false;
            _queuedLight = _queuedHeavy = false;
            _frameAcc = 0;
            Velocity.X = hit.Facing * Math.Max(hit.Knockback, Tuning.MinHurtKnockback);
            Velocity.Y = -Tuning.HurtPopVelocity;
            _onGround = false;
        }

        private void Die()
        {
            Health = 0;
            _state = State.Dead;
            _blocking = false;
            _jumping = false;
        }

        // ---- Drawing ----

        // Animation tracking only (read-only with respect to behaviour).
        private readonly Animator _anim = new(SpriteDefs.Joseph);
        private Plane _drawnPlane = Plane.Material;
        private float _shiftAnimUntil;
        private const float ShiftAnimTime = 0.2f;

        /// <summary>Picks and draws the sprite for the current state. False means no art: draw rectangles.</summary>
        private bool DrawSprite(SpriteBatch sb, Rectangle body, bool astral)
        {
            bool ok;
            if (_state == State.Dead) ok = _anim.Play("dead");
            else if (_state == State.Attack)
            {
                int s = _heavy ? Tuning.HeavyStartupFrames : Tuning.LightStartupFrames;
                int a = _heavy ? Tuning.HeavyActiveFrames : Tuning.LightActiveFrames;
                int r = _heavy ? Tuning.HeavyRecoveryFrames : Tuning.LightRecoveryFrames;
                int done = _phase == Phase.Startup ? s - _phaseFrames
                         : _phase == Phase.Active ? s + a - _phaseFrames
                         : s + a + r - _phaseFrames;
                ok = _anim.Scrub((done + 0.5f) / (s + a + r), _heavy ? "heavy" : "light");
            }
            else if (_state == State.Hurt || _state == State.Stun) ok = _anim.Play("hurt");
            else if (_clock < _shiftAnimUntil) ok = _anim.Play("shift");
            else if (_blocking) ok = _anim.Play("block");
            else if (!_onGround) ok = _anim.Play("jump");
            else if (Math.Abs(Velocity.X) > 8f) ok = _anim.Play("walk");
            else ok = _anim.Play("idle");

            if (!ok) return false;
            // Hitbox bottom centre = the feet.
            _anim.Draw(sb, new Point(body.X + Size.X / 2, body.Bottom), Facing, astral, Color.White);
            return true;
        }

        public void Draw(SpriteBatch sb)
        {
            var body = Hurtbox;
            bool astral = Plane == Plane.Astral;
            if (Plane != _drawnPlane) { _drawnPlane = Plane; _shiftAnimUntil = _clock + ShiftAnimTime; }

            if (_state == State.Dead)
            {
                if (DrawSprite(sb, body, astral)) return;
                Gfx.Fill(sb, new Rectangle(body.Left - 4, body.Bottom - 8, Size.Y - 8, 8), new Color(90, 60, 40));
                return;
            }

            bool flickerOff = _invuln > 0 && _state != State.Free ? ((int)(_clock / Tuning.FlickerInterval) % 2 == 0)
                            : _invuln > Tuning.BlockInvulnTime && ((int)(_clock / Tuning.FlickerInterval) % 2 == 0);

            bool drewSprite = !flickerOff && DrawSprite(sb, body, astral);

            if (!flickerOff && !drewSprite)
            {
                Color bodyColor = astral ? new Color(150, 130, 255) * 0.55f : new Color(235, 140, 40);
                Color head = astral ? new Color(120, 230, 255) * 0.65f : new Color(240, 200, 150);
                if (_state == State.Hurt || _state == State.Stun)
                    bodyColor = astral ? new Color(255, 255, 255) * 0.6f : Color.White;

                Gfx.Fill(sb, new Rectangle(body.X, body.Y + 8, body.Width, body.Height - 8), bodyColor);
                Gfx.Fill(sb, new Rectangle(body.X + 2, body.Y, body.Width - 4, 8), head);

                // Facing marker: a sash/eye on the front edge.
                int mx = Facing > 0 ? body.Right - 3 : body.Left;
                Gfx.Fill(sb, new Rectangle(mx, body.Y + 3, 3, 2), astral ? new Color(255, 255, 255) * 0.8f : Color.Black);
            }

            if (_blocking && !drewSprite)
            {
                int sx = Facing > 0 ? body.Right + 1 : body.Left - 4;
                Gfx.Fill(sb, new Rectangle(sx, body.Y + 2, 3, 22), new Color(90, 190, 255));
            }

            if (_flashTimer > 0)
            {
                Color c = astral ? new Color(170, 140, 255) * 0.6f : new Color(255, 240, 160) * 0.8f;
                Gfx.Fill(sb, _flash, c);
            }
        }
    }
}
