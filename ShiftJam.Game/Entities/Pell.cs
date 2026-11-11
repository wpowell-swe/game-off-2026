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
        private const float IdleTime = 0.55f;           // minimum pause between phase 1 patterns
        private const float IdleTimeAstral = 0.5f;     // phase 2 pause (shorter: harder)

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
        private static readonly float[] FlameOffsets = { -58f, -26f, 26f, 58f };   // px from the player's centre: four fires, a gap in the middle
        private const int FlameCount = 4;

        // Blink: a flash of light moves Pell behind the player, then he strikes after a short re-appear telegraph.
        private const float BlinkWindup = 0.45f;       // the destination is shown during this
        private const float BlinkAppear = 0.25f;       // he stands still here before the strike telegraph starts
        private const float BlinkFollowWindup = 0.5f;  // the follow-up attack's telegraph is a little shorter than normal (0.6)
        private const float BlinkSideOffset = 40f;     // lands this far to the far side of the player

        // Thrown lanterns: two arcing lanterns land on marked spots and become fires.
        private const float ThrowWindup = 0.55f;
        private const float ThrowRecover = 0.7f;
        private const float ThrowFlight = 0.75f;
        private const float ThrowArc = 38f;
        private const int ThrownDamage = 8;
        private const int ThrownSize = 8;

        private const float WispWindup = 0.6f;         // telegraphed
        private const float WispRecover = 0.9f;
        private const int WispCount = 5;
        private const float WispFan = 0.36f;           // radians between wisps
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

        private enum K { Swing, Lunge, Lantern, Wisps, Rest, Blink, Throw }
        private enum S
        {
            Idle, SwingWind, SwingHit, SwingRecover, LungeWind, LungeDash, LungeRecover,
            LanternWind, LanternRecover, WispWind, WispRecover, RestDown, Rest, RestUp,
            BlinkWind, BlinkAppear, ThrowWind, ThrowRecover
        }

        private static readonly K[] SeqOne = { K.Swing, K.Throw, K.Lunge, K.Blink, K.Lantern, K.Lunge, K.Throw, K.Blink };
        private static readonly K[] SeqTwo = { K.Wisps, K.Blink, K.Wisps, K.Lunge, K.Blink, K.Wisps, K.Rest };

        private S _s = S.Idle;
        private float _timer;
        private float _idleTotal;
        private int _seq;
        private bool _landed;
        private int _lungeDir = 1;
        private readonly Rectangle[] _tele = new Rectangle[FlameCount];
        private float _teleProgress;
        private readonly Vector2[] _flamePos = new Vector2[FlameCount];
        private float _swingWind = SwingWindup;        // current wind-up lengths (a blink follow-up uses a shorter one)
        private float _lungeWind = LungeWindup;
        private Vector2 _blinkDest;
        private readonly Vector2[] _throwSpot = new Vector2[2];

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
            _s = S.Idle; _timer = idle; _idleTotal = 0; _landed = false; Dual = false;
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
                    _teleProgress = 1f - _timer / _swingWind;
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
                    _teleProgress = 1f - _timer / _lungeWind;
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
                    for (int i = 0; i < FlameCount; i++)
                        _tele[i] = new Rectangle((int)_flamePos[i].X, (int)_flamePos[i].Y, FlameW, FlameH);
                    if (_timer <= 0)
                    {
                        for (int i = 0; i < FlameCount; i++) world.Add(new Flame(this, _flamePos[i]));
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

                case S.BlinkWind:
                    hover = true;
                    Face(dx);
                    _teleProgress = 1f - _timer / BlinkWindup;
                    _tele[0] = new Rectangle((int)_blinkDest.X - 2, (int)_blinkDest.Y - 2, Size.X + 4, Size.Y + 4);
                    {
                        // Also pre-show where the follow-up strike will land, so the player can already move out of it.
                        var destBody = new Rectangle((int)_blinkDest.X, (int)_blinkDest.Y, Size.X, Size.Y);
                        int destFacing = p.Hurtbox.Center.X >= destBody.Center.X ? 1 : -1;
                        _tele[1] = astral ? LungeArea(destBody, true, destFacing) : SwingAreaFor(destBody, destFacing);
                    }
                    if (_timer <= 0)
                    {
                        world.Sparks.Sparks(new Vector2(b.Center.X, b.Center.Y), Plane, 14, 0);
                        Position = _blinkDest;
                        Velocity = Vector2.Zero;
                        world.Sparks.Sparks(new Vector2(_blinkDest.X + Size.X / 2f, _blinkDest.Y + Size.Y / 2f), Plane, 14, 0);
                        Sfx.Play(SfxId.ShiftIn, 0.5f, 0.1f);
                        Face(p.Hurtbox.Center.X - (_blinkDest.X + Size.X / 2f));
                        _s = S.BlinkAppear; _timer = BlinkAppear;
                    }
                    break;

                case S.BlinkAppear:
                    hover = true;
                    Face(dx);
                    _tele[0] = Body;
                    _teleProgress = 1f - _timer / BlinkAppear;
                    if (_timer <= 0)
                    {
                        // Strike straight away from behind, but with a shorter telegraph than usual.
                        if (astral) { _s = S.LungeWind; _timer = _lungeWind = BlinkFollowWindup; _lungeDir = Facing; }
                        else { _s = S.SwingWind; _timer = _swingWind = BlinkFollowWindup; }
                    }
                    break;

                case S.ThrowWind:
                    Face(dx);
                    _teleProgress = 1f - _timer / ThrowWindup;
                    for (int i = 0; i < _throwSpot.Length; i++)
                        _tele[i] = new Rectangle((int)_throwSpot[i].X, (int)_throwSpot[i].Y, FlameW, FlameH);
                    if (_timer <= 0)
                    {
                        var origin = new Vector2(b.Center.X, b.Y + 6);
                        for (int i = 0; i < _throwSpot.Length; i++) world.Add(new Thrown(this, origin, _throwSpot[i]));
                        Sfx.Play(SfxId.Swing, 0.5f, 0.15f);
                        _s = S.ThrowRecover; _timer = ThrowRecover;
                    }
                    break;

                case S.ThrowRecover:
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
            _s = S.Idle; _timer = astral ? IdleTimeAstral : IdleTime; _idleTotal = 0; Dual = false;
        }

        private void Face(float dx) { if (MathF.Abs(dx) > 1f) Facing = dx > 0 ? 1 : -1; }

        private void StartPattern(GameWorld world, Player p)
        {
            var kind = Seq[_seq % Seq.Length];
            _seq++;
            Dual = kind == K.Lunge;   // the lunge lands in both planes: shifting does not dodge it
            Face(p.Hurtbox.Center.X - Body.Center.X);
            switch (kind)
            {
                case K.Swing: _s = S.SwingWind; _timer = _swingWind = SwingWindup; break;
                case K.Lunge: _s = S.LungeWind; _timer = _lungeWind = LungeWindup; _lungeDir = Facing; break;
                case K.Blink:
                    if (TryBlinkTarget(world, p, out _blinkDest)) { _s = S.BlinkWind; _timer = BlinkWindup; }
                    else { _s = S.SwingWind; _timer = _swingWind = SwingWindup; }   // nowhere to blink to: fall back to a swing
                    break;
                case K.Throw: _s = S.ThrowWind; _timer = ThrowWindup; PlaceThrowSpots(world, p); break;
                case K.Lantern: _s = S.LanternWind; _timer = LanternWindup; PlaceFlames(world, p); break;
                case K.Wisps: _s = S.WispWind; _timer = WispWindup; break;
                case K.Rest: _s = S.RestDown; _timer = RestWarn; break;
            }
            _teleProgress = 0;
        }

        private Rectangle SwingArea(Rectangle b) => SwingAreaFor(b, Facing);

        private static Rectangle SwingAreaFor(Rectangle b, int facing) =>
            facing > 0 ? new Rectangle(b.Right - 2, b.Y - 8, SwingReach, SwingHeight)
                       : new Rectangle(b.Left - SwingReach + 2, b.Y - 8, SwingReach, SwingHeight);

        private Rectangle LungeArea(Rectangle b, bool astral) => LungeArea(b, astral, _lungeDir);

        private static Rectangle LungeArea(Rectangle b, bool astral, int dir)
        {
            int len = (int)((astral ? LungeSpeedAstral * LungeTimeAstral : LungeSpeedMaterial * LungeTimeMaterial));
            return dir > 0 ? new Rectangle(b.Right, b.Y + 6, len, b.Height - 12)
                           : new Rectangle(b.Left - len, b.Y + 6, len, b.Height - 12);
        }

        /// <summary>Fire spots on the floor around the player (shown as telegraphs, then spawned).</summary>
        private void PlaceFlames(GameWorld world, Player p)
        {
            float cx = p.Hurtbox.Center.X;
            for (int i = 0; i < FlameCount; i++)
                _flamePos[i] = FlameSpot(world, p, cx + FlameOffsets[i]);
        }

        /// <summary>Two landing spots for the thrown lanterns: on the player and a little ahead of where they are walking.</summary>
        private void PlaceThrowSpots(GameWorld world, Player p)
        {
            float cx = p.Hurtbox.Center.X;
            int ahead = p.Velocity.X >= 0 ? 1 : -1;
            _throwSpot[0] = FlameSpot(world, p, cx);
            _throwSpot[1] = FlameSpot(world, p, cx + ahead * 44f);
        }

        /// <summary>Top-left of a fire standing on the floor below the given x, falling back to the player's own column if that spot is inside a wall.</summary>
        private static Vector2 FlameSpot(GameWorld world, Player p, float centreX)
        {
            const int T = Metrics.TileSize;
            var tiles = world.Tiles;
            float x = centreX - FlameW / 2f;
            float floorY = p.Hurtbox.Bottom;
            if (tiles != null)
            {
                int tx = (int)MathF.Floor((x + FlameW / 2f) / T);
                int ty = Math.Max(0, (int)MathF.Floor((p.Hurtbox.Bottom - 1) / T));
                while (ty < tiles.Height && !tiles.IsSolid(tx, ty, Plane.Material)) ty++;
                floorY = ty * T;
                var pos = new Vector2(x, floorY - FlameH);
                if (Mover.Overlaps(tiles, Plane.Material, pos, new Point(FlameW, FlameH)))
                    x = p.Hurtbox.Center.X - FlameW / 2f;
            }
            return new Vector2((float)Math.Round(x), (float)Math.Round(floorY - FlameH));
        }

        /// <summary>Where the blink lands: behind the player (far side from Pell), else the other side. Never inside a wall in Material.</summary>
        private bool TryBlinkTarget(GameWorld world, Player p, out Vector2 dest)
        {
            dest = Position;
            if (world.Tiles == null) return false;
            float levelW = world.Tiles.Width * Metrics.TileSize;
            float cx = p.Hurtbox.Center.X;
            int away = cx >= Body.Center.X ? 1 : -1;   // the side of the player away from Pell
            foreach (int side in new[] { away, -away })
            {
                float x = MathHelper.Clamp(cx + side * BlinkSideOffset - Size.X / 2f, 2f, levelW - Size.X - 2f);
                float y = Plane == Plane.Astral ? p.Hurtbox.Bottom - Size.Y - HoverLift : p.Hurtbox.Bottom - Size.Y;
                var cand = new Vector2(x, y);
                if (Plane == Plane.Material && Mover.Overlaps(world.Tiles, Plane.Material, cand, Size)) continue;
                if (MathF.Abs(x + Size.X / 2f - cx) < 14f) continue;   // too close to the player to be fair
                dest = cand;
                return true;
            }
            return false;
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

        protected override string SpriteName => SpriteDefs.Pell;
        protected override string StateName => _s.ToString();

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
                if (_tele[i].Width > 0) DrawTelegraph(sb, _tele[i], (_s == S.LanternWind || _s == S.ThrowWind) ? Plane.Material : Plane, _teleProgress, Dual);
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

        /// <summary>A lantern lobbed in an arc. It can hurt the player in flight and lands as a fire on its marked spot.</summary>
        private sealed class Thrown : IEntity
        {
            private readonly Pell _owner;
            private readonly Vector2 _from;
            private readonly Vector2 _to;      // top-left of the fire it becomes
            private float _t;

            public Thrown(Pell owner, Vector2 from, Vector2 to) { _owner = owner; _from = from; _to = to + new Vector2(FlameW / 2f, FlameH); }

            private Vector2 PosAt(float f)
            {
                var p = Vector2.Lerp(_from, _to, f);
                p.Y -= MathF.Sin(f * MathF.PI) * ThrowArc;
                return p;
            }

            public void Update(float dt, GameWorld world)
            {
                _t += dt;
                if (_owner.ClearHazards) { world.Entities.Remove(this); return; }
                float f = MathHelper.Clamp(_t / ThrowFlight, 0f, 1f);
                var c = PosAt(f);
                if (world.Player != null && world.Player.IsAlive)
                {
                    var area = new Rectangle((int)MathF.Round(c.X) - ThrownSize / 2, (int)MathF.Round(c.Y) - ThrownSize / 2, ThrownSize, ThrownSize);
                    int face = _to.X >= _from.X ? 1 : -1;
                    if (world.SpawnHit(new Hit(area, ThrownDamage, face, Plane.Material, 70f, false), _owner) > 0)
                    {
                        world.Entities.Remove(this);
                        return;
                    }
                }
                if (f >= 1f)
                {
                    world.Entities.Remove(this);
                    world.Add(new Flame(_owner, new Vector2(_to.X - FlameW / 2f, _to.Y - FlameH)));
                }
            }

            public void Draw(SpriteBatch sb)
            {
                var c = PosAt(MathHelper.Clamp(_t / ThrowFlight, 0f, 1f));
                int x = (int)MathF.Round(c.X), y = (int)MathF.Round(c.Y);
                Gfx.Fill(sb, new Rectangle(x - ThrownSize / 2, y - ThrownSize / 2, ThrownSize, ThrownSize), new Color(255, 140, 30));
                Gfx.Fill(sb, new Rectangle(x - 2, y - 2, 4, 4), new Color(255, 230, 120));
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
