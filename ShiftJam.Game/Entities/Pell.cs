using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftJam
{
    /// <summary>
    /// Pell, the lantern-keeper (Lantern Streets boss).
    /// Phase 1 (Material, 140 to 70): lantern pole swing, lunge, lights two flames.
    /// Phase 2 (Astral, 70 to 0): floats; wisps, Astral lunge, and every cycle steps back into Material for a
    /// harmless rest so the player can recover Ki and hit him there too.
    /// </summary>
    public sealed class Pell : Boss
    {
        public static readonly Point BodySize = new(18, 36);

        // ---- Tuning (all Pell constants live here) ----
        private const int PellHealth = 140;
        private const int PhaseOneEnd = 70;            // health at which he steps into Astral
        private const float TriggerRangePx = 150f;

        private const float WalkSpeed = 34f;           // phase 1 approach
        private const float NearDist = 30f;            // phase 1 stops walking at this horizontal distance
        private const float SwingStartDist = 44f;      // a swing waits until he is this close (or MaxApproach passes)
        private const float MaxApproach = 2.0f;
        private const float IdleTime = 0.8f;           // minimum pause between phase 1 patterns
        private const float IdleTimeAstral = 0.7f;     // phase 2 pause (shorter: harder)

        private const float SwingWindup = 0.6f;        // telegraphed
        private const float SwingActive = 0.15f;
        private const float SwingRecoverTime = 0.8f;   // the punish window
        private const int SwingDamage = 14;
        private const float SwingKnockback = 170f;
        private const int SwingReach = 42;             // px in front of him
        private const int SwingHeight = 46;

        private const float LungeWindup = 0.5f;        // telegraphed
        private const float LungeTimeMaterial = 0.25f;
        private const float LungeSpeedMaterial = 320f; // 0.25s * 320 = 80 px = 5 tiles
        private const float LungeTimeAstral = 0.3f;
        private const float LungeSpeedAstral = 320f;   // 96 px
        private const float LungeRecoverMaterial = 0.9f;
        private const float LungeRecoverAstral = 0.8f;
        private const int LungeDamage = 12;
        private const float LungeKnockback = 150f;

        private const float LanternWindup = 0.6f;      // flame spots are shown during this
        private const float LanternRecover = 1.0f;
        private const float FlameLife = 3f;
        private const int FlameDamage = 8;
        private const float FlameTick = 0.3f;
        private const int FlameW = 10, FlameH = 14;
        private const float FlameOffset = 26f;         // px either side of the player's centre

        private const float WispWindup = 0.6f;         // telegraphed
        private const float WispRecover = 0.9f;
        private const int WispCount = 3;
        private const float WispFan = 0.45f;           // radians between wisps
        private const float WispSpeed = 55f;
        private const float WispLife = 4f;
        private const int WispDamage = 8;
        private const int WispSize = 10;

        private const float HoverSpeed = 36f;
        private const float HoverStandOff = 56f;       // horizontal distance he keeps in phase 2
        private const float HoverLift = 8f;            // his feet float this far above the player's feet
        private const float HoverMaxVy = 60f;

        private const float RestWarn = 0.4f;           // telegraph before stepping into Material
        private const float RestTime = 3.2f;           // harmless, in Material: Ki recovers, player can hit him
        private const float RestReturnWarn = 0.4f;     // telegraph before going Astral again

        private enum K { Swing, Lunge, Lantern, Wisps, Rest }
        private enum S
        {
            Idle, SwingWind, SwingHit, SwingRecover, LungeWind, LungeDash, LungeRecover,
            LanternWind, LanternRecover, WispWind, WispRecover, RestDown, Rest, RestUp
        }

        private static readonly K[] SeqOne = { K.Swing, K.Lunge, K.Lantern, K.Swing, K.Lunge };
        private static readonly K[] SeqTwo = { K.Wisps, K.Lunge, K.Wisps, K.Rest };

        private S _s = S.Idle;
        private float _timer;
        private float _idleTotal;
        private int _seq;
        private bool _landed;
        private int _lungeDir = 1;
        private readonly Rectangle[] _tele = new Rectangle[2];
        private float _teleProgress;
        private readonly Vector2[] _flamePos = new Vector2[2];

        public Pell(Vector2 position) : base("Pell", PellHealth, BodySize, position, new[]
        {
            new BossPhase(Plane.Material, PhaseOneEnd, 0, "Lantern pole"),
            new BossPhase(Plane.Astral, 0, 1, "Wisps"),
        })
        {
            TriggerRange = TriggerRangePx;
        }

        private K[] Seq => PhaseIndex == 0 ? SeqOne : SeqTwo;

        protected override void OnPhaseStart(int index, GameWorld world) => ResetPattern(1.0f);
        protected override void OnPhaseEnd(int index, GameWorld world) => ResetPattern(1.0f);
        protected override void OnPlayerGone(GameWorld world) => ResetPattern(1.0f);

        private void ResetPattern(float idle)
        {
            _s = S.Idle; _timer = idle; _idleTotal = 0; _landed = false;
            Array.Clear(_tele);
            PlaneOverride = null;   // never stay stuck in a Rest stand if the pattern is cancelled
        }

        protected override void UpdatePhase(float dt, GameWorld world, BossPhase phase)
        {
            var p = world.Player;
            bool astral = phase.Pattern == 1;
            var b = Body;
            float dx = p.Hurtbox.Center.X - b.Center.X;
            float vx = 0;
            bool hover = false;
            Array.Clear(_tele);
            _timer -= dt;

            switch (_s)
            {
                case S.Idle:
                {
                    _idleTotal += dt;
                    hover = true;
                    Face(dx);
                    float stand = astral ? HoverStandOff : NearDist;
                    if (MathF.Abs(dx) > stand) vx = Math.Sign(dx) * (astral ? HoverSpeed : WalkSpeed);
                    var next = Seq[_seq % Seq.Length];
                    bool close = astral || next != K.Swing || MathF.Abs(dx) <= SwingStartDist || _idleTotal > MaxApproach;
                    if (_timer <= 0 && close) StartPattern(world, p);
                    break;
                }

                case S.SwingWind:
                    _teleProgress = 1f - _timer / SwingWindup;
                    _tele[0] = SwingArea(b);
                    if (_timer <= 0) { _s = S.SwingHit; _timer = SwingActive; _landed = false; Sfx.Play(SfxId.Swing, 0.7f, 0.1f); }
                    break;

                case S.SwingHit:
                    if (!_landed) _landed = Strike(world, SwingArea(b), SwingDamage, SwingKnockback);
                    if (_timer <= 0) { _s = S.SwingRecover; _timer = SwingRecoverTime; }
                    break;

                case S.SwingRecover:
                    if (_timer <= 0) EndPattern(astral);
                    break;

                case S.LungeWind:
                    _teleProgress = 1f - _timer / LungeWindup;
                    _tele[0] = LungeArea(b, astral);
                    if (_timer <= 0)
                    {
                        _s = S.LungeDash; _timer = astral ? LungeTimeAstral : LungeTimeMaterial; _landed = false;
                        Sfx.Play(SfxId.Swing, 0.7f, 0.1f);
                    }
                    break;

                case S.LungeDash:
                {
                    vx = _lungeDir * (astral ? LungeSpeedAstral : LungeSpeedMaterial);
                    var area = new Rectangle(b.X - 4, b.Y + 2, b.Width + 8, b.Height - 4);
                    area.X += _lungeDir * 8;
                    if (!_landed) _landed = Strike(world, area, LungeDamage, LungeKnockback);
                    if (_timer <= 0) { _s = S.LungeRecover; _timer = astral ? LungeRecoverAstral : LungeRecoverMaterial; }
                    break;
                }

                case S.LungeRecover:
                    if (_timer <= 0) EndPattern(astral);
                    break;

                case S.LanternWind:
                    _teleProgress = 1f - _timer / LanternWindup;
                    for (int i = 0; i < 2; i++)
                        _tele[i] = new Rectangle((int)_flamePos[i].X, (int)_flamePos[i].Y, FlameW, FlameH);
                    if (_timer <= 0)
                    {
                        for (int i = 0; i < 2; i++) world.Add(new Flame(this, _flamePos[i]));
                        Sfx.Play(SfxId.Swing, 0.4f, 0.2f);
                        _s = S.LanternRecover; _timer = LanternRecover;
                    }
                    break;

                case S.LanternRecover:
                    if (_timer <= 0) EndPattern(astral);
                    break;

                case S.WispWind:
                    hover = false;
                    _teleProgress = 1f - _timer / WispWindup;
                    _tele[0] = new Rectangle(Facing > 0 ? b.Right : b.Left - 70, b.Y, 70, b.Height);
                    if (_timer <= 0)
                    {
                        FireWisps(world, p, b);
                        _s = S.WispRecover; _timer = WispRecover;
                    }
                    break;

                case S.WispRecover:
                    hover = true;
                    if (_timer <= 0) EndPattern(astral);
                    break;

                case S.RestDown:
                    _teleProgress = 1f - _timer / RestWarn;
                    _tele[0] = b;
                    if (_timer <= 0)
                    {
                        PlaneOverride = Plane.Material;
                        world.Sparks.Sparks(new Vector2(b.Center.X, b.Center.Y), Plane.Material, 10, 0);
                        Sfx.Play(SfxId.ShiftOut, 0.6f);
                        _s = S.Rest; _timer = RestTime;
                    }
                    break;

                case S.Rest:
                    // Harmless, standing in Material: no attacks in this state.
                    if (_timer <= 0) { _s = S.RestUp; _timer = RestReturnWarn; }
                    break;

                case S.RestUp:
                    _teleProgress = 1f - _timer / RestReturnWarn;
                    _tele[0] = b;
                    if (_timer <= 0)
                    {
                        PlaneOverride = null;
                        world.Sparks.Sparks(new Vector2(b.Center.X, b.Center.Y), Plane.Astral, 10, 0);
                        Sfx.Play(SfxId.ShiftIn, 0.6f);
                        EndPattern(astral);
                    }
                    break;
            }

            Velocity.X = vx;
            if (Plane == Plane.Astral)
            {
                float target = p.Hurtbox.Bottom - Size.Y - HoverLift;
                Velocity.Y = hover ? MathHelper.Clamp((target - Position.Y) * 3f, -HoverMaxVy, HoverMaxVy) : 0f;
            }
        }

        private void EndPattern(bool astral)
        {
            _s = S.Idle; _timer = astral ? IdleTimeAstral : IdleTime; _idleTotal = 0;
        }

        private void Face(float dx) { if (MathF.Abs(dx) > 1f) Facing = dx > 0 ? 1 : -1; }

        private void StartPattern(GameWorld world, Player p)
        {
            var kind = Seq[_seq % Seq.Length];
            _seq++;
            Face(p.Hurtbox.Center.X - Body.Center.X);
            switch (kind)
            {
                case K.Swing: _s = S.SwingWind; _timer = SwingWindup; break;
                case K.Lunge: _s = S.LungeWind; _timer = LungeWindup; _lungeDir = Facing; break;
                case K.Lantern: _s = S.LanternWind; _timer = LanternWindup; PlaceFlames(world, p); break;
                case K.Wisps: _s = S.WispWind; _timer = WispWindup; break;
                case K.Rest: _s = S.RestDown; _timer = RestWarn; break;
            }
            _teleProgress = 0;
        }

        private Rectangle SwingArea(Rectangle b) =>
            Facing > 0 ? new Rectangle(b.Right - 2, b.Y - 8, SwingReach, SwingHeight)
                       : new Rectangle(b.Left - SwingReach + 2, b.Y - 8, SwingReach, SwingHeight);

        private Rectangle LungeArea(Rectangle b, bool astral)
        {
            int len = (int)((astral ? LungeSpeedAstral * LungeTimeAstral : LungeSpeedMaterial * LungeTimeMaterial));
            return _lungeDir > 0 ? new Rectangle(b.Right, b.Y + 6, len, b.Height - 12)
                                 : new Rectangle(b.Left - len, b.Y + 6, len, b.Height - 12);
        }

        /// <summary>Two flame spots on the floor either side of the player (shown as telegraphs, then spawned).</summary>
        private void PlaceFlames(GameWorld world, Player p)
        {
            const int T = Metrics.TileSize;
            var tiles = world.Tiles;
            float cx = p.Hurtbox.Center.X;
            for (int i = 0; i < 2; i++)
            {
                float x = cx + (i == 0 ? -FlameOffset : FlameOffset) - FlameW / 2f;
                float floorY = p.Hurtbox.Bottom;
                if (tiles != null)
                {
                    int tx = (int)MathF.Floor((x + FlameW / 2f) / T);
                    int ty = Math.Max(0, (int)MathF.Floor((p.Hurtbox.Bottom - 1) / T));
                    while (ty < tiles.Height && !tiles.IsSolid(tx, ty, Plane.Material)) ty++;
                    floorY = ty * T;
                    // Not inside a wall: fall back to the player's own column.
                    var pos = new Vector2(x, floorY - FlameH);
                    if (Mover.Overlaps(tiles, Plane.Material, pos, new Point(FlameW, FlameH)))
                        x = cx - FlameW / 2f;
                }
                _flamePos[i] = new Vector2((float)Math.Round(x), (float)Math.Round(floorY - FlameH));
            }
        }

        private void FireWisps(GameWorld world, Player p, Rectangle b)
        {
            var origin = new Vector2(b.Center.X, b.Center.Y);
            var to = new Vector2(p.Hurtbox.Center.X, p.Hurtbox.Center.Y) - origin;
            float baseAngle = to.LengthSquared() < 1f ? (Facing > 0 ? 0f : MathF.PI) : MathF.Atan2(to.Y, to.X);
            for (int i = 0; i < WispCount; i++)
            {
                float a = baseAngle + (i - (WispCount - 1) / 2f) * WispFan;
                world.Add(new Wisp(this, origin, new Vector2(MathF.Cos(a), MathF.Sin(a)) * WispSpeed));
            }
            Sfx.Play(SfxId.Swing, 0.5f, 0.2f);
        }

        // ---- Drawing ----

        protected override void DrawBody(SpriteBatch sb, Rectangle r, float flash, float alpha)
        {
            bool astral = Plane == Plane.Astral;
            Color robe = astral ? new Color(120, 170, 255) : new Color(130, 90, 60);
            Color trim = astral ? new Color(200, 235, 255) : new Color(200, 160, 90);
            Color skin = astral ? new Color(220, 240, 255) : new Color(220, 180, 140);
            Color pole = astral ? new Color(160, 200, 255) : new Color(90, 62, 40);
            Color lamp = astral ? new Color(160, 240, 255) : new Color(255, 200, 70);

            int f = Facing;
            Gfx.Fill(sb, r, Shade(robe, flash, alpha));
            Gfx.Fill(sb, new Rectangle(r.X, r.Y + 22, r.Width, 3), Shade(trim, flash, alpha));
            Gfx.Fill(sb, new Rectangle(r.X + 3, r.Y, r.Width - 6, 10), Shade(skin, flash, alpha));
            Gfx.Fill(sb, new Rectangle(r.X + (f > 0 ? 10 : 4), r.Y + 3, 3, 3), Shade(Color.Black, 0, alpha));

            // Pole and lantern: raised overhead in a swing windup, forward on the strike, else held upright.
            int hx = f > 0 ? r.Right - 2 : r.Left + 2;
            if (_s == S.SwingWind)
            {
                Gfx.Fill(sb, new Rectangle(hx - 2, r.Y - 10, 4, 4), Shade(lamp, flash, alpha));
                Gfx.Fill(sb, new Rectangle(f > 0 ? r.X : r.Right - 22, r.Y - 6, 22, 2), Shade(pole, flash, alpha));
            }
            else if (_s == S.SwingHit || _s == S.LungeDash)
            {
                int len = 30;
                int px = f > 0 ? r.Right - 2 : r.Left - len + 2;
                Gfx.Fill(sb, new Rectangle(px, r.Y + 12, len, 2), Shade(pole, flash, alpha));
                Gfx.Fill(sb, new Rectangle(f > 0 ? px + len - 4 : px, r.Y + 8, 4, 8), Shade(lamp, flash, alpha));
            }
            else
            {
                Gfx.Fill(sb, new Rectangle(hx, r.Y - 4, 2, r.Height + 4), Shade(pole, flash, alpha));
                bool lit = _s != S.Rest;
                Gfx.Fill(sb, new Rectangle(hx - 2, r.Y - 10, 6, 6), Shade(lit ? lamp : pole, flash, alpha));
            }
        }

        protected override void DrawOverlay(SpriteBatch sb, Rectangle r)
        {
            for (int i = 0; i < _tele.Length; i++)
                if (_tele[i].Width > 0) DrawTelegraph(sb, _tele[i], _s == S.LanternWind ? Plane.Material : Plane, _teleProgress);
        }

        // ---- Hazards ----

        /// <summary>Floor fire. Material plane; hurts the player while it burns. Removed on expiry, phase change or defeat.</summary>
        private sealed class Flame : IEntity
        {
            private readonly Pell _owner;
            private readonly Vector2 _pos;
            private float _life = FlameLife;
            private float _tick;
            private float _t;

            public Flame(Pell owner, Vector2 pos) { _owner = owner; _pos = pos; }

            public void Update(float dt, GameWorld world)
            {
                _t += dt; _life -= dt; _tick -= dt;
                if (_life <= 0 || _owner.ClearHazards) { world.Entities.Remove(this); return; }
                if (_tick > 0 || world.Player == null || !world.Player.IsAlive) return;
                _tick = FlameTick;
                var area = new Rectangle((int)_pos.X - 1, (int)_pos.Y, FlameW + 2, FlameH);
                int face = world.Player.Hurtbox.Center.X >= area.Center.X ? 1 : -1;
                world.SpawnHit(new Hit(area, FlameDamage, face, Plane.Material, 60f, false), _owner);
            }

            public void Draw(SpriteBatch sb)
            {
                float fade = MathHelper.Clamp(_life / 0.5f, 0f, 1f);
                int h = FlameH - 3 + (int)(3f * MathF.Sin(_t * 30f + _pos.X));
                int x = (int)MathF.Round(_pos.X), bottom = (int)MathF.Round(_pos.Y) + FlameH;
                Gfx.Fill(sb, new Rectangle(x, bottom - h, FlameW, h), new Color(255, 110, 20) * fade);
                int ih = Math.Max(2, h - 5 + (((int)(_t * 24f) & 1) == 0 ? 1 : -1));
                Gfx.Fill(sb, new Rectangle(x + 2, bottom - ih, FlameW - 4, ih), new Color(255, 215, 70) * fade);
            }
        }

        /// <summary>Slow Astral projectile. Flies straight, expires after a few seconds, stops on solid walls.</summary>
        private sealed class Wisp : IEntity
        {
            private readonly Pell _owner;
            private Vector2 _pos;          // centre
            private readonly Vector2 _vel;
            private float _life = WispLife;
            private float _t;

            public Wisp(Pell owner, Vector2 pos, Vector2 vel) { _owner = owner; _pos = pos; _vel = vel; }

            public void Update(float dt, GameWorld world)
            {
                _t += dt; _life -= dt;
                if (_life <= 0 || _owner.ClearHazards) { world.Entities.Remove(this); return; }
                _pos += _vel * dt;
                int tx = (int)MathF.Floor(_pos.X / Metrics.TileSize), ty = (int)MathF.Floor(_pos.Y / Metrics.TileSize);
                if (world.Tiles != null && world.Tiles.IsSolid(tx, ty, Plane.Astral)) { world.Entities.Remove(this); return; }
                if (world.Player == null || !world.Player.IsAlive) return;
                var area = new Rectangle((int)MathF.Round(_pos.X) - WispSize / 2, (int)MathF.Round(_pos.Y) - WispSize / 2, WispSize, WispSize);
                int face = _vel.X >= 0 ? 1 : -1;
                if (world.SpawnHit(new Hit(area, WispDamage, face, Plane.Astral, 70f, false), _owner) > 0)
                    world.Entities.Remove(this);
            }

            public void Draw(SpriteBatch sb)
            {
                float a = MathHelper.Clamp(_life / 0.5f, 0f, 1f) * (0.7f + 0.3f * MathF.Sin(_t * 14f));
                int x = (int)MathF.Round(_pos.X), y = (int)MathF.Round(_pos.Y);
                Gfx.Fill(sb, new Rectangle(x - WispSize / 2, y - WispSize / 2, WispSize, WispSize), new Color(90, 200, 255) * a);
                Gfx.Fill(sb, new Rectangle(x - 2, y - 2, 4, 4), new Color(230, 250, 255) * a);
            }
        }
    }
}
