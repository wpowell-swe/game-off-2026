using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftJam
{
    /// <summary>
    /// Dova, the market stall owner (Cellars boss). Brisk, bureaucratic about prices, hollowed mid-sale.
    /// Phase 1 (Material, 160 to 80): crate toss (rolling crates), haggle slam (floor shockwaves), stall dash.
    /// Phase 2 (Astral, 80 to 0): floats; coin spray, coin ring, receipt drop; every ~7s she steps into Material
    /// for a harmless rest so Ki recovers and the player can hit her there too.
    /// </summary>
    public sealed class Dova : Boss
    {
        public static readonly Point BodySize = new(20, 34);

        // ---- Tuning (all Dova constants live here) ----
        private const int DovaHealth = 160;
        private const int PhaseOneEnd = 80;
        private const float TriggerRangePx = 150f;

        private const float WalkSpeed = 40f;
        private const float NearDist = 36f;
        private const float MaxApproach = 2.0f;        // stops waiting to get close after this long
        private const float IdleTime = 0.55f;
        private const float IdleTimeAstral = 0.5f;
        private const float SlamStartDist = 60f;       // slam waits until the player is this close
        private const float DashStartDist = 100f;

        // Crate toss: two crates arc to marked spots, land and roll along the floor.
        private const float CrateWindup = 0.55f;
        private const float CrateRecover = 0.8f;
        private const float CrateFlight = 0.7f;
        private const float CrateArc = 40f;
        private const float CrateRollTime = 2.0f;
        private const float CrateRollSpeed = 60f;
        private const int CrateDamage = 10;
        private const int CrateSize = 10;
        private const float CrateAhead = 44f;          // second crate lands this far ahead of the player's walk

        // Haggle slam: overhead slam, two low shockwaves along the floor.
        private const float SlamWindup = 0.6f;
        private const float SlamActive = 0.12f;
        private const float SlamRecover = 0.8f;
        private const int ShockDamage = 10;
        private const float ShockRange = 96f;          // px each side (shown in the telegraph)
        private const float ShockSpeed = 140f;
        private const int ShockW = 12, ShockH = 10;
        private const float ShockKnockback = 90f;

        // Stall dash: 5-tile shoulder charge, long recovery is the punish window.
        private const float DashWindup = 0.5f;
        private const float DashTime = 0.25f;
        private const float DashSpeed = 320f;          // 0.25 * 320 = 80 px = 5 tiles
        private const float DashRecover = 0.9f;
        private const int DashDamage = 12;
        private const float DashKnockback = 150f;

        // Coin spray and ring (Astral).
        private const float SprayWindup = 0.55f;
        private const float SprayRecover = 0.9f;
        private const int SprayCount = 5;
        private const float SprayFan = 0.3f;           // radians between coins
        private const float SpraySpeed = 55f;
        private const float CoinLife = 4f;
        private const int CoinDamage = 8;
        private const int CoinSize = 8;
        private const float RingWindup = 0.7f;
        private const float RingRecover = 0.9f;
        private const int RingCount = 8;
        private const float RingSpeed = 48f;
        private const float RingStartRadius = 12f;
        private const int RingTeleHalf = 40;           // half size of the zone shown while winding up

        // Receipt drop: she vanishes, reappears above the marked spot, then drops.
        private const float ReceiptMark = 0.45f;       // flashes, spot is marked
        private const float ReceiptVanish = 0.6f;     // gone; spot stays marked
        private const float ReceiptHold = 0.3f;        // back above the spot, still marked
        private const float ReceiptDropMax = 0.3f;
        private const float ReceiptFallSpeed = 420f;
        private const float ReceiptRecover = 0.8f;
        private const int ReceiptDamage = 12;
        private const int ReceiptHalfWidth = 14;
        private static readonly float[] ReceiptGaps = { 56f, 40f, 24f };   // her feet above the floor, tried in order

        private const float HoverSpeed = 36f;
        private const float HoverStandOff = 56f;
        private const float HoverLift = 8f;
        private const float HoverMaxVy = 60f;

        private const float RestEvery = 7f;            // seconds of Astral fighting between rests
        private const float RestWarn = 0.4f;
        private const float RestTime = 3f;             // harmless, in Material
        private const float RestReturnWarn = 0.4f;

        private enum K { Crate, Slam, Dash, Spray, Ring, Receipt, Rest }
        private enum S
        {
            Idle, CrateWind, CrateRecover, SlamWind, SlamHit, SlamRecover, DashWind, DashRun, DashRecover,
            SprayWind, SprayRecover, RingWind, RingRecover,
            RecMark, RecVanish, RecHold, RecDrop, RecRecover,
            RestDown, Rest, RestUp
        }

        private static readonly K[] SeqOne = { K.Crate, K.Slam, K.Dash, K.Slam, K.Crate, K.Dash, K.Slam, K.Dash };
        private static readonly K[] SeqTwo = { K.Spray, K.Ring, K.Receipt, K.Spray, K.Receipt, K.Ring };

        private S _s = S.Idle;
        private float _timer;
        private float _idleTotal;
        private float _sinceRest;
        private int _seq;
        private bool _landed;
        private int _dashDir = 1;
        private float _teleProgress;
        private readonly Rectangle[] _tele = new Rectangle[4];
        private readonly Vector2[] _crateSpot = new Vector2[2];   // top-left of each crate on landing
        private Vector2 _recPos;                                   // where she reappears (top-left)
        private float _recFloor;                                   // floor top under the spot
        private float _recCx;
        private int _ringTurn;

        public Dova(Vector2 position) : base("Dova", DovaHealth, BodySize, position, new[]
        {
            new BossPhase(Plane.Material, PhaseOneEnd, 0, "Haggling"),
            new BossPhase(Plane.Astral, 0, 1, "Coins"),
        })
        {
            TriggerRange = TriggerRangePx;
        }

        private K[] Seq => PhaseIndex == 0 ? SeqOne : SeqTwo;

        protected override void OnPhaseStart(int index, GameWorld world) { _sinceRest = 0; ResetPattern(1.0f); }
        protected override void OnPhaseEnd(int index, GameWorld world) => ResetPattern(1.0f);
        protected override void OnPlayerGone(GameWorld world) => ResetPattern(1.0f);

        private void ResetPattern(float idle)
        {
            _s = S.Idle; _timer = idle; _idleTotal = 0; _landed = false;
            Array.Clear(_tele);
            PlaneOverride = null;   // never stay stuck in a Rest stand (or hidden) if a pattern is cancelled
        }

        protected override void UpdatePhase(float dt, GameWorld world, BossPhase phase)
        {
            Intangible = _s == S.RecVanish;   // gone: she cannot be hit while she is invisible
            var p = world.Player;
            bool astral = phase.Pattern == 1;
            var b = Body;
            float dx = p.Hurtbox.Center.X - b.Center.X;
            float vx = 0;
            bool hover = false;
            bool fall = false;
            Array.Clear(_tele);
            _timer -= dt;
            if (astral && _s != S.Rest && _s != S.RestUp && _s != S.RestDown) _sinceRest += dt;

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
                    bool close = astral
                        || (next == K.Slam ? MathF.Abs(dx) <= SlamStartDist : next == K.Dash ? MathF.Abs(dx) <= DashStartDist : true)
                        || _idleTotal > MaxApproach;
                    if (_timer <= 0 && close) StartPattern(world, p, astral);
                    break;
                }

                case S.CrateWind:
                    Face(dx);
                    _teleProgress = 1f - _timer / CrateWindup;
                    for (int i = 0; i < _crateSpot.Length; i++)
                        _tele[i] = new Rectangle((int)_crateSpot[i].X, (int)_crateSpot[i].Y, CrateSize, CrateSize);
                    if (_timer <= 0)
                    {
                        var origin = new Vector2(b.Center.X, b.Y + 6);
                        for (int i = 0; i < _crateSpot.Length; i++) world.Add(new Crate(this, origin, _crateSpot[i]));
                        Sfx.Play(SfxId.Swing, 0.5f, 0.15f);
                        _s = S.CrateRecover; _timer = CrateRecover;
                    }
                    break;

                case S.CrateRecover:
                    if (_timer <= 0) EndPattern(astral);
                    break;

                case S.SlamWind:
                    _teleProgress = 1f - _timer / SlamWindup;
                    SlamZones(b);
                    if (_timer <= 0)
                    {
                        float y = b.Bottom - ShockH;
                        world.Add(new Shock(this, new Vector2(b.Left - ShockW, y), -1));
                        world.Add(new Shock(this, new Vector2(b.Right, y), 1));
                        world.Camera.Shake(2f, 0.12f);
                        Sfx.Play(SfxId.HeavyHit, 0.6f, 0.1f);
                        _s = S.SlamHit; _timer = SlamActive;
                    }
                    break;

                case S.SlamHit:
                    if (_timer <= 0) { _s = S.SlamRecover; _timer = SlamRecover; }
                    break;

                case S.SlamRecover:
                    if (_timer <= 0) EndPattern(astral);
                    break;

                case S.DashWind:
                    _teleProgress = 1f - _timer / DashWindup;
                    _tele[0] = DashArea(b, _dashDir);
                    if (_timer <= 0)
                    {
                        _s = S.DashRun; _timer = DashTime; _landed = false;
                        Sfx.Play(SfxId.Swing, 0.7f, 0.1f);
                    }
                    break;

                case S.DashRun:
                {
                    vx = _dashDir * DashSpeed;
                    var area = new Rectangle(b.X - 4, b.Y + 2, b.Width + 8, b.Height - 4);
                    area.X += _dashDir * 8;
                    if (!_landed) _landed = Strike(world, area, DashDamage, DashKnockback);
                    if (_timer <= 0) { _s = S.DashRecover; _timer = DashRecover; }
                    break;
                }

                case S.DashRecover:
                    if (_timer <= 0) EndPattern(astral);
                    break;

                case S.SprayWind:
                    Face(dx);
                    _teleProgress = 1f - _timer / SprayWindup;
                    _tele[0] = new Rectangle(Facing > 0 ? b.Right : b.Left - 80, b.Y - 14, 80, b.Height + 28);
                    if (_timer <= 0)
                    {
                        FireSpray(world, p, b);
                        _s = S.SprayRecover; _timer = SprayRecover;
                    }
                    break;

                case S.SprayRecover:
                    hover = true;
                    if (_timer <= 0) EndPattern(astral);
                    break;

                case S.RingWind:
                    _teleProgress = 1f - _timer / RingWindup;
                    _tele[0] = new Rectangle(b.Center.X - RingTeleHalf, b.Center.Y - RingTeleHalf, RingTeleHalf * 2, RingTeleHalf * 2);
                    if (_timer <= 0)
                    {
                        FireRing(world, b);
                        _s = S.RingRecover; _timer = RingRecover;
                    }
                    break;

                case S.RingRecover:
                    hover = true;
                    if (_timer <= 0) EndPattern(astral);
                    break;

                case S.RecMark:
                    Face(dx);
                    _teleProgress = 1f - _timer / ReceiptMark;
                    ReceiptZones();
                    _tele[2] = b;
                    if (_timer <= 0)
                    {
                        world.Sparks.Sparks(new Vector2(b.Center.X, b.Center.Y), Plane, 14, 0);
                        Sfx.Play(SfxId.ShiftOut, 0.6f);
                        Position = _recPos;       // hidden up here until she reappears
                        Velocity = Vector2.Zero;
                        _s = S.RecVanish; _timer = ReceiptVanish;
                    }
                    break;

                case S.RecVanish:
                    _teleProgress = 1f - _timer / ReceiptVanish;
                    ReceiptZones();
                    if (_timer <= 0)
                    {
                        var nb = Body;
                        world.Sparks.Sparks(new Vector2(nb.Center.X, nb.Center.Y), Plane, 14, 0);
                        Sfx.Play(SfxId.ShiftIn, 0.6f);
                        _s = S.RecHold; _timer = ReceiptHold;
                    }
                    break;

                case S.RecHold:
                    _teleProgress = 1f - _timer / ReceiptHold;
                    ReceiptZones();
                    if (_timer <= 0) { _s = S.RecDrop; _timer = ReceiptDropMax; _landed = false; }
                    break;

                case S.RecDrop:
                {
                    fall = true;
                    ReceiptZones();
                    var area = new Rectangle((int)_recCx - ReceiptHalfWidth, b.Y, ReceiptHalfWidth * 2, Math.Max(8, (int)_recFloor - b.Y));
                    if (!_landed) _landed = Strike(world, area, ReceiptDamage, 120f);
                    if (_timer <= 0 || OnGround)
                    {
                        world.Camera.Shake(2f, 0.1f);
                        _s = S.RecRecover; _timer = ReceiptRecover;
                    }
                    break;
                }

                case S.RecRecover:
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
                        _sinceRest = 0;
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
                Velocity.Y = fall ? ReceiptFallSpeed
                    : hover ? MathHelper.Clamp((target - Position.Y) * 3f, -HoverMaxVy, HoverMaxVy) : 0f;
            }
        }

        private void EndPattern(bool astral)
        {
            _s = S.Idle; _timer = astral ? IdleTimeAstral : IdleTime; _idleTotal = 0;
        }

        private void Face(float dx) { if (MathF.Abs(dx) > 1f) Facing = dx > 0 ? 1 : -1; }

        private void StartPattern(GameWorld world, Player p, bool astral)
        {
            var kind = Seq[_seq % Seq.Length];
            if (astral && _sinceRest >= RestEvery) kind = K.Rest;   // the timed rest takes the slot; the sequence does not advance
            else _seq++;
            Face(p.Hurtbox.Center.X - Body.Center.X);
            switch (kind)
            {
                case K.Crate: _s = S.CrateWind; _timer = CrateWindup; PlaceCrates(world, p); break;
                case K.Slam: _s = S.SlamWind; _timer = SlamWindup; break;
                case K.Dash: _s = S.DashWind; _timer = DashWindup; _dashDir = Facing; break;
                case K.Spray: _s = S.SprayWind; _timer = SprayWindup; break;
                case K.Ring: _s = S.RingWind; _timer = RingWindup; break;
                case K.Receipt:
                    if (TryReceiptTarget(world, p)) { _s = S.RecMark; _timer = ReceiptMark; }
                    else { _s = S.SprayWind; _timer = SprayWindup; }   // no safe place to reappear: spray instead
                    break;
                case K.Rest: _s = S.RestDown; _timer = RestWarn; break;
            }
            _teleProgress = 0;
        }

        // ---- Geometry helpers ----

        private static float FloorBelow(ITileWorld tiles, float cx, float fromY)
        {
            if (tiles == null) return fromY;
            const int T = Metrics.TileSize;
            int tx = (int)MathF.Floor(cx / T);
            int ty = Math.Max(0, (int)MathF.Floor((fromY - 1) / T));
            while (ty < tiles.Height && !tiles.IsSolid(tx, ty, Plane.Material)) ty++;
            return ty * T;
        }

        private static Rectangle DashArea(Rectangle b, int dir)
        {
            int len = (int)(DashSpeed * DashTime);
            return dir > 0 ? new Rectangle(b.Right, b.Y + 6, len, b.Height - 12)
                           : new Rectangle(b.Left - len, b.Y + 6, len, b.Height - 12);
        }

        /// <summary>Marks the whole path of both shockwaves plus the overhead slam, in advance.</summary>
        private void SlamZones(Rectangle b)
        {
            int y = b.Bottom - ShockH;
            _tele[0] = new Rectangle(b.Left - (int)ShockRange, y, (int)ShockRange, ShockH);
            _tele[1] = new Rectangle(b.Right, y, (int)ShockRange, ShockH);
            _tele[2] = new Rectangle(b.X - 4, b.Y - 16, b.Width + 8, 14);
        }

        private void ReceiptZones()
        {
            _tele[0] = new Rectangle((int)_recCx - ReceiptHalfWidth, (int)_recFloor - 8, ReceiptHalfWidth * 2, 8);
            _tele[1] = new Rectangle((int)_recCx - ReceiptHalfWidth, (int)_recPos.Y, ReceiptHalfWidth * 2, Math.Max(8, (int)_recFloor - (int)_recPos.Y));
        }

        /// <summary>Two crate landing spots: on the player and a little ahead of where they are walking.</summary>
        private void PlaceCrates(GameWorld world, Player p)
        {
            float cx = p.Hurtbox.Center.X;
            int ahead = p.Velocity.X >= 0 ? 1 : -1;
            _crateSpot[0] = CrateSpot(world, p, cx);
            _crateSpot[1] = CrateSpot(world, p, cx + ahead * CrateAhead);
        }

        private static Vector2 CrateSpot(GameWorld world, Player p, float centreX)
        {
            var tiles = world.Tiles;
            float x = centreX - CrateSize / 2f;
            float floorY = p.Hurtbox.Bottom;
            if (tiles != null)
            {
                floorY = FloorBelow(tiles, centreX, p.Hurtbox.Bottom);
                if (Mover.Overlaps(tiles, Plane.Material, new Vector2(x, floorY - CrateSize), new Point(CrateSize, CrateSize)))
                {
                    x = p.Hurtbox.Center.X - CrateSize / 2f;
                    floorY = FloorBelow(tiles, p.Hurtbox.Center.X, p.Hurtbox.Bottom);
                }
            }
            return new Vector2(MathF.Round(x), MathF.Round(floorY - CrateSize));
        }

        /// <summary>Picks where she reappears: above the player's spot, never inside a wall. False if nothing fits.</summary>
        private bool TryReceiptTarget(GameWorld world, Player p)
        {
            var tiles = world.Tiles;
            if (tiles == null) return false;
            const int T = Metrics.TileSize;
            float levelW = tiles.Width * T;
            float cx = MathHelper.Clamp(p.Hurtbox.Center.X, T + Size.X / 2f + 1f, levelW - T - Size.X / 2f - 1f);
            foreach (float candidateX in new[] { cx, p.Hurtbox.Center.X })
            {
                float floorY = FloorBelow(tiles, candidateX, p.Hurtbox.Bottom);
                foreach (float gap in ReceiptGaps)
                {
                    var pos = new Vector2(MathF.Round(candidateX - Size.X / 2f), MathF.Round(floorY - gap - Size.Y));
                    if (pos.Y < T + 1) continue;
                    if (Mover.Overlaps(tiles, Plane.Material, pos, Size)) continue;
                    _recPos = pos; _recFloor = floorY; _recCx = pos.X + Size.X / 2f;
                    return true;
                }
            }
            return false;
        }

        private void FireSpray(GameWorld world, Player p, Rectangle b)
        {
            var origin = new Vector2(b.Center.X, b.Center.Y);
            var to = new Vector2(p.Hurtbox.Center.X, p.Hurtbox.Center.Y) - origin;
            float baseAngle = to.LengthSquared() < 1f ? (Facing > 0 ? 0f : MathF.PI) : MathF.Atan2(to.Y, to.X);
            for (int i = 0; i < SprayCount; i++)
            {
                float a = baseAngle + (i - (SprayCount - 1) / 2f) * SprayFan;
                world.Add(new Coin(this, origin, new Vector2(MathF.Cos(a), MathF.Sin(a)) * SpraySpeed));
            }
            Sfx.Play(SfxId.Swing, 0.5f, 0.2f);
        }

        private void FireRing(GameWorld world, Rectangle b)
        {
            var origin = new Vector2(b.Center.X, b.Center.Y);
            float turn = (_ringTurn++ & 1) * (MathF.PI / RingCount);   // alternate rings shift their gaps
            for (int i = 0; i < RingCount; i++)
            {
                float a = turn + i * MathF.Tau / RingCount;
                var dir = new Vector2(MathF.Cos(a), MathF.Sin(a));
                world.Add(new Coin(this, origin + dir * RingStartRadius, dir * RingSpeed));
            }
            Sfx.Play(SfxId.Swing, 0.5f, 0.2f);
        }

        // ---- Drawing ----

        protected override void DrawBody(SpriteBatch sb, Rectangle r, float flash, float alpha)
        {
            if (_s == S.RecVanish) return;   // gone in a flash
            bool astral = Plane == Plane.Astral;
            Color dress = astral ? new Color(110, 150, 230) : new Color(160, 70, 55);
            Color apron = astral ? new Color(205, 235, 255) : new Color(235, 220, 180);
            Color skin = astral ? new Color(220, 240, 255) : new Color(225, 180, 140);
            Color hair = astral ? new Color(150, 130, 230) : new Color(90, 55, 35);
            Color tag = astral ? new Color(160, 240, 255) : new Color(255, 205, 70);
            Color wood = astral ? new Color(150, 190, 255) : new Color(130, 90, 55);
            int f = Facing;

            // Stout body, apron with a price tag, head with a hair bun.
            Gfx.Fill(sb, new Rectangle(r.X, r.Y + 10, r.Width, r.Height - 10), Shade(dress, flash, alpha));
            Gfx.Fill(sb, new Rectangle(r.X + 3, r.Y + 14, r.Width - 6, r.Height - 14), Shade(apron, flash, alpha));
            Gfx.Fill(sb, new Rectangle(r.X + (f > 0 ? r.Width - 8 : 4), r.Y + 18, 4, 5), Shade(tag, flash, alpha));
            Gfx.Fill(sb, new Rectangle(r.X + 4, r.Y, r.Width - 8, 11), Shade(skin, flash, alpha));
            Gfx.Fill(sb, new Rectangle(r.X + 3, r.Y - 1, r.Width - 6, 4), Shade(hair, flash, alpha));
            Gfx.Fill(sb, new Rectangle(r.X + r.Width / 2 - 3, r.Y - 5, 6, 5), Shade(hair, flash, alpha));
            Gfx.Fill(sb, new Rectangle(r.X + (f > 0 ? 11 : 5), r.Y + 4, 3, 3), Shade(Color.Black, 0, alpha));

            // Arms: crate overhead for the toss, hammer-fist overhead for the slam, shoulder forward for the dash.
            if (_s == S.CrateWind)
                Gfx.Fill(sb, new Rectangle(r.X + 3, r.Y - 14, 14, 9), Shade(wood, flash, alpha));
            else if (_s == S.SlamWind)
                Gfx.Fill(sb, new Rectangle(r.X + 2, r.Y - 12, r.Width - 4, 5), Shade(skin, flash, alpha));
            else if (_s == S.DashWind || _s == S.DashRun)
                Gfx.Fill(sb, new Rectangle(f > 0 ? r.Right - 3 : r.Left - 5, r.Y + 10, 8, 12), Shade(dress, flash, alpha));
            else
                Gfx.Fill(sb, new Rectangle(f > 0 ? r.Right - 2 : r.Left - 4, r.Y + 14, 6, 4), Shade(skin, flash, alpha));
        }

        protected override void DrawOverlay(SpriteBatch sb, Rectangle r)
        {
            bool materialAttack = _s == S.CrateWind || _s == S.SlamWind || _s == S.DashWind;
            for (int i = 0; i < _tele.Length; i++)
                if (_tele[i].Width > 0) DrawTelegraph(sb, _tele[i], materialAttack ? Plane.Material : Plane, _teleProgress);
        }

        // ---- Hazards (all removed when ClearHazards turns true: phase change, defeat) ----

        /// <summary>Lobbed crate, Material. Harmless in the air; lands on its marked spot and then rolls along the floor.</summary>
        private sealed class Crate : IEntity
        {
            private readonly Dova _owner;
            private readonly Vector2 _from;
            private readonly Vector2 _to;      // top-left on landing
            private readonly int _dir;
            private float _t;
            private float _x;
            private bool _rolling;

            public Crate(Dova owner, Vector2 from, Vector2 to)
            {
                _owner = owner; _from = from; _to = to; _x = to.X;
                _dir = to.X + CrateSize / 2f >= from.X ? 1 : -1;
            }

            private Rectangle Box(float f)
            {
                if (_rolling) return new Rectangle((int)MathF.Round(_x), (int)MathF.Round(_to.Y), CrateSize, CrateSize);
                var c = Vector2.Lerp(_from, _to + new Vector2(CrateSize / 2f, CrateSize / 2f), f);
                c.Y -= MathF.Sin(f * MathF.PI) * CrateArc;
                return new Rectangle((int)MathF.Round(c.X) - CrateSize / 2, (int)MathF.Round(c.Y) - CrateSize / 2, CrateSize, CrateSize);
            }

            public void Update(float dt, GameWorld world)
            {
                _t += dt;
                if (_owner.ClearHazards) { world.Entities.Remove(this); return; }
                if (!_rolling)
                {
                    if (_t >= CrateFlight) { _rolling = true; _t = 0; Sfx.Play(SfxId.LightHit, 0.4f, 0.2f); }
                    return;
                }
                if (_t >= CrateRollTime) { world.Entities.Remove(this); return; }
                _x += _dir * CrateRollSpeed * dt;
                var box = Box(1f);
                // Stop at walls and at the end of the floor.
                int front = _dir > 0 ? box.Right : box.Left - 1;
                const int T = Metrics.TileSize;
                var tiles = world.Tiles;
                if (tiles != null)
                {
                    int tx = (int)MathF.Floor(front / (float)T);
                    if (tiles.IsSolid(tx, (int)MathF.Floor((box.Y + CrateSize / 2) / (float)T), Plane.Material)
                        || !tiles.IsSolid(tx, (int)MathF.Floor(box.Bottom / (float)T), Plane.Material))
                    { world.Entities.Remove(this); return; }
                }
                if (world.Player == null || !world.Player.IsAlive) return;
                if (world.SpawnHit(new Hit(box, CrateDamage, _dir, Plane.Material, 80f, false), _owner) > 0)
                    world.Entities.Remove(this);
            }

            public void Draw(SpriteBatch sb)
            {
                var r = Box(MathHelper.Clamp(_t / CrateFlight, 0f, 1f));
                float fade = _rolling ? MathHelper.Clamp((CrateRollTime - _t) / 0.4f, 0f, 1f) : 1f;
                Gfx.Fill(sb, r, new Color(130, 90, 55) * fade);
                Gfx.Fill(sb, new Rectangle(r.X, r.Y, r.Width, 2), new Color(190, 140, 80) * fade);
                int slat = _rolling && (((int)(_t * 8f)) & 1) == 0 ? 3 : 6;
                Gfx.Fill(sb, new Rectangle(r.X + 1, r.Y + slat, r.Width - 2, 1), new Color(80, 52, 30) * fade);
            }
        }

        /// <summary>Low shockwave along the floor, Material. Travels a fixed range, stops at walls, removed on hit.</summary>
        private sealed class Shock : IEntity
        {
            private readonly Dova _owner;
            private readonly float _y;
            private readonly int _dir;
            private float _x;
            private float _travelled;

            public Shock(Dova owner, Vector2 pos, int dir) { _owner = owner; _x = pos.X; _y = pos.Y; _dir = dir; }

            private Rectangle Box => new((int)MathF.Round(_x), (int)MathF.Round(_y), ShockW, ShockH);

            public void Update(float dt, GameWorld world)
            {
                if (_owner.ClearHazards) { world.Entities.Remove(this); return; }
                float step = ShockSpeed * dt;
                _x += _dir * step; _travelled += step;
                var box = Box;
                var tiles = world.Tiles;
                if (_travelled >= ShockRange) { world.Entities.Remove(this); return; }
                if (tiles != null)
                {
                    int front = _dir > 0 ? box.Right : box.Left - 1;
                    if (tiles.IsSolid((int)MathF.Floor(front / (float)Metrics.TileSize), (int)MathF.Floor((box.Y + ShockH / 2) / (float)Metrics.TileSize), Plane.Material))
                    { world.Entities.Remove(this); return; }
                }
                if (world.Player == null || !world.Player.IsAlive) return;
                if (world.SpawnHit(new Hit(box, ShockDamage, _dir, Plane.Material, ShockKnockback, false), _owner) > 0)
                    world.Entities.Remove(this);
            }

            public void Draw(SpriteBatch sb)
            {
                var r = Box;
                Gfx.Fill(sb, r, new Color(255, 140, 50));
                Gfx.Fill(sb, new Rectangle(r.X + 2, r.Y + 3, r.Width - 4, r.Height - 3), new Color(255, 225, 150));
            }
        }

        /// <summary>Astral coin. Flies straight, expires after a few seconds, stops on solid walls. Material players are not hurt.</summary>
        private sealed class Coin : IEntity
        {
            private readonly Dova _owner;
            private Vector2 _pos;          // centre
            private readonly Vector2 _vel;
            private float _life = CoinLife;
            private float _t;

            public Coin(Dova owner, Vector2 pos, Vector2 vel) { _owner = owner; _pos = pos; _vel = vel; }

            public void Update(float dt, GameWorld world)
            {
                _t += dt; _life -= dt;
                if (_life <= 0 || _owner.ClearHazards) { world.Entities.Remove(this); return; }
                _pos += _vel * dt;
                int tx = (int)MathF.Floor(_pos.X / Metrics.TileSize), ty = (int)MathF.Floor(_pos.Y / Metrics.TileSize);
                if (world.Tiles != null && world.Tiles.IsSolid(tx, ty, Plane.Astral)) { world.Entities.Remove(this); return; }
                if (world.Player == null || !world.Player.IsAlive) return;
                var area = new Rectangle((int)MathF.Round(_pos.X) - CoinSize / 2, (int)MathF.Round(_pos.Y) - CoinSize / 2, CoinSize, CoinSize);
                int face = _vel.X >= 0 ? 1 : -1;
                if (world.SpawnHit(new Hit(area, CoinDamage, face, Plane.Astral, 70f, false), _owner) > 0)
                    world.Entities.Remove(this);
            }

            public void Draw(SpriteBatch sb)
            {
                float a = MathHelper.Clamp(_life / 0.5f, 0f, 1f) * (0.7f + 0.3f * MathF.Sin(_t * 14f));
                int x = (int)MathF.Round(_pos.X), y = (int)MathF.Round(_pos.Y);
                bool wide = ((int)(_t * 10f) & 1) == 0;   // spin
                int w = wide ? CoinSize : CoinSize - 4;
                Gfx.Fill(sb, new Rectangle(x - w / 2, y - CoinSize / 2, w, CoinSize), new Color(120, 210, 255) * a);
                Gfx.Fill(sb, new Rectangle(x - 1, y - 2, 2, 4), new Color(235, 250, 255) * a);
            }
        }
    }
}
