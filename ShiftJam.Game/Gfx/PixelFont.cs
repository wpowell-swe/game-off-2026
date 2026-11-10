using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftJam
{
    /// <summary>
    /// Tiny built-in 3x5 pixel font drawn with rectangles, so text works without any font assets.
    /// Uppercase letters, digits and a few symbols. Good enough for banners; replace with a real
    /// bitmap font for dialogue boxes later.
    /// </summary>
    public static class PixelFont
    {
        public const int GlyphW = 3, GlyphH = 5;

        // Each glyph: 5 rows of 3 bits, written as a 15-char string of 0/1.
        private static readonly Dictionary<char, string> Glyphs = new()
        {
            ['A'] = "010101111101101", ['B'] = "110101110101110", ['C'] = "011100100100011",
            ['D'] = "110101101101110", ['E'] = "111100110100111", ['F'] = "111100110100100",
            ['G'] = "011100101101011", ['H'] = "101101111101101", ['I'] = "111010010010111",
            ['J'] = "001001001101010", ['K'] = "101101110101101", ['L'] = "100100100100111",
            ['M'] = "101111111101101", ['N'] = "110101101101101", ['O'] = "010101101101010",
            ['P'] = "110101110100100", ['Q'] = "010101101011001", ['R'] = "110101110101101",
            ['S'] = "011100010001110", ['T'] = "111010010010010", ['U'] = "101101101101111",
            ['V'] = "101101101101010", ['W'] = "101101111111101", ['X'] = "101101010101101",
            ['Y'] = "101101010010010", ['Z'] = "111001010100111",
            ['0'] = "111101101101111", ['1'] = "010110010010111", ['2'] = "110001010100111",
            ['3'] = "110001010001110", ['4'] = "101101111001001", ['5'] = "111100110001110",
            ['6'] = "011100111101111", ['7'] = "111001010010010", ['8'] = "111101111101111",
            ['9'] = "111101111001110",
            ['.'] = "000000000000010", ['!'] = "010010010000010", ['-'] = "000000111000000",
            [':'] = "000010000010000", ['?'] = "110001010000010", ['/'] = "001001010100100",
        };

        public static int Width(string text, int scale = 1) =>
            text.Length == 0 ? 0 : (text.Length * (GlyphW + 1) - 1) * scale;

        public static void Draw(SpriteBatch sb, string text, int x, int y, Color color, int scale = 1)
        {
            int cx = x;
            foreach (char raw in text)
            {
                char c = char.ToUpperInvariant(raw);
                if (Glyphs.TryGetValue(c, out var g))
                {
                    for (int row = 0; row < GlyphH; row++)
                        for (int col = 0; col < GlyphW; col++)
                            if (g[row * GlyphW + col] == '1')
                                Gfx.Fill(sb, new Rectangle(cx + col * scale, y + row * scale, scale, scale), color);
                }
                cx += (GlyphW + 1) * scale;
            }
        }

        /// <summary>Draws text centred horizontally on the 320x180 screen, with a dark shadow for readability.</summary>
        public static void DrawCentered(SpriteBatch sb, string text, int y, Color color, int scale = 1)
        {
            int x = (Metrics.ScreenWidth - Width(text, scale)) / 2;
            Draw(sb, text, x + scale, y + scale, Color.Black, scale);
            Draw(sb, text, x, y, color, scale);
        }
    }
}
