using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftJam
{
    /// <summary>
    /// Plays animations for one entity. Call <see cref="Play"/> every Draw with the animation you want;
    /// it restarts only when the name changes. Returns false when there is no art, so the caller can
    /// draw its placeholder rectangles instead.
    /// </summary>
    public sealed class Animator
    {
        private static readonly Stopwatch Clock = Stopwatch.StartNew();
        private static float Now => (float)Clock.Elapsed.TotalSeconds;

        private readonly string _character;
        private string _name;
        private float _start;
        private float _progress = -1f;   // >= 0: frame chosen by progress (0..1) instead of by time
        private SpriteSheet _sheet;

        public Animator(string character) { _character = character; }

        public string Current => _name;

        /// <summary>True once a non-looping animation has shown its last frame for a full frame time.</summary>
        public bool Finished =>
            _sheet != null && !_sheet.Def.Loop && _progress < 0 && (Now - _start) * _sheet.Def.Fps >= _sheet.Def.FrameCount;

        /// <summary>Play the first of the named animations that has art. Time-driven. Returns false if none has.</summary>
        public bool Play(params string[] names)
        {
            _progress = -1f;
            return Select(names);
        }

        /// <summary>Show the animation spread over progress 0..1 (used when game logic owns the timing, like attacks).</summary>
        public bool Scrub(float progress, params string[] names)
        {
            if (!Select(names)) return false;
            _progress = Clamp01(progress);
            return true;
        }

        private bool Select(string[] names)
        {
            foreach (var n in names)
            {
                var sheet = SpriteStore.Get(_character, n);
                if (sheet == null) continue;
                if (n != _name) { _name = n; _start = Now; }
                _sheet = sheet;
                return true;
            }
            _sheet = null;
            _name = null;
            return false;
        }

        public int FrameIndex
        {
            get
            {
                if (_sheet == null) return 0;
                int count = _sheet.Def.FrameCount;
                if (_progress >= 0) return System.Math.Min(count - 1, (int)(_progress * count));
                int f = (int)((Now - _start) * _sheet.Def.Fps);
                return _sheet.Def.Loop ? f % count : System.Math.Min(count - 1, f);
            }
        }

        /// <summary>
        /// Draws the current frame. <paramref name="feet"/> is the bottom centre of the hitbox in world pixels.
        /// The art faces right; facing -1 mirrors it around the origin so the body does not slide sideways.
        /// </summary>
        public void Draw(SpriteBatch sb, Point feet, int facing, bool astral, Color tint)
        {
            if (_sheet == null) return;
            var d = _sheet.Def;
            bool flip = facing < 0;
            int ox = flip ? d.FrameWidth - d.OriginX : d.OriginX;
            var pos = new Vector2(feet.X - ox, feet.Y - d.OriginY);
            var tex = astral ? _sheet.Astral : _sheet.Normal;
            sb.Draw(tex, pos, _sheet.Source(FrameIndex), tint, 0f, Vector2.Zero, 1f,
                flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
        }

        private static float Clamp01(float v) => v < 0 ? 0 : v > 1 ? 1 : v;
    }
}
