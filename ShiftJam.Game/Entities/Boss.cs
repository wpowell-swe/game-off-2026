using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftJam
{
    /// <summary>What the boss bar (and the level flow) needs to know about any boss.</summary>
    public interface IBoss
    {
        string Name { get; }
        int Health { get; }
        int MaxHealth { get; }
        /// <summary>True once the boss has been freed (health reached zero).</summary>
        bool IsDefeated { get; }
        /// <summary>True once the player entered the arena trigger.</summary>
        bool Engaged { get; }
        /// <summary>The plane the boss currently stands in (used to colour the bar).</summary>
        Plane Plane { get; }
        /// <summary>How many health bars the boss has (1 for a plain boss).</summary>
        int Bars { get; }
        /// <summary>Which bar is in play, from 0.</summary>
        int BarIndex { get; }
        /// <summary>How full the current bar is, 0 to 1.</summary>
        float BarFraction { get; }
    }

    /// <summary>One row of a boss's phase table.</summary>
    /// <param name="Plane">Plane the boss stands in for this phase.</param>
    /// <param name="EndHealth">The phase ends when health falls to this value (0 for the last phase).</param>
    /// <param name="Pattern">Attack pattern selector; the subclass decides what each number means.</param>
    public sealed record BossPhase(Plane Plane, int EndHealth, int Pattern, string Name = "");

    /// <summary>
    /// Reusable boss base: engage trigger, phase table, damage and hit flash, an invulnerable plane-swap transition
    /// between phases, ground physics, and the "freed, not killed" defeat. Subclasses supply the phase table, the
    /// patterns (UpdatePhase) and the look (DrawBody).
    /// </summary>
    public abstract class Boss : IEntity, IHittable, IBoss
    {
        // ---- Tuning for every boss (private to this class) ----
        private static float Gravity = Cfg.F("Boss.Gravity", 600f, v => Gravity = v);
        private static float IntroTime = Cfg.F("Boss.IntroTime", 0.9f, v => IntroTime = v);               // standing and flashing after the player enters, before attacking
        private static float TransitionTelegraph = Cfg.F("Boss.TransitionTelegraph", 0.5f, v => TransitionTelegraph = v);     // flashing in the old plane before the swap
        private static float TransitionRecover = Cfg.F("Boss.TransitionRecover", 0.5f, v => TransitionRecover = v);       // flashing in the new plane after the swap
        private static float FreedTime = Cfg.F("Boss.FreedTime", 1.5f, v => FreedTime = v);               // relieved fade before the level is cleared
        private static float HitFlashTime = Cfg.F("Boss.HitFlashTime", 0.1f, v => HitFlashTime = v);
        private static float DefaultTriggerRange = Cfg.F("Boss.DefaultTriggerRange", 150f, v => DefaultTriggerRange = v);     // px, used when no TriggerArea is set
        private static float FreedHitStop = Cfg.F("Boss.FreedHitStop", 0.12f, v => FreedHitStop = v);
        private static float FreedRise = Cfg.F("Boss.FreedRise", 9f, v => FreedRise = v);                 // px the soul drifts up while fading

        private enum BossState { Dormant, Intro, Fight, Transition, Freed }

        private readonly IReadOnlyList<BossPhase> _phases;
        private BossState _state = BossState.Dormant;
        private int _phase;
        private float _timer;
        private float _flash;
        private float _clock;
        private bool _swapped;
        private bool _done;
        private Plane? _planeOverride;
        private GameWorld _world;

        public Vector2 Position;
        public Vector2 Velocity;
        public int Facing = -1;

        /// <summary>If set, the boss engages as soon as the player's hurtbox touches this area (world pixels).</summary>
        public Rectangle? TriggerArea;
        /// <summary>Fallback engage distance (px, centre to centre) when TriggerArea is null.</summary>
        public float TriggerRange = DefaultTriggerRange;

        public string Name { get; }
        public int Health { get; private set; }
        public int MaxHealth { get; }
        public Point Size { get; }
        public bool Engaged { get; private set; }
        public bool IsDefeated => _state == BossState.Freed;
        public bool IsAlive => !_done;

        public virtual int Bars => 1;
        public virtual int BarIndex => 0;
        public virtual float BarFraction => MaxHealth > 0 ? MathHelper.Clamp(Health / (float)MaxHealth, 0f, 1f) : 0f;

        protected Boss(string name, int maxHealth, Point size, Vector2 position, IReadOnlyList<BossPhase> phases)
        {
            if (phases == null || phases.Count == 0) throw new ArgumentException("A boss needs at least one phase.");
            Name = name; MaxHealth = maxHealth; Health = maxHealth; Size = size; Position = position; _phases = phases;
        }

        // ---- State exposed to subclasses ----

        protected BossPhase Phase => _phases[_phase];
        protected int PhaseIndex => _phase;
        protected bool OnGround { get; private set; }
        protected Rectangle Body => new((int)Position.X, (int)Position.Y, Size.X, Size.Y);
        protected float Clock => _clock;

        /// <summary>The plane the boss is really in right now: the phase's plane unless a subclass overrides it temporarily.</summary>
        public Plane Plane => _planeOverride ?? Phase.Plane;
        protected Plane? PlaneOverride { get => _planeOverride; set => _planeOverride = value; }

        /// <summary>Hazards the boss spawned (flames, wisps) remove themselves when this is true.</summary>
        internal bool ClearHazards => _state == BossState.Transition || _state == BossState.Freed || _done;

        /// <summary>Only floats (no gravity) while in the Astral plane by default.</summary>
        protected virtual bool AffectedByGravity => Plane == Plane.Material;

        /// <summary>Set by a boss while it is vanished or otherwise untargetable (nothing can hit it, and its hurtbox is empty).</summary>
        protected bool Intangible;

        /// <summary>While true, Strike() hits both planes, so the player cannot dodge it by shifting. Telegraphs turn violet.</summary>
        protected bool Dual;

        private bool CanBeHit => Engaged && _state == BossState.Fight && !Intangible;
        public Rectangle Hurtbox => CanBeHit ? Body : Rectangle.Empty;

        /// <summary>Starts the fight on a later phase (a checkpoint): health is whatever the earlier phases left.</summary>
        protected void SkipToPhase(int index)
        {
            _phase = Math.Clamp(index, 0, _phases.Count - 1);
            Health = _phase == 0 ? MaxHealth : _phases[_phase - 1].EndHealth;
        }

        // ---- Art ----

        private Animator _anim;

        /// <summary>Sprite set name (see SpriteDefs). Null means code-drawn only.</summary>
        protected virtual string SpriteName => null;
        /// <summary>The boss's own pattern state as text; "...Wind" is a windup, "...Hit", "...Dash", "...Recover" and so on an attack.</summary>
        protected virtual string StateName => null;

        private string Pose()
        {
            if (_state == BossState.Freed) return "freed";
            if (_state == BossState.Transition) return "hurt";
            if (_state == BossState.Fight)
            {
                string n = StateName ?? "";
                if (n.EndsWith("Wind")) return "windup";
                if (n.EndsWith("Hit") || n.EndsWith("Dash") || n.EndsWith("Run") || n.EndsWith("Drop")
                    || n.EndsWith("Appear") || n.EndsWith("Recover")) return "attack";
                if (MathF.Abs(Velocity.X) > 12f) return "walk";
            }
            return "idle";
        }

        /// <summary>Draws the artist's sprite if there is one for the current pose; false means use DrawBody.</summary>
        private bool TryDrawSprite(SpriteBatch sb, Rectangle r, float flash, float alpha)
        {
            if (SpriteName == null) return false;
            _anim ??= new Animator(SpriteName);
            string pose = Pose();
            if (!_anim.Play(pose, "idle")) return false;
            var tint = flash > 0.5f ? new Color(255, 170, 170) : Color.White;
            _anim.Draw(sb, new Point(r.Center.X, r.Bottom), Facing, Plane == Plane.Astral, tint * MathHelper.Clamp(alpha, 0f, 1f));
            return true;
        }

        // ---- Hooks ----

        /// <summary>
        /// Called every frame of a phase transition after the opening flash, until it returns false. A boss can use it to
        /// run to another room first. It may set Velocity.X; nothing can hit the boss while it runs.
        /// </summary>
        protected virtual bool HoldTransition(float dt, GameWorld world) => false;

        /// <summary>Called once when the player triggers the fight.</summary>
        protected virtual void OnEngaged(GameWorld world) { }
        /// <summary>Called when a phase begins (index 0 at the start of the fight, then after each transition).</summary>
        protected virtual void OnPhaseStart(int index, GameWorld world) { }
        /// <summary>Called when a phase ends, as the transition starts. Cancel patterns here.</summary>
        protected virtual void OnPhaseEnd(int index, GameWorld world) { }
        /// <summary>Runs every frame of the fight while the player is alive. Set Velocity.X (and Velocity.Y when floating).</summary>
        protected abstract void UpdatePhase(float dt, GameWorld world, BossPhase phase);
        /// <summary>The player is dead or missing: drop whatever pattern is running.</summary>
        protected virtual void OnPlayerGone(GameWorld world) { }
        /// <summary>Draw the body in the given rectangle. flash is 0..1 white-out, alpha 0..1 opacity (use Shade).</summary>
        protected virtual void DrawBody(SpriteBatch sb, Rectangle r, float flash, float alpha)
        {
            var c = Plane == Plane.Astral ? new Color(110, 200, 255) : new Color(150, 110, 80);
            Gfx.Fill(sb, r, Shade(c, flash, alpha));
        }
        /// <summary>Drawn over the body (telegraphs). Not drawn once freed.</summary>
        protected virtual void DrawOverlay(SpriteBatch sb, Rectangle r) { }

        // ---- Helpers for subclasses ----

        protected static Color Shade(Color c, float flash, float alpha) =>
            Color.Lerp(c, Color.White, MathHelper.Clamp(flash, 0f, 1f)) * MathHelper.Clamp(alpha, 0f, 1f);

        protected static float MoveToward(float v, float target, float step) =>
            v < target ? MathF.Min(v + step, target) : MathF.Max(v - step, target);

        /// <summary>Attack in the boss's current plane. Returns true if it hurt something.</summary>
        protected bool Strike(GameWorld world, Rectangle area, int damage, float knockback, bool breaksGuard = false) =>
            Dual ? world.SpawnHitBoth(area, damage, Facing, knockback, breaksGuard, this) > 0
                 : world.SpawnHit(new Hit(area, damage, Facing, Plane, knockback, breaksGuard), this) > 0;

        /// <summary>Flashing rectangle that warns of an attack. progress runs 0 to 1 over the windup.</summary>
        protected static void DrawTelegraph(SpriteBatch sb, Rectangle area, Plane plane, float progress, bool both = false)
        {
            if (area.Width <= 0 || area.Height <= 0) return;
            progress = MathHelper.Clamp(progress, 0f, 1f);
            var c = both ? new Color(220, 120, 255) : plane == Plane.Astral ? new Color(110, 220, 255) : new Color(255, 110, 60);
            bool on = ((int)(progress * 14f) & 1) == 0;
            Gfx.Fill(sb, area, c * (0.16f + 0.3f * progress + (on ? 0.18f : 0f)));
            float e = 0.5f + 0.5f * progress;
            Gfx.Fill(sb, new Rectangle(area.X, area.Y, area.Width, 1), c * e);
            Gfx.Fill(sb, new Rectangle(area.X, area.Bottom - 1, area.Width, 1), c * e);
            Gfx.Fill(sb, new Rectangle(area.X, area.Y, 1, area.Height), c * e);
            Gfx.Fill(sb, new Rectangle(area.Right - 1, area.Y, 1, area.Height), c * e);
        }

        // ---- Update ----

        public void Update(float dt, GameWorld world)
        {
            if (_done) return;
            _world = world;
            _clock += dt;
            if (_flash > 0) _flash -= dt;
            var player = world.Player;
            bool playerOk = player != null && player.IsAlive;

            switch (_state)
            {
                case BossState.Dormant:
                    Velocity.X = 0;
                    if (playerOk && PlayerInTrigger(player))
                    {
                        Engaged = true;
                        _state = BossState.Intro; _timer = IntroTime;
                        OnEngaged(world);
                    }
                    break;

                case BossState.Intro:
                    Velocity.X = 0;
                    if (playerOk) Facing = player.Hurtbox.Center.X >= Body.Center.X ? 1 : -1;
                    _timer -= dt;
                    if (_timer <= 0) { _state = BossState.Fight; OnPhaseStart(_phase, world); }
                    break;

                case BossState.Fight:
                    if (Health <= 0) { BeginFreed(world); break; }
                    if (Health <= Phase.EndHealth && _phase < _phases.Count - 1) { BeginTransition(world); break; }
                    if (!playerOk) { Velocity.X = MoveToward(Velocity.X, 0, 600f * dt); OnPlayerGone(world); }
                    else UpdatePhase(dt, world, Phase);
                    break;

                case BossState.Transition:
                    UpdateTransition(dt, world);
                    break;

                case BossState.Freed:
                    Velocity.X = 0;
                    _timer += dt;
                    if (_timer >= FreedTime)
                    {
                        _done = true;
                        world.LevelCleared = true;
                        world.SoulsFreed++;
                    }
                    break;
            }

            Physics(dt, world);
        }

        private bool PlayerInTrigger(Player p)
        {
            if (TriggerArea.HasValue) return TriggerArea.Value.Intersects(p.Hurtbox);
            float dx = p.Hurtbox.Center.X - Body.Center.X, dy = p.Hurtbox.Center.Y - Body.Center.Y;
            return dx * dx + dy * dy <= TriggerRange * TriggerRange;
        }

        private void Physics(float dt, GameWorld world)
        {
            if (AffectedByGravity) Velocity.Y += Gravity * dt;
            var size = Size;
            // Bosses always collide as Material: walls hold them in every phase, so a plane swap can never embed them.
            var pos = Position; var vel = Velocity;
            Mover.Move(world.Tiles, Plane.Material, ref pos, size, ref vel, dt, out bool ground, out _, out _);
            Position = pos; Velocity = vel; OnGround = ground;
        }

        private void BeginTransition(GameWorld world)
        {
            OnPhaseEnd(_phase, world);
            _state = BossState.Transition; _timer = 0; _swapped = false;
            _planeOverride = null;
            Velocity.X = 0;
        }

        private void UpdateTransition(float dt, GameWorld world)
        {
            _timer += dt;
            Velocity.X = 0;
            if (!_swapped && _timer >= TransitionTelegraph && HoldTransition(dt, world)) return;
            if (!_swapped && _timer >= TransitionTelegraph)
            {
                _swapped = true;
                _phase++;
                var hb = Body;
                world.Sparks.Sparks(new Vector2(hb.Center.X, hb.Center.Y), Plane, 14, 0);
                Sfx.Play(Plane == Plane.Astral ? SfxId.ShiftIn : SfxId.ShiftOut);
                world.Camera.Shake(2f, 0.15f);
                if (Plane == Plane.Astral) Velocity.Y = -40f;   // lift off
            }
            if (_swapped && Plane == Plane.Astral) Velocity.Y = MoveToward(Velocity.Y, -20f, 200f * dt);
            if (_timer >= TransitionTelegraph + TransitionRecover)
            {
                _state = BossState.Fight;
                OnPhaseStart(_phase, world);
            }
        }

        private void BeginFreed(GameWorld world)
        {
            _state = BossState.Freed; _timer = 0;
            _planeOverride = null;
            Velocity.X = 0;
            world.HitStop(FreedHitStop);
            var hb = Body;
            world.Sparks.Sparks(new Vector2(hb.Center.X, hb.Center.Y), Plane, 20, 0);
        }

        // ---- Damage ----

        public void TakeHit(in Hit hit)
        {
            if (!CanBeHit || hit.Plane != Plane) return;
            // A phase cannot be skipped: damage stops at the phase threshold, then the transition begins.
            int floor = _phase < _phases.Count - 1 ? Phase.EndHealth : 0;
            Health = Math.Max(floor, Health - hit.Damage);
            _flash = HitFlashTime;
        }

        // ---- Draw ----

        public void Draw(SpriteBatch sb)
        {
            if (_done) return;
            float rise = _state == BossState.Freed ? FreedRise * (_timer / FreedTime) : 0f;
            var r = new Rectangle((int)MathF.Round(Position.X), (int)MathF.Round(Position.Y - rise), Size.X, Size.Y);

            float alpha = Plane == Plane.Astral ? 0.78f + 0.1f * MathF.Sin(_clock * 6f) : 1f;
            float flash = _flash > 0 ? 1f : 0f;
            if (_state == BossState.Transition && ((int)(_clock * 20f) & 1) == 0) flash = 0.85f;
            if (_state == BossState.Intro && ((int)(_clock * 12f) & 1) == 0) flash = 0.35f;
            if (_state == BossState.Freed)
            {
                flash = 0.55f;   // pale and relieved
                alpha *= MathHelper.Clamp(1f - _timer / FreedTime, 0f, 1f);
            }
            if (Intangible && _state == BossState.Fight) { /* vanished: nothing to draw */ }
            else if (!TryDrawSprite(sb, r, flash, alpha)) DrawBody(sb, r, flash, alpha);
            if (_state != BossState.Freed) DrawOverlay(sb, r);
        }
    }
}
