using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftJam
{
    /// <summary>
    /// Optional PNG art for everything that is not a character or a map tile: pickups, doors, HUD frames, the dialogue box,
    /// backdrop layers. Each image is looked up by its content path (for example "Ui/hud_material"). When the PNG does not
    /// exist the caller keeps its code-drawn look, so art can arrive one image at a time. Results (including "missing") are
    /// cached until <see cref="Clear"/> (called by the F11 reload).
    /// </summary>
    public static class Art
    {
        private static ContentManager _content;
        private static readonly Dictionary<string, Texture2D> Cache = new();

        public static void Init(ContentManager content) { _content = content; Cache.Clear(); }
        public static void Clear() => Cache.Clear();

        public static Texture2D Get(string path)
        {
            if (_content == null) return null;
            if (Cache.TryGetValue(path, out var t)) return t;
            try { t = _content.Load<Texture2D>(path); }
            catch (Exception) { t = null; }
            Cache[path] = t;
            return t;
        }

        public static bool Has(string path) => Get(path) != null;

        /// <summary>Stretches the whole image over the rectangle. False when there is no such PNG.</summary>
        public static bool Stretch(SpriteBatch sb, string path, Rectangle dest, Color? tint = null)
        {
            var t = Get(path);
            if (t == null) return false;
            sb.Draw(t, dest, null, tint ?? Color.White);
            return true;
        }

        /// <summary>Draws the image at its own size with its top-left at (x, y).</summary>
        public static bool At(SpriteBatch sb, string path, int x, int y, Color? tint = null)
        {
            var t = Get(path);
            if (t == null) return false;
            sb.Draw(t, new Vector2(x, y), null, tint ?? Color.White);
            return true;
        }

        /// <summary>
        /// A strip of square frames (frame size = image height). Draws the frame for the clock centred in the rectangle
        /// (not scaled). A one-frame image works too.
        /// </summary>
        public static bool Anim(SpriteBatch sb, string path, Rectangle box, float clock, float fps = 6f, Color? tint = null)
        {
            var t = Get(path);
            if (t == null) return false;
            int fs = t.Height, n = Math.Max(1, t.Width / fs);
            int f = n == 1 ? 0 : (int)(clock * fps) % n;
            var pos = new Vector2(box.X + (box.Width - fs) / 2f, box.Y + (box.Height - fs) / 2f);
            sb.Draw(t, new Vector2(MathF.Round(pos.X), MathF.Round(pos.Y)), new Rectangle(f * fs, 0, fs, fs), tint ?? Color.White);
            return true;
        }

        /// <summary>Nine-slice: the corners keep their size, the edges and middle stretch. <paramref name="border"/> is in image pixels.</summary>
        public static bool NineSlice(SpriteBatch sb, string path, Rectangle r, int border, Color? tint = null)
        {
            var t = Get(path);
            if (t == null) return false;
            var c = tint ?? Color.White;
            int b = Math.Min(border, Math.Min(t.Width, t.Height) / 2);
            int[] sx = { 0, b, t.Width - b, t.Width }, sy = { 0, b, t.Height - b, t.Height };
            int[] dx = { r.X, r.X + b, r.Right - b, r.Right }, dy = { r.Y, r.Y + b, r.Bottom - b, r.Bottom };
            for (int j = 0; j < 3; j++)
                for (int i = 0; i < 3; i++)
                {
                    var src = new Rectangle(sx[i], sy[j], sx[i + 1] - sx[i], sy[j + 1] - sy[j]);
                    var dst = new Rectangle(dx[i], dy[j], dx[i + 1] - dx[i], dy[j + 1] - dy[j]);
                    if (src.Width > 0 && src.Height > 0 && dst.Width > 0 && dst.Height > 0) sb.Draw(t, dst, src, c);
                }
            return true;
        }
    }
}
