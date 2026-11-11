using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftJam
{
    /// <summary>
    /// The Tithe's Clerk, the final boss. Its attacks come in three kinds: its own plane, the OPPOSITE plane (so being in
    /// the Clerk's own plane is not always the dangerous place), and BOTH planes (shifting does not dodge it; moving does).
    /// Four health bars, each in its own catacomb room with its own trick.
    /// Empty a bar and the Clerk flees to the next room (see HoldTransition), opening the gate on the way.
    ///   Bar 1 the Stacks, Material:  guard-breaking stamp columns and swipes.
    ///   Bar 2 the Ledger Hall, Astral: floats and fires ledger pages; they hurt only an Astral player.
    ///   Bar 3 the Crypt, Material: vanishes and reappears behind you, marked on the floor first.
    ///   Bar 4 the Vault, both: flips plane mid-fight; you can only hurt it in the plane it is in.
    /// </summary>
    public sealed class Clerk : Boss
    {
        public static readonly Point BodySize = new(18, 30);

        // ---- Tuning (private to this class) ----
        private const int BarHealth = 96;                 // twelve light hits (or about three heavies) per bar
        private const int BarCount = 4;
        private const float WalkSpeed = 52f;
        private const float HoverSpeed = 50f;
        private const float FleeSpeed = 150f;
        private const float SwipeWind = 0.45f, SwipeRecover = 0.7f;
        private const int SwipeW = 26, SwipeDamage = 16;
        private const float StampWind = 0.9f, StampRecover = 1.1f, StampShow = 0.15f;
        private const int StampW = 18, StampDamage = 14;
        private const float FanWind = 0.6f, FanRecover = 0.9f;
        private const float RainWind = 0.9f, RainRecover = 1.0f;
        private const int PageSize = 8, PageDamage = 10;
        private const float PageSpeed = 105f, RainSpeed = 130f, PageLife = 3.2f;
        private const float VanishWind = 0.35f, VanishHidden = 0.9f, VanishRecover = 0.9f;
        private const int VanishW = 44, VanishDamage = 16;
        private const float FlipEvery = 3.2f, FlipWind = 0.5f;
        private const int Ceiling = Metrics.TileSize;     // columns and rain start under the top wall
        private const int Tile = Metrics.TileSize;
        private const float WaveWind = 0.7f, WaveRecover = 0.9f, WaveSpeed = 150f, WaveRange = 260f;
        private const int WaveW = 18, WaveH = 10, WaveDamage = 12;

        private enum K { Swipe, Stamp, Fan, Rain, Vanish, Wave }
        private enum M { Own, Opp, Both }   // which plane an attack hits: the Clerk's own, the other, or both
        private enum S { Idle, Wind, Hidden, Recover, FlipWind }

        private static readonly (K, M)[][] Sequences =
        {
            new[] { (K.Stamp, M.Own), (K.Swipe, M.Own), (K.Stamp, M.Opp), (K.Swipe, M.Both), (K.Stamp, M.Both) },
            new[] { (K.Fan, M.Own), (K.Wave, M.Opp), (K.Rain, M.Own), (K.Fan, M.Both), (K.Rain, M.Opp) },
            new[] { (K.Vanish, M.Own), (K.Swipe, M.Own), (K.Stamp, M.Opp), (K.Vanish, M.Both), (K.Swipe, M.Both), (K.Vanish, M.Opp) },
            null,   // the last bar picks by plane (see Next)
        };
        private static readonly (K, M)[] VaultMaterial = { (K.Stamp, M.Both), (K.Swipe, M.Opp), (K.Vanish, M.Own), (K.Stamp, M.Opp), (K.Wave, M.Both) };
        private static readonly (K, M)[] VaultAstral = { (K.Fan, M.Both), (K.Wave, M.Opp), (K.Rain, M.Own), (K.Fan, M.Opp), (K.Rain, M.Both) };

        private static readonly BossPhase[] Table =
        {
            new(Plane.Material, BarHealth * 3, 0, "The Stacks"),
            new(Plane.Astral, BarHealth * 2, 1, "The Ledger Hall"),
            new(Plane.Material, BarHealth, 2, "The Crypt"),
            new(Plane.Astral, 0, 3, "The Vault"),
        };

        private S _s = S.Idle;
        private K _k;
        private M _mode;
        private bool _teleBoth;
        private float _timer, _idleTotal, _flipClock, _teleProgress, _slam;
        private int _seq;
        private float _floorBottom;
        private float _spotX;
        private readonly Rectangle[] _tele = new Rectangle[4];
        private Plane _teleIs = Plane.Material;
        private readonly Rectangle[] _slamRects = new Rectangle[4];

        public override int Bars => BarCount;
        public override int BarIndex => PhaseIndex;
        public override float BarFraction => MathHelper.Clamp((Health - Phase.EndHealth) / (float)BarHealth, 0f, 1f);

        public Clerk(Vector2 position, int startBar) : base("The Clerk", BarHealth * BarCount, BodySize, position, Table)
        {
            _floorBottom = position.Y + BodySize.Y;
            if (startBar > 0) SkipToPhase(startBar);
        }

        protected override void OnPhaseStart(int index, GameWorld world)
        {
            ClerkArena.Checkpoint = Math.Max(ClerkArena.Checkpoint, index);
            ResetPattern(1.0f);
        }
        protected override void OnPhaseEnd(int index, GameWorld world) => ResetPattern(1.0f);
        protected override void OnPlayerGone(GameWorld world) => ResetPattern(1.0f);

        private void ResetPattern(float idle)
        {
            _s = S.Idle; _timer = idle; _idleTotal = 0; _flipClock = 0;
            Array.Clear(_tele); Array.Clear(_slamRects); _slam = 0;
            Intangible = false;
            PlaneOverride = null;
        }

        /// <summary>After a bar empties: run right to the next room, opening its gate, and wait for the player to follow.</summary>
        protected override bool HoldTransition(float dt, GameWorld world)
        {
            int next = PhaseIndex + 1;
            float doorX = ClerkArena.RoomStart(next) * Tile;
            float stopX = doorX + 9 * Tile;
            PlaneOverride = Plane.Material;          // run along the floor whatever plane the bar was in
            var b = Body;
            if (b.Center.X > doorX - 3 * Tile) ClerkArena.OpenGate(world, next);
            if (b.Center.X < stopX) { Facing = 1; Velocity.X = FleeSpeed; return true; }
            Velocity.X = 0;
            Facing = -1;
            return world.Player.IsAlive && world.Player.Hurtbox.Center.X < doorX + Tile;
        }

        private void Face(float dx) { if (MathF.Abs(dx) > 4f) Facing = dx > 0 ? 1 : -1; }

        private static Plane Other(Plane p) => p == Plane.Astral ? Plane.Material : Plane.Astral;
        private Plane ModePlane(M m) => m == M.Opp ? Other(Plane) : Plane;

        /// <summary>Damage in the plane(s) the current attack's mode picks.</summary>
        private bool Hurt(GameWorld world, Rectangle area, int damage, float knockback, bool guard = false) => _mode switch
        {
            M.Both => world.SpawnHitBoth(area, damage, Facing, knockback, guard, this) > 0,
            M.Opp => world.SpawnHit(new Hit(area, damage, Facing, Other(Plane), knockback, guard), this) > 0,
            _ => Strike(world, area, damage, knockback, guard),
        };

        private (K, M) Next()
        {
            (K, M)[] seq = PhaseIndex < 3 ? Sequences[PhaseIndex] : (Plane == Plane.Astral ? VaultAstral : VaultMaterial);
            return seq[_seq % seq.Length];
        }

        protected override void UpdatePhase(float dt, GameWorld world, BossPhase phase)
        {
            var p = world.Player;
            var b = Body;
            float dx = p.Hurtbox.Center.X - b.Center.X;
            bool astral = Plane == Plane.Astral;
            float vx = 0;
            Intangible = _s == S.Hidden;
            Array.Clear(_tele);
            if (_slam > 0) _slam -= dt;
            _timer -= dt;
            if (PhaseIndex == 3 && _s == S.Idle) _flipClock += dt;
            if (OnGround) _floorBottom = b.Bottom;

            switch (_s)
            {
                case S.Idle:
                {
                    _idleTotal += dt;
                    Face(dx);
                    var (next, _) = Next();
                    float stand = next == K.Swipe ? 22f : astral ? 85f : 70f;
                    if (MathF.Abs(dx) > stand + 8) vx = Math.Sign(dx) * (astral ? HoverSpeed : WalkSpeed);
                    else if (astral && MathF.Abs(dx) < stand - 25) vx = -Math.Sign(dx) * HoverSpeed;
                    bool close = next != K.Swipe || MathF.Abs(dx) <= 32f || _idleTotal > 3f;
                    if (PhaseIndex == 3 && _flipClock >= FlipEvery) { _s = S.FlipWind; _timer = FlipWind; Velocity.X = 0; break; }
                    if (_timer <= 0 && close) Start(world, p);
                    break;
                }

                case S.FlipWind:
                    _teleProgress = 1f - _timer / FlipWind;
                    if (_timer <= 0)
                    {
                        PlaneOverride = astral ? Plane.Material : null;     // null falls back to the bar's own plane, Astral
                        _flipClock = 0; _s = S.Idle; _timer = 0.5f;
                        var hb = Body;
                        world.Sparks.Sparks(new Vector2(hb.Center.X, hb.Center.Y), Plane, 12, 0);
                        Sfx.Play(Plane == Plane.Astral ? SfxId.ShiftIn : SfxId.ShiftOut);
                        world.Camera.Shake(1.5f, 0.12f);
                        if (Plane == Plane.Astral) Velocity.Y = -50f;
                    }
                    break;

                case S.Wind:
                    Face(dx);
                    Windup(world, p, b);
                    break;

                case S.Hidden:
                    _teleProgress = 1f - _timer / VanishHidden;
                    _tele[0] = VanishArea(); _teleIs = ModePlane(_mode); _teleBoth = _mode == M.Both;
                    if (_timer <= 0)
                    {
                        Position = new Vector2(_spotX - b.Width / 2f, _floorBottom - b.Height);
                        Intangible = false;
                        Facing = dx >= 0 ? 1 : -1;
                        Hurt(world, VanishArea(), VanishDamage, 140f);
                        world.Camera.Shake(1.5f, 0.1f);
                        Sfx.Play(SfxId.HeavyHit, 0.5f, 0.1f);
                        _s = S.Recover; _timer = VanishRecover;
                    }
                    break;

                case S.Recover:
                    if (_timer <= 0) { _s = S.Idle; _timer = PhaseIndex == 3 ? 0.55f : 0.8f; _idleTotal = 0; }
                    break;
            }

            // Hover while Astral, otherwise stand (gravity does the rest).
            if (astral)
            {
                float targetY = _floorBottom - 40f - b.Height;
                Velocity.Y = MoveToward(Velocity.Y, MathHelper.Clamp((targetY - Position.Y) * 4f, -60f, 60f), 300f * dt);
            }
            Velocity.X = MoveToward(Velocity.X, _s == S.Idle ? vx : 0f, 700f * dt);
        }

        private Rectangle VanishArea() => new((int)_spotX - VanishW / 2, (int)_floorBottom - BodySize.Y, VanishW, BodySize.Y);

        private void Start(GameWorld world, Player p)
        {
            (_k, _mode) = Next(); _seq++;
            var kind = _k;
            var b = Body;
            _teleProgress = 0;
            switch (kind)
            {
                case K.Swipe: _s = S.Wind; _timer = SwipeWind; break;
                case K.Stamp: _s = S.Wind; _timer = StampWind; break;
                case K.Fan: _s = S.Wind; _timer = FanWind; break;
                case K.Wave: _s = S.Wind; _timer = WaveWind; break;
                case K.Rain: _s = S.Wind; _timer = RainWind; break;
                case K.Vanish:
                    _s = S.Wind; _timer = VanishWind;
                    break;
            }
            // Where the stamp columns and rain fall is fixed now, so the player can read it and move.
            if (kind == K.Stamp || kind == K.Rain) PickColumns(p, kind);
        }

        private float[] _cols = Array.Empty<float>();

        private void PickColumns(Player p, K kind)
        {
            float px = p.Hurtbox.Center.X;
            _cols = kind == K.Stamp
                ? (PhaseIndex == 3 ? new[] { px - 30, px + 30 } : new[] { px - 44, px, px + 44 })
                : new[] { px - 66, px - 22, px + 22, px + 66 };
        }

        private Rectangle ColumnRect(float cx, bool full) =>
            full ? new Rectangle((int)cx - StampW / 2, Ceiling, StampW, (int)_floorBottom - Ceiling)
                 : new Rectangle((int)cx - StampW / 2, (int)_floorBottom - 8, StampW, 8);

        private void Windup(GameWorld world, Player p, Rectangle b)
        {
            switch (_k)
            {
                case K.Swipe:
                    _teleProgress = 1f - _timer / SwipeWind; _teleIs = ModePlane(_mode); _teleBoth = _mode == M.Both;
                    _tele[0] = SwipeArea(b);
                    if (_timer <= 0)
                    {
                        Hurt(world, SwipeArea(b), SwipeDamage, 130f);
                        Sfx.Play(SfxId.Swing, 0.6f, 0.1f);
                        _s = S.Recover; _timer = SwipeRecover;
                    }
                    break;

                case K.Stamp:
                    _teleProgress = 1f - _timer / StampWind; _teleIs = ModePlane(_mode); _teleBoth = _mode == M.Both;
                    for (int i = 0; i < _cols.Length; i++) _tele[i] = ColumnRect(_cols[i], true);
                    if (_timer <= 0)
                    {
                        for (int i = 0; i < _cols.Length; i++)
                        {
                            _slamRects[i] = ColumnRect(_cols[i], true);
                            Hurt(world, _slamRects[i], StampDamage, 90f, true);
                            world.Sparks.Sparks(new Vector2(_cols[i], _floorBottom - 4), Plane, 6, 0);
                        }
                        _slam = StampShow;
                        world.Camera.Shake(2.5f, 0.15f);
                        Sfx.Play(SfxId.HeavyHit, 0.8f, 0.1f);
                        _s = S.Recover; _timer = StampRecover;
                    }
                    break;

                case K.Fan:
                    _teleProgress = 1f - _timer / FanWind; _teleIs = ModePlane(_mode); _teleBoth = _mode == M.Both;
                    if (_timer <= 0)
                    {
                        var from = new Vector2(b.Center.X, b.Center.Y);
                        var aim = new Vector2(p.Hurtbox.Center.X, p.Hurtbox.Center.Y) - from;
                        float baseAng = MathF.Atan2(aim.Y, aim.X);
                        for (int i = -1; i <= 1; i++)
                        {
                            float a = baseAng + i * 0.24f;
                            world.Add(new Page(this, from, new Vector2(MathF.Cos(a), MathF.Sin(a)) * PageSpeed, ModePlane(_mode), _mode == M.Both));
                        }
                        Sfx.Play(SfxId.Swing, 0.5f, 0.2f);
                        _s = S.Recover; _timer = FanRecover;
                    }
                    break;

                case K.Rain:
                    _teleProgress = 1f - _timer / RainWind; _teleIs = ModePlane(_mode); _teleBoth = _mode == M.Both;
                    for (int i = 0; i < _cols.Length; i++) _tele[i] = ColumnRect(_cols[i], true);
                    if (_timer <= 0)
                    {
                        for (int i = 0; i < _cols.Length; i++)
                            world.Add(new Page(this, new Vector2(_cols[i], Ceiling + 4), new Vector2(0, RainSpeed), ModePlane(_mode), _mode == M.Both));
                        Sfx.Play(SfxId.Swing, 0.5f, 0.2f);
                        _s = S.Recover; _timer = RainRecover;
                    }
                    break;

                case K.Wave:
                    _teleProgress = 1f - _timer / WaveWind; _teleIs = ModePlane(_mode); _teleBoth = _mode == M.Both;
                    _tele[0] = new Rectangle(Facing > 0 ? b.Right : b.Left - 56, (int)_floorBottom - WaveH, 56, WaveH);
                    if (_timer <= 0)
                    {
                        world.Add(new Wave(this, new Vector2(Facing > 0 ? b.Right : b.Left - WaveW, _floorBottom - WaveH), Facing, ModePlane(_mode), _mode == M.Both));
                        Sfx.Play(SfxId.HeavyHit, 0.5f, 0.15f);
                        _s = S.Recover; _timer = WaveRecover;
                    }
                    break;

                case K.Vanish:
                    _teleProgress = _timer <= 0 ? 1f : 1f - _timer / VanishWind;
                    if (_timer <= 0)
                    {
                        // Reappear behind the player (the side away from the Clerk), marked on the floor first.
                        int side = p.Hurtbox.Center.X >= b.Center.X ? 1 : -1;
                        _spotX = p.Hurtbox.Center.X + side * 34;
                        _spotX = MathHelper.Clamp(_spotX, Tile * 2, (world.Tiles.Width - 2) * Tile);
                        _s = S.Hidden; _timer = VanishHidden;
                        Sfx.Play(SfxId.ShiftOut, 0.5f, 0.2f);
                        world.Sparks.Sparks(new Vector2(b.Center.X, b.Center.Y), Plane, 10, 0);
                    }
                    break;
            }
        }

        private Rectangle SwipeArea(Rectangle b) =>
            new(Facing > 0 ? b.Right : b.Left - SwipeW, b.Y, SwipeW, b.Height);

        // ---- Drawing ----

        protected override void DrawBody(SpriteBatch sb, Rectangle r, float flash, float alpha)
        {
            if (_s == S.Hidden) return;
            if (_s == S.FlipWind && ((int)(Clock * 24f) & 1) == 0) flash = Math.Max(flash, 0.7f);
            bool astral = Plane == Plane.Astral;
            Color coat = astral ? new Color(100, 140, 225) : new Color(48, 46, 66);
            Color trim = astral ? new Color(210, 240, 255) : new Color(225, 220, 205);
            Color face = astral ? new Color(235, 248, 255) : new Color(205, 200, 190);
            Color ink = astral ? new Color(90, 210, 255) : new Color(150, 40, 50);
            int f = Facing;

            Gfx.Fill(sb, new Rectangle(r.X + 1, r.Y + 8, r.Width - 2, r.Height - 8), Shade(coat, flash, alpha));      // long coat
            Gfx.Fill(sb, new Rectangle(r.X + 6, r.Y + 8, r.Width - 12, 6), Shade(trim, flash, alpha));                // collar
            Gfx.Fill(sb, new Rectangle(r.X + 3, r.Y, r.Width - 6, 9), Shade(face, flash, alpha));                     // blank face
            Gfx.Fill(sb, new Rectangle(r.X + 2, r.Y - 5, r.Width - 4, 6), Shade(coat, flash, alpha));                 // tall hat
            Gfx.Fill(sb, new Rectangle(r.X + (f > 0 ? 10 : 4), r.Y + 4, 3, 2), Shade(Color.Black, 0, alpha));
            Gfx.Fill(sb, new Rectangle(r.X + 4, r.Y + 17, r.Width - 8, 2), Shade(ink, flash, alpha));                 // ink stripe

            bool raising = _s == S.Wind && _k == K.Stamp;
            int armY = raising ? r.Y - 10 : r.Y + 14;
            Gfx.Fill(sb, new Rectangle(f > 0 ? r.Right - 3 : r.Left - 4, armY, 7, raising ? 10 : 5), Shade(trim, flash, alpha));
            if (raising) Gfx.Fill(sb, new Rectangle(f > 0 ? r.Right - 5 : r.Left - 6, armY - 5, 11, 5), Shade(ink, flash, alpha));  // the stamp
        }

        protected override void DrawOverlay(SpriteBatch sb, Rectangle r)
        {
            for (int i = 0; i < _tele.Length; i++)
                if (_tele[i].Width > 0) DrawTelegraph(sb, _tele[i], _teleIs, _teleProgress, _teleBoth);
            if (_slam > 0)
                foreach (var s in _slamRects)
                    if (s.Width > 0) Gfx.Fill(sb, s, Color.White * (0.7f * (_slam / StampShow)));
        }

        /// <summary>A ledger page. Flies straight; Astral pages hurt only an Astral player, Material ones only a Material player, violet ones both.</summary>
        private sealed class Page : IEntity
        {
            private readonly Clerk _owner;
            private Vector2 _pos;
            private readonly Vector2 _vel;
            private readonly Plane _plane;
            private readonly bool _both;
            private float _life = PageLife, _t;

            public Page(Clerk owner, Vector2 pos, Vector2 vel, Plane plane, bool both) { _owner = owner; _pos = pos; _vel = vel; _plane = plane; _both = both; }

            public void Update(float dt, GameWorld world)
            {
                _t += dt; _life -= dt;
                if (_life <= 0 || _owner.ClearHazards) { world.Entities.Remove(this); return; }
                _pos += _vel * dt;
                int tx = (int)MathF.Floor(_pos.X / Tile), ty = (int)MathF.Floor(_pos.Y / Tile);
                if (world.Tiles != null && world.Tiles.IsSolid(tx, ty, _both ? Plane.Astral : _plane)) { world.Entities.Remove(this); return; }
                if (world.Player == null || !world.Player.IsAlive) return;
                var area = new Rectangle((int)MathF.Round(_pos.X) - PageSize / 2, (int)MathF.Round(_pos.Y) - PageSize / 2, PageSize, PageSize);
                int face = _vel.X >= 0 ? 1 : -1;
                int hit = _both ? world.SpawnHitBoth(area, PageDamage, face, 70f, false, _owner)
                                : world.SpawnHit(new Hit(area, PageDamage, face, _plane, 70f, false), _owner);
                if (hit > 0) world.Entities.Remove(this);
            }

            public void Draw(SpriteBatch sb)
            {
                float a = MathHelper.Clamp(_life / 0.5f, 0f, 1f) * (0.75f + 0.25f * MathF.Sin(_t * 16f));
                int x = (int)MathF.Round(_pos.X), y = (int)MathF.Round(_pos.Y);
                Color glow = _both ? new Color(220, 120, 255) : _plane == Plane.Astral ? new Color(150, 220, 255) : new Color(255, 150, 80);
                Color line = _both ? new Color(150, 60, 220) : _plane == Plane.Astral ? new Color(70, 140, 230) : new Color(200, 80, 40);
                Gfx.Fill(sb, new Rectangle(x - 5, y - 4, 10, 8), glow * (a * 0.35f));
                Gfx.Fill(sb, new Rectangle(x - 3, y - 4, 7, 8), new Color(245, 242, 235) * a);
                Gfx.Fill(sb, new Rectangle(x - 2, y - 2, 5, 1), line * a);
                Gfx.Fill(sb, new Rectangle(x - 2, y, 5, 1), line * a);
            }
        }

        /// <summary>A low wave along the floor. Hurts the plane(s) it was sent in; jump it. Stops at walls and after a fixed range.</summary>
        private sealed class Wave : IEntity
        {
            private readonly Clerk _owner;
            private float _x;
            private readonly float _y;
            private readonly int _dir;
            private readonly Plane _plane;
            private readonly bool _both;
            private float _travelled;

            public Wave(Clerk owner, Vector2 pos, int dir, Plane plane, bool both) { _owner = owner; _x = pos.X; _y = pos.Y; _dir = dir; _plane = plane; _both = both; }

            private Rectangle Box => new((int)MathF.Round(_x), (int)MathF.Round(_y), WaveW, WaveH);

            public void Update(float dt, GameWorld world)
            {
                if (_owner.ClearHazards) { world.Entities.Remove(this); return; }
                float step = WaveSpeed * dt;
                _x += _dir * step; _travelled += step;
                var box = Box;
                int front = _dir > 0 ? box.Right : box.Left - 1;
                if (_travelled >= WaveRange || (world.Tiles != null && world.Tiles.IsSolid((int)MathF.Floor(front / (float)Tile), (int)MathF.Floor((box.Y + WaveH / 2) / (float)Tile), _both ? Plane.Astral : _plane)))
                { world.Entities.Remove(this); return; }
                if (world.Player == null || !world.Player.IsAlive) return;
                int hit = _both ? world.SpawnHitBoth(box, WaveDamage, _dir, 100f, false, _owner)
                                : world.SpawnHit(new Hit(box, WaveDamage, _dir, _plane, 100f, false), _owner);
                if (hit > 0) world.Entities.Remove(this);
            }

            public void Draw(SpriteBatch sb)
            {
                var r = Box;
                Color c = _both ? new Color(220, 120, 255) : _plane == Plane.Astral ? new Color(110, 200, 255) : new Color(255, 140, 50);
                Gfx.Fill(sb, r, c);
                Gfx.Fill(sb, new Rectangle(r.X + 2, r.Y + 3, r.Width - 4, r.Height - 3), Color.Lerp(c, Color.White, 0.6f));
            }
        }
    }
}
