using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftJam
{
    /// <summary>A loaded strip: the normal texture plus its Astral (blue-violet) twin.</summary>
    public sealed class SpriteSheet
    {
        public SpriteDef Def { get; }
        public Texture2D Normal { get; }
        public Texture2D Astral { get; }

        public SpriteSheet(SpriteDef def, Texture2D normal, Texture2D astral)
        {
            Def = def; Normal = normal; Astral = astral;
        }

        public Rectangle Source(int frame) =>
            new(frame * Def.FrameWidth, 0, Def.FrameWidth, Def.FrameHeight);
    }

    /// <summary>
    /// Loads every sprite in <see cref="SpriteDefs"/>. A missing or broken PNG is simply skipped, so artists
    /// can add one animation at a time; entities fall back to rectangles for anything missing.
    /// </summary>
    public static class SpriteStore
    {
        private static readonly Dictionary<string, SpriteSheet> Sheets = new();

        /// <summary>Call once from LoadContent, after Gfx.Init.</summary>
        public static void Load(ContentManager content)
        {
            Sheets.Clear();
            foreach (var pair in SpriteDefs.All)
            {
                var def = pair.Value;
                try
                {
                    var tex = content.Load<Texture2D>(def.Asset);
                    if (tex.Width < def.FrameWidth * def.FrameCount || tex.Height < def.FrameHeight)
                    {
                        Console.WriteLine($"[sprites] {def.Asset}: expected {def.FrameWidth * def.FrameCount}x{def.FrameHeight}, " +
                                          $"got {tex.Width}x{tex.Height}; using placeholder rectangles.");
                        continue;
                    }
                    Sheets[pair.Key] = new SpriteSheet(def, tex, MakeAstral(tex));
                }
                catch (Exception)
                {
                    // Not drawn yet (or unreadable): the entity keeps its rectangle look for this animation.
                }
            }
            Console.WriteLine($"[sprites] loaded {Sheets.Count} of {SpriteDefs.All.Count} animation strips.");
        }

        public static SpriteSheet Get(string character, string animation) =>
            Sheets.TryGetValue(character + "_" + animation, out var s) ? s : null;

        // Cold ramp: shadows -> deep violet, midtones -> blue-violet, highlights -> pale ice.
        private static readonly Color[] Ramp =
        {
            new(14, 10, 52), new(70, 60, 190), new(150, 160, 255), new(220, 240, 255),
        };

        private const float AstralOpacity = 0.8f;

        /// <summary>Remaps colour by luminance onto the ramp on the CPU. Alpha is kept (and lowered a little).</summary>
        private static Texture2D MakeAstral(Texture2D src)
        {
            var data = new Color[src.Width * src.Height];
            src.GetData(data);
            for (int i = 0; i < data.Length; i++)
            {
                var c = data[i];
                if (c.A == 0) { data[i] = Color.Transparent; continue; }
                float lum = (0.299f * c.R + 0.587f * c.G + 0.114f * c.B) / 255f;
                float p = Math.Clamp(lum, 0f, 1f) * (Ramp.Length - 1);
                int lo = Math.Min((int)p, Ramp.Length - 2);
                var m = Color.Lerp(Ramp[lo], Ramp[lo + 1], p - lo);
                // Output is premultiplied so it blends correctly with BlendState.AlphaBlend.
                float a = c.A / 255f * AstralOpacity;
                data[i] = new Color((int)(m.R * a), (int)(m.G * a), (int)(m.B * a), (int)(255 * a));
            }
            var tex = new Texture2D(src.GraphicsDevice, src.Width, src.Height);
            tex.SetData(data);
            return tex;
        }
    }
}
