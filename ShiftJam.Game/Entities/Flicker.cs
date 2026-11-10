using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftJam
{
    /// <summary>
    /// A Hollowed whose soul pulls it back and forth between the planes on a beat. It can only be hit
    /// (and only hit back) while it shares a plane with the player. Freed, not killed, at 0 health.
    /// </summary>
    public sealed class Flicker : IEntity, IHittable
    {
        public static readonly Point Size = new(14, 28);

        private enum State { Idle, Windup, Strike, Recover, Stun, Swap }

        // ---- Tunables (private; Data/Tuning.cs is not ours) ----
        private const float BeatPeriod = 2.0f;        // seconds between plane swaps
        private const float TelegraphTime = 0.5f;     // ring + shimmer lead-in before a swap
        private const float SwapTime = 0.12f;         // not hittable, flashing
        private const float MaxSwapDelay = 1.0f;      // wait this long for a safe Material landing, then skip the beat
        private const float RingMaxGrow = 10f;        // px the telegraph ring grows outward
        private const int MaxPushOut = 24;            // px we may nudge it out of a wall when entering Material
        private const float LandProbe = 96f;          // Material swap needs floor within this many px below

        private const float Gravity = 600f;           // Material only
        private const float PatrolSpeed = 18f;
        private const float ChaseSpeed = 38f;
        private const float FloatSpeed = 34f;         // Astral approach speed
        private const float SeeRange = 110f;          // sees the player in either plane
        private const float LoseRange = 150f;
        private const float MeleeRange = 22f;
        private const float MeleeWindup = 0.45f;
        private const float MeleeRecover = 0.5f;
        private const int MeleeDamage = 9;
        private const float LungeRange = 48f;
        private const float LungeWindup = 0.4f;
        private const float LungeTime = 0.22f;
        private const float LungeSpeed = 150f;
        private const float LungeRecover = 0.6f;
        private const int LungeDamage = 9;
        private const float StunTime = 0.3f;
        private const float FlashTime = 0.12f;
        private const float FadeTime = 0.8f;
        private const int StartHealth = 36;

        public Vector2 Position;
        public Vector2 Velocity;
        public int Health = StartHealth;
        public int Facing = 1;

        private Plane _plane = Plane.Material;
        private State _state = State.Idle;
        private float _timer;
        private float _flash;
        private float _fade;
        private float _clock;
        private float _beat;          // counts up to BeatPeriod, then swaps
        private float _swapWait;      // time spent waiting for a safe landing
        private float _patrolTimer;
        private float _homeY;
        private bool _struck;
        private Vector2 _lungeDir;
        private readonly Vector2 _spawn;
        private readonly float _seed;

        public Plane Plane => _plane;
        private bool Defeated => Health <= 0;
        public bool IsAlive => Health > 0 || _fade < FadeTime;
        public Rectangle Hurtbox => (_state == State.Swap || Defeated)
            ? Rectangle.Empty
            : new Rectangle((int)Position.X, (int)Position.Y, Size.X, Size.Y);

        public Flicker(Vector2 position)
        {
            Position = position;
            _spawn = position;
            _homeY = position.Y;
            _seed = position.X * 0.13f + position.Y * 0.07f;
            // Deterministic phase offset in [0, BeatPeriod) so neighbours are not in sync.
            float f = MathF.Abs(MathF.Sin(position.X * 0.0371f + position.Y * 0.0173f) * 43758.5453f);
            _beat = (f - MathF.Floor(f)) * BeatPeriod;
        }

        public void Update(float dt, GameWorld world)
        {
            if (!IsAlive) return;
            _clock += dt;
            if (_flash > 0) _flash -= dt;

            if (Defeated)
            {
                _fade += dt;
                if (_plane == Plane.Material)
                {
                    Velocity.X *= MathF.Max(0, 1 - 6f * dt);
                    Velocity.Y += Gravity * dt;
                    Mover.Move(world.Tiles, Plane.Material, ref Position, Size, ref Velocity, dt, out _, out _, out _);
                }
                else
                {
                    Velocity *= MathF.Max(0, 1 - 4f * dt);
                    Position += Velocity * dt;
                }
                if (_fade >= FadeTime) world.SoulsFreed++;   // once: IsAlive turns false, Update returns early after this
                return;
            }

            // Safety net: fell out of the world (e.g. Material over a pit). Go home, Astral.
            if (world.Tiles != null && Position.Y > (world.Tiles.Height + 4) * Metrics.TileSize)
            {
                Position = _spawn; Velocity = Vector2.Zero; _plane = Plane.Astral; _homeY = _spawn.Y;
                _state = State.Idle; _beat = 0; _swapWait = 0;
            }

            // ---- The beat ----
            if (_state == State.Swap)
            {
                _timer -= dt;
                Velocity = Vector2.Zero;
                if (_timer <= 0) _state = State.Idle;
                return;
            }
            _beat += dt;
            if (_beat >= BeatPeriod)
            {
                if (TrySwap(world)) return;
                _swapWait += dt;
                if (_swapWait >= MaxSwapDelay)
                {   // give up this beat; stay put and try again after a half period. Never forced into a wall.
                    _swapWait = 0; _beat = BeatPeriod * 0.5f;
                }
            }

            var player = world.Player;
            bool alive = player != null && player.IsAlive;
            bool shared = alive && player.Plane == _plane;
            Vector2 to = Vector2.Zero;
            if (alive) to = new Vector2(player.Hurtbox.Center.X - Hurtbox.Center.X, player.Hurtbox.Center.Y - Hurtbox.Center.Y);
            float dist = to.Length();
            bool seen = alive && dist <= SeeRange;

            if (_plane == Plane.Material) UpdateMaterial(dt, world, to, dist, seen, shared);
            else UpdateAstral(dt, to, dist, seen, shared, world);
        }

        /// <summary>Starts a swap if the destination is safe. Returns false when it must wait.</summary>
        private bool TrySwap(GameWorld world)
        {
            Plane next = _plane == Plane.Material ? Plane.Astral : Plane.Material;
            if (next == Plane.Material)
            {
                if (!FindSafeSpot(world, out Vector2 spot)) return false;
                Position = spot;
            }
            _plane = next;
            _state = State.Swap; _timer = SwapTime;     // cancels any windup/strike/stun cleanly
            _struck = false;
            _beat = 0; _swapWait = 0;
            Velocity = Vector2.Zero;
            _homeY = Position.Y;
            _patrolTimer = 1f;
            Sfx.Play(next == Plane.Astral ? SfxId.ShiftIn : SfxId.ShiftOut, 0.3f, 0.1f);
            world.Sparks.Sparks(new Vector2(Position.X + Size.X / 2f, Position.Y + Size.Y / 2f), next, 6, Facing);
            return true;
        }

        /// <summary>Finds a Material-safe position near here: not in a wall, with floor not far below.</summary>
        private bool FindSafeSpot(GameWorld world, out Vector2 spot)
        {
            spot = Position;
            if (world.Tiles == null) return true;
            Vector2 p = Position;
            if (Mover.Overlaps(world.Tiles, Plane.Material, p, Size))
            {
                bool found = false;
                for (int d = 2; d <= MaxPushOut && !found; d += 2)
                {
                    foreach (var off in new[] { new Vector2(0, -d), new Vector2(-d, 0), new Vector2(d, 0), new Vector2(-d, -d), new Vector2(d, -d) })
                    {
                        var c = Position + off;
                        if (!Mover.Overlaps(world.Tiles, Plane.Material, c, Size)) { p = c; found = true; break; }
                    }
                }
                if (!found) return false;
            }
            // Do not drop into a pit: need floor within LandProbe below.
            for (float dy = 1; dy <= LandProbe; dy += 4)
                if (Mover.Overlaps(world.Tiles, Plane.Material, new Vector2(p.X, p.Y + dy), Size)) { spot = p; return true; }
            return false;
        }

        private void UpdateMaterial(float dt, GameWorld world, Vector2 to, float dist, bool seen, bool shared)
        {
            float walk = 0;
            switch (_state)
            {
                case State.Idle:
                    if (seen)
                    {
                        if (MathF.Abs(to.X) > 1) Facing = to.X > 0 ? 1 : -1;
                        if (shared && MathF.Abs(to.X) <= MeleeRange && MathF.Abs(to.Y) < 24)
                        { _state = State.Windup; _timer = MeleeWindup; _struck = false; break; }
                        walk = MathF.Abs(to.X) > 4 ? Facing * ChaseSpeed : 0;
                    }
                    else
                    {
                        _patrolTimer -= dt;
                        if (_patrolTimer <= 0) { _patrolTimer = 2f; Facing = -Facing; }
                        walk = Facing * PatrolSpeed;
                    }
                    break;

                case State.Windup:
                    _timer -= dt;
                    if (_timer <= 0)
                    {
                        int x = Facing > 0 ? Hurtbox.Right : Hurtbox.Left - 20;
                        var area = new Rectangle(x, (int)Position.Y + 4, 20, 20);
                        Sfx.Play(SfxId.Swing, 0.5f, 0.1f);
                        world.SpawnHit(new Hit(area, MeleeDamage, Facing, Plane.Material, 140f, false), this);
                        _state = State.Recover; _timer = MeleeRecover;
                    }
                    break;

                case State.Recover:
                case State.Stun:
                    _timer -= dt;
                    if (_timer <= 0) _state = State.Idle;
                    break;
            }

            if (walk != 0)
            {
                if (!GroundAhead(world, Facing))
                {
                    walk = 0;
                    if (!seen) { Facing = -Facing; _patrolTimer = 2f; }
                }
                else Velocity.X = walk;
            }
            else if (_state != State.Stun) Velocity.X = MoveToward(Velocity.X, 0, 600f * dt);

            Velocity.Y += Gravity * dt;
            Mover.Move(world.Tiles, Plane.Material, ref Position, Size, ref Velocity, dt, out bool ground, out bool wall, out _);
            if (walk == 0 && ground) Velocity.X = MoveToward(Velocity.X, 0, 400f * dt);
            if (wall && _state == State.Idle && !seen) { Facing = -Facing; _patrolTimer = 2f; }
        }

        private void UpdateAstral(float dt, Vector2 to, float dist, bool seen, bool shared, GameWorld world)
        {
            switch (_state)
            {
                case State.Idle:
                {
                    Vector2 target = Vector2.Zero;
                    if (seen)
                    {
                        if (to.X != 0) Facing = to.X > 0 ? 1 : -1;
                        if (shared && dist <= LungeRange) { _state = State.Windup; _timer = LungeWindup; _struck = false; break; }
                        target = to / MathF.Max(dist, 0.001f) * FloatSpeed;
                    }
                    else
                    {
                        float a = (_clock * 0.9f + _seed) * 2.3f;
                        target = new Vector2(MathF.Cos(a) * 10f, MathHelper.Clamp((_homeY - Position.Y) * 0.5f, -10, 10));
                        if (target.X != 0) Facing = target.X > 0 ? 1 : -1;
                    }
                    Velocity = Vector2.Lerp(Velocity, target, MathF.Min(1, 4f * dt));
                    break;
                }
                case State.Windup:
                    Velocity = Vector2.Lerp(Velocity, Vector2.Zero, MathF.Min(1, 8f * dt));
                    _timer -= dt;
                    if (_timer <= 0)
                    {
                        _lungeDir = dist > 0.001f ? to / dist : new Vector2(Facing, 0);
                        if (_lungeDir.X != 0) Facing = _lungeDir.X > 0 ? 1 : -1;
                        _state = State.Strike; _timer = LungeTime; _struck = false;
                    }
                    break;

                case State.Strike:
                    Velocity = _lungeDir * LungeSpeed;
                    _timer -= dt;
                    if (!_struck && _timer <= LungeTime * 0.6f)
                    {
                        _struck = true;
                        var area = Hurtbox; area.Inflate(8, 6);
                        Sfx.Play(SfxId.Swing, 0.4f, 0.2f);
                        world.SpawnHit(new Hit(area, LungeDamage, Facing, Plane.Astral, 100f, false), this);
                    }
                    if (_timer <= 0) { _state = State.Recover; _timer = LungeRecover; }
                    break;

                case State.Recover:
                case State.Stun:
                    Velocity = Vector2.Lerp(Velocity, Vector2.Zero, MathF.Min(1, 6f * dt));
                    _timer -= dt;
                    if (_timer <= 0) _state = State.Idle;
                    break;
            }
            Position += Velocity * dt;   // no gravity, no tile collision
        }

        private bool GroundAhead(GameWorld world, int dir)
        {
            float x = dir > 0 ? Position.X + Size.X + 2 : Position.X - 4;
            var probeFloor = new Vector2(x, Position.Y + Size.Y + 1);
            var below = new Vector2(Position.X, Position.Y + 1);
            if (!Mover.Overlaps(world.Tiles, Plane.Material, below, Size)) return true;
            return Mover.Overlaps(world.Tiles, Plane.Material, probeFloor, new Point(2, 2));
        }

        public void Draw(SpriteBatch sb)
        {
            if (!IsAlive) return;
            bool astral = _plane == Plane.Astral;
            float bob = (astral && !Defeated) ? MathF.Sin(_clock * 4f + _seed) * 1.5f : 0;
            var r = new Rectangle((int)MathF.Round(Position.X), (int)MathF.Round(Position.Y + bob), Size.X, Size.Y);

            Color warm = new(220, 140, 80);
            Color cold = new(120, 220, 255);
            Color body = astral ? cold * 0.65f : warm;
            Color other = astral ? warm : cold;

            // Telegraph: shimmer toward the other plane's colour and a ring that grows.
            float tele = 0;
            if (!Defeated && _state != State.Swap && _beat > BeatPeriod - TelegraphTime)
                tele = MathHelper.Clamp((_beat - (BeatPeriod - TelegraphTime)) / TelegraphTime, 0f, 1f);
            if (_swapWait > 0) tele = 1f;   // stuck waiting for a safe landing: keep the ring up
            if (tele > 0)
            {
                float mix = tele * (0.5f + 0.5f * MathF.Sin(_clock * 36f));
                body = Color.Lerp(body, other * (astral ? 1f : 0.65f), mix);
                int g = (int)MathF.Round(tele * RingMaxGrow) + 2;
                var ring = r; ring.Inflate(g, g);
                DrawFrame(sb, ring, other * (0.35f + 0.5f * tele));
            }

            if (_flash > 0) body = Color.White;
            else if (_state == State.Windup && ((int)(_timer * 20f) % 2 == 0)) body = new Color(255, 120, 80);

            float a = Defeated ? 1f - _fade / FadeTime : 1f;
            if (_state == State.Swap)
            {
                float k = MathHelper.Clamp(_timer / SwapTime, 0f, 1f);
                var fl = r; fl.Inflate(3, 3);
                Gfx.Fill(sb, fl, Color.White * (0.9f * k));
                return;
            }
            Gfx.Fill(sb, r, body * a);
            if (astral) Gfx.Fill(sb, new Rectangle(r.X + 3, r.Bottom, Size.X - 6, 4), body * (a * 0.4f));
            int ex = Facing > 0 ? r.X + 8 : r.X + 2;
            Gfx.Fill(sb, new Rectangle(ex, r.Y + 5, 4, 3), new Color(10, 20, 40) * a);
            if (_state == State.Windup && !astral)
            {
                int ax = Facing > 0 ? r.Right : r.Left - 6;
                Gfx.Fill(sb, new Rectangle(ax, r.Y + 8, 6, 4), Color.OrangeRed);
            }
        }

        private static void DrawFrame(SpriteBatch sb, Rectangle r, Color c)
        {
            Gfx.Fill(sb, new Rectangle(r.X, r.Y, r.Width, 1), c);
            Gfx.Fill(sb, new Rectangle(r.X, r.Bottom - 1, r.Width, 1), c);
            Gfx.Fill(sb, new Rectangle(r.X, r.Y, 1, r.Height), c);
            Gfx.Fill(sb, new Rectangle(r.Right - 1, r.Y, 1, r.Height), c);
        }

        public void TakeHit(in Hit hit)
        {
            if (Defeated || _state == State.Swap) return;
            Health -= hit.Damage;
            _flash = FlashTime;
            _struck = false;
            Velocity.X = hit.Facing * hit.Knockback;
            Velocity.Y = _plane == Plane.Material ? -60f : -20f;
            if (Health <= 0) { _fade = 0; return; }
            _state = State.Stun; _timer = StunTime;
        }

        private static float MoveToward(float v, float target, float step) =>
            v < target ? MathF.Min(v + step, target) : MathF.Max(v - step, target);
    }
}
