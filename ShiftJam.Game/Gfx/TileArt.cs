using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftJam
{
    /// <summary>
    /// Optional PNG art for level tiles: ShiftJamContent/Tiles/&lt;name&gt;.png. Each image is one tile (16 px tall, 16 px wide),
    /// or several tiles side by side (16, 32, 48... px wide); with more than one, the game picks a variant per map cell so
    /// walls do not look stamped. A tile with no PNG keeps the code-drawn look, so art can arrive one tile at a time.
    /// Names: solid, solid_top (a solid tile with open air above), wall, astral, gate, spikes, shrine, shrine_lit.
    /// </summary>
    public static class TileArt
    {
        public static readonly string[] Names =
            { "solid", "solid_top", "wall", "astral", "gate", "spikes", "shrine", "shrine_lit" };

        private static readonly Dictionary<string, Texture2D> Tex = new();

        public static void Load(ContentManager content)
        {
            Tex.Clear();
            foreach (var n in Names)
            {
                try
                {
                    var t = content.Load<Texture2D>("Tiles/" + n);
                    if (t.Height != Metrics.TileSize || t.Width % Metrics.TileSize != 0)
                    {
                        Console.WriteLine($"[tiles] Tiles/{n}: expected {Metrics.TileSize} px tall and a multiple of {Metrics.TileSize} wide, got {t.Width}x{t.Height}; using code-drawn tile.");
                        continue;
                    }
                    Tex[n] = t;
                }
                catch (Exception) { /* no PNG yet: the code-drawn tile is used */ }
            }
            Console.WriteLine($"[tiles] loaded {Tex.Count} of {Names.Length} tile images.");
        }

        /// <summary>Draws the named tile at map cell (x, y) and returns true; false if there is no PNG for it.</summary>
        public static bool Draw(SpriteBatch sb, string name, int x, int y, Color? tint = null)
        {
            if (!Tex.TryGetValue(name, out var t)) return false;
            int T = Metrics.TileSize, n = t.Width / T;
            int v = n == 1 ? 0 : (int)(((uint)(x * 73856093) ^ (uint)(y * 19349663)) % (uint)n);
            sb.Draw(t, new Vector2(x * T, y * T), new Rectangle(v * T, 0, T, T), tint ?? Color.White);
            return true;
        }
    }
}
