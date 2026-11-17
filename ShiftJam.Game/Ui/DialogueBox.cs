using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftJam
{
    /// <summary>
    /// A Castlevania-style text box: a bordered black panel across the bottom of the screen with a name tab, text that
    /// types out a letter at a time with a small blip, and a blinking arrow when the page is done. Enter, Space or J
    /// finishes a page that is still typing, and moves on when it is complete. The game freezes while it is open.
    /// </summary>
    public sealed class DialogueBox
    {
        private static float CharsPerSecond = Cfg.F("Dialogue.CharsPerSecond", 48f, v => CharsPerSecond = v);
        private static int LinesPerPage = Cfg.I("Dialogue.LinesPerPage", 4, v => LinesPerPage = v);
        private static int BlipEvery = Cfg.I("Dialogue.BlipEvery", 2, v => BlipEvery = v);

        private const int BoxX = 8, BoxW = 304, BoxH = 62, BoxY = Metrics.ScreenHeight - BoxH - 6;
        private const int Pad = 8;
        private const int CharW = PixelFont.GlyphW + 1;
        private const int LineH = 9;

        private sealed record Page(string Speaker, string[] Lines)
        {
            public int Chars => Total(Lines);
            private static int Total(string[] lines) { int n = 0; foreach (var l in lines) n += l.Length; return n; }
        }

        private readonly Queue<Page> _pages = new();
        private Page _page;
        private float _typed;          // characters revealed so far on this page
        private float _clock;
        private int _lastBlip;

        public bool Active => _page != null;

        /// <summary>Adds lines to the end of whatever is showing (or starts showing them).</summary>
        public void Enqueue(IEnumerable<StoryLine> lines)
        {
            foreach (var line in lines)
                foreach (var page in Paginate(line))
                    _pages.Enqueue(page);
            if (_page == null) Next();
        }

        public void Clear() { _pages.Clear(); _page = null; }

        private void Next()
        {
            _page = _pages.Count > 0 ? _pages.Dequeue() : null;
            _typed = 0; _lastBlip = 0;
        }

        /// <summary>Word-wraps a line to the box and splits it into pages of at most LinesPerPage lines.</summary>
        private static IEnumerable<Page> Paginate(StoryLine line)
        {
            int maxChars = (BoxW - 2 * Pad) / CharW;
            var wrapped = new List<string>();
            string current = "";
            foreach (var word in PixelFont.Normalize(line.Text).ToUpperInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (current.Length > 0 && current.Length + 1 + word.Length > maxChars) { wrapped.Add(current); current = word; }
                else current = current.Length == 0 ? word : current + " " + word;
            }
            if (current.Length > 0) wrapped.Add(current);
            if (wrapped.Count == 0) wrapped.Add("");
            for (int i = 0; i < wrapped.Count; i += LinesPerPage)
                yield return new Page(line.Speaker, wrapped.GetRange(i, Math.Min(LinesPerPage, wrapped.Count - i)).ToArray());
        }

        /// <summary>Types the text out; returns after handling the confirm press. Call only while Active.</summary>
        public void Update(float dt, bool confirm)
        {
            if (_page == null) return;
            _clock += dt;
            bool done = _typed >= _page.Chars;
            if (confirm)
            {
                if (!done) _typed = _page.Chars;
                else Next();
                return;
            }
            if (done) return;
            _typed = Math.Min(_page.Chars, _typed + CharsPerSecond * dt);
            int shown = (int)_typed;
            if (shown / BlipEvery != _lastBlip / BlipEvery) Sfx.Play(SfxId.Blip, 0.5f, 0.05f);
            _lastBlip = shown;
        }

        public void Draw(SpriteBatch sb)
        {
            if (_page == null) return;
            var gold = new Color(255, 225, 140);
            var darkGold = new Color(150, 115, 50);

            // Panel with a double border.
            Gfx.Fill(sb, new Rectangle(BoxX, BoxY, BoxW, BoxH), gold);
            Gfx.Fill(sb, new Rectangle(BoxX + 1, BoxY + 1, BoxW - 2, BoxH - 2), Color.Black);
            Gfx.Fill(sb, new Rectangle(BoxX + 3, BoxY + 3, BoxW - 6, BoxH - 6), darkGold);
            Gfx.Fill(sb, new Rectangle(BoxX + 4, BoxY + 4, BoxW - 8, BoxH - 8), new Color(6, 4, 14));

            // Name tab above the panel's left corner.
            if (!string.IsNullOrEmpty(_page.Speaker))
            {
                string name = PixelFont.Normalize(_page.Speaker).ToUpperInvariant();
                int w = PixelFont.Width(name) + 12;
                var tab = new Rectangle(BoxX + 8, BoxY - 10, w, 12);
                Gfx.Fill(sb, tab, gold);
                Gfx.Fill(sb, new Rectangle(tab.X + 1, tab.Y + 1, tab.Width - 2, tab.Height - 1), Color.Black);
                PixelFont.Draw(sb, name, tab.X + 6, tab.Y + 4, gold);
            }

            // The text, revealed up to _typed characters.
            int left = (int)_typed;
            for (int i = 0; i < _page.Lines.Length && left > 0; i++)
            {
                string line = _page.Lines[i];
                string shown = line.Length <= left ? line : line.Substring(0, left);
                PixelFont.Draw(sb, shown, BoxX + Pad, BoxY + 9 + i * LineH, Color.White);
                left -= line.Length;
            }

            // Blinking arrow when the page is finished: down for "more", a square for "last".
            if (_typed >= _page.Chars && ((int)(_clock * 3f) & 1) == 0)
            {
                int ax = BoxX + BoxW - 16, ay = BoxY + BoxH - 13;
                if (_pages.Count > 0)
                {
                    Gfx.Fill(sb, new Rectangle(ax, ay, 7, 1), gold);
                    Gfx.Fill(sb, new Rectangle(ax + 1, ay + 1, 5, 1), gold);
                    Gfx.Fill(sb, new Rectangle(ax + 2, ay + 2, 3, 1), gold);
                    Gfx.Fill(sb, new Rectangle(ax + 3, ay + 3, 1, 1), gold);
                }
                else Gfx.Fill(sb, new Rectangle(ax + 1, ay, 5, 4), gold);
            }
        }
    }
}
