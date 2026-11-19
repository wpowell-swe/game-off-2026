using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftJam
{
    /// <summary>
    /// The scenery behind a stage: a sky gradient plus parallax layers (skylines, arches, columns, stars, mist), all
    /// described per stage in Data/backgrounds.json. Layers are drawn procedurally and tile forever, so no art is needed,
    ///.
    /// The colours blend smoothly between the Material and Astral palettes when the player shifts.
    /// </summary>
    public sealed class Backdrop
    {
        private sealed class Layer
        {
            public string Kind = "skyline";
            public float Factor = 0.3f, YFactor = 0.05f, Drift;
            public int BaseY = 150, Spacing = 40, MinH = 20, MaxH = 60, MinW = 16, MaxW = 36, Seed = 1;
            public string File = "";
            public Color Tint = Color.White, AstralTint = new(150, 170, 255);
            public float Windows = 0.3f, Density = 0.5f;
            public Color Mat = new(30, 24, 44), Astral = new(20, 28, 64), Lit = new(255, 200, 110), AstralLit = new(120, 180, 255);
        }

        private sealed class Theme
        {
            public Color SkyTopMat = new(20, 16, 28), SkyBotMat = new(34, 26, 44);
            public Color SkyTopAstral = new(12, 18, 44), SkyBotAstral = new(22, 34, 80);
            public string SkyImage = "";
            public readonly List<Layer> Layers = new();
        }

        private static Dictionary<string, Theme> _themes;
        private static Theme _default;

        private Theme _theme = new();
        private float _astral;       // 0 = Material, 1 = Astral
        private float _time;

        public static void Reload() { _themes = null; }

        /// <summary>Choose the theme for a stage id such as "level:streets" or "keep" (falls back to "default").</summary>
        public void Use(string stageId, bool astral)
        {
            EnsureLoaded();
            _theme = _themes.TryGetValue(stageId ?? "", out var t) ? t : _default;
            _astral = astral ? 1f : 0f;
        }

        public void Update(float dt, bool astral)
        {
            _time += dt;
            float target = astral ? 1f : 0f;
            float step = dt * 4f;
            _astral = _astral < target ? MathF.Min(target, _astral + step) : MathF.Max(target, _astral - step);
        }

        /// <summary>Draws in screen space (call before the world batch, with no camera transform).</summary>
        public void Draw(SpriteBatch sb, Vector2 cam)
        {
            int w = Metrics.ScreenWidth, h = Metrics.ScreenHeight;
            if (_theme.SkyImage == "" || !Art.Stretch(sb, "Backdrops/" + _theme.SkyImage, new Rectangle(0, 0, w, h), Mix(Color.White, new Color(150, 170, 255))))
                DrawSky(sb, w, h);
            foreach (var l in _theme.Layers)
            {
                float sx = cam.X * l.Factor + _time * l.Drift;
                float sy = cam.Y * l.YFactor;
                switch (l.Kind)
                {
                    case "skyline": DrawBlocks(sb, l, sx, sy, w, windows: true); break;
                    case "arches": DrawArches(sb, l, sx, sy, w); break;
                    case "columns": DrawColumns(sb, l, sx, sy, w, h); break;
                    case "stars": DrawStars(sb, l, sx, sy, w, h); break;
                    case "mist": DrawMist(sb, l, sx, sy, w); break;
                    case "image": DrawImage(sb, l, sx, sy, w); break;
                    case "moon": DrawMoon(sb, l, w); break;
                }
            }
        }

        private Color Mix(Color mat, Color astral) => Color.Lerp(mat, astral, _astral);

        private void DrawSky(SpriteBatch sb, int w, int h)
        {
            Color top = Mix(_theme.SkyTopMat, _theme.SkyTopAstral), bot = Mix(_theme.SkyBotMat, _theme.SkyBotAstral);
            const int band = 6;
            for (int y = 0; y < h; y += band)
                Gfx.Fill(sb, new Rectangle(0, y, w, band), Color.Lerp(top, bot, (float)y / h));
        }

        private static uint Hash(int seed, int i)
        {
            unchecked
            {
                uint x = (uint)(i * 374761393 + seed * 668265263);
                x = (x ^ (x >> 13)) * 1274126177u;
                return x ^ (x >> 16);
            }
        }
        private static float R(uint h, int shift) => ((h >> shift) & 0xFF) / 255f;

        private static int CellStart(float scroll, int cell, out int first)
        {
            first = (int)MathF.Floor(scroll / cell) - 1;
            return first;
        }

        private void DrawBlocks(SpriteBatch sb, Layer l, float sx, float sy, int w, bool windows)
        {
            Color body = Mix(l.Mat, l.Astral), lit = Mix(l.Lit, l.AstralLit);
            int cell = Math.Max(8, l.Spacing);
            CellStart(sx, cell, out int i0);
            int baseY = l.BaseY - (int)sy;
            for (int i = i0; i * cell - sx < w + cell; i++)
            {
                uint hs = Hash(l.Seed, i);
                int bw = l.MinW + (int)(R(hs, 0) * (l.MaxW - l.MinW));
                int bh = l.MinH + (int)(R(hs, 8) * (l.MaxH - l.MinH));
                int x = (int)(i * cell - sx) + (int)(R(hs, 16) * MathF.Max(0, cell - bw));
                Gfx.Fill(sb, new Rectangle(x, baseY - bh, bw, bh + 200), body);
                if (R(hs, 24) > 0.6f) Gfx.Fill(sb, new Rectangle(x + bw / 2 - 1, baseY - bh - 6, 2, 6), body);   // chimney / spire
                if (!windows || l.Windows <= 0) continue;
                for (int wy = baseY - bh + 4; wy < baseY - 3; wy += 8)
                    for (int wx = x + 3; wx < x + bw - 4; wx += 6)
                    {
                        uint hw = Hash(l.Seed + 7, wx * 31 + wy + i * 997);
                        if (R(hw, 0) < l.Windows) Gfx.Fill(sb, new Rectangle(wx, wy, 2, 3), lit * (0.5f + 0.5f * R(hw, 8)));
                    }
            }
        }

        private void DrawArches(SpriteBatch sb, Layer l, float sx, float sy, int w)
        {
            Color c = Mix(l.Mat, l.Astral);
            int cell = Math.Max(16, l.Spacing), baseY = l.BaseY - (int)sy;
            CellStart(sx, cell, out int i0);
            int top = baseY - l.MaxH;
            for (int i = i0; i * cell - sx < w + cell; i++)
            {
                int x = (int)(i * cell - sx), pillar = Math.Max(3, cell / 6), span = cell - pillar;
                Gfx.Fill(sb, new Rectangle(x, top, pillar, l.MaxH + 200), c);
                // the arch: a half-circle cut out of a solid block above the opening
                int r = span / 2, cx = x + pillar + r, cy = top + r + 4;
                for (int yy = top; yy < cy + r * 2 && yy < baseY; yy++)
                {
                    int half = yy < cy ? (int)MathF.Sqrt(MathF.Max(0, r * r - (cy - yy) * (cy - yy))) : r;
                    int left = cx - half, right = cx + half;
                    if (yy < cy) { Gfx.Fill(sb, new Rectangle(x + pillar, yy, Math.Max(0, left - (x + pillar)), 1), c); Gfx.Fill(sb, new Rectangle(right, yy, Math.Max(0, x + cell - right), 1), c); }
                }
                Gfx.Fill(sb, new Rectangle(x, top - 3, cell, 3), c);
            }
        }

        private void DrawColumns(SpriteBatch sb, Layer l, float sx, float sy, int w, int h)
        {
            Color c = Mix(l.Mat, l.Astral);
            int cell = Math.Max(10, l.Spacing);
            CellStart(sx, cell, out int i0);
            for (int i = i0; i * cell - sx < w + cell; i++)
            {
                uint hs = Hash(l.Seed, i);
                if (R(hs, 0) > l.Density) continue;
                int cw = l.MinW + (int)(R(hs, 8) * (l.MaxW - l.MinW));
                int x = (int)(i * cell - sx);
                int fromTop = (int)(R(hs, 16) * l.MaxH);            // stalactite or full pillar
                Gfx.Fill(sb, new Rectangle(x, 0, cw, Math.Max(8, fromTop + l.MinH) - (int)sy), c);
                Gfx.Fill(sb, new Rectangle(x - 1, Math.Max(8, fromTop + l.MinH) - (int)sy, cw + 2, 2), c);
            }
        }

        private void DrawStars(SpriteBatch sb, Layer l, float sx, float sy, int w, int h)
        {
            Color c = Mix(l.Lit, l.AstralLit);
            int cell = Math.Max(6, l.Spacing);
            CellStart(sx, cell, out int i0);
            int rows = Math.Max(1, l.BaseY / cell);
            for (int i = i0; i * cell - sx < w + cell; i++)
                for (int j = 0; j < rows; j++)
                {
                    uint hs = Hash(l.Seed, i * 53 + j);
                    if (R(hs, 0) > l.Density) continue;
                    float tw = 0.55f + 0.45f * MathF.Sin(_time * (1f + R(hs, 8) * 2f) + R(hs, 16) * 6.28f);
                    int x = (int)(i * cell - sx) + (int)(R(hs, 8) * cell), y = j * cell + (int)(R(hs, 16) * cell) - (int)sy;
                    Gfx.Fill(sb, new Rectangle(x, y, 1, 1), c * tw);
                }
        }

        private void DrawMist(SpriteBatch sb, Layer l, float sx, float sy, int w)
        {
            Color c = Mix(l.Mat, l.Astral);
            int baseY = l.BaseY - (int)sy;
            for (int b = 0; b < 4; b++)
            {
                float wob = MathF.Sin(_time * 0.4f + b * 1.7f) * 3f;
                float a = 0.12f + 0.05f * b;
                Gfx.Fill(sb, new Rectangle(0, baseY - b * 6 + (int)wob, w, 6), c * a);
            }
        }

        // A painted strip (Backdrops/<file>.png) tiled sideways. Its top edge sits at BaseY; with no PNG the layer is skipped.
        private void DrawImage(SpriteBatch sb, Layer l, float sx, float sy, int w)
        {
            var t = Art.Get("Backdrops/" + l.File);
            if (t == null) return;
            var c = Mix(l.Tint, l.AstralTint);
            int x = -(int)MathF.Floor(((sx % t.Width) + t.Width) % t.Width), y = l.BaseY - (int)sy;
            for (; x < w; x += t.Width) sb.Draw(t, new Vector2(x, y), c);
        }

        private void DrawMoon(SpriteBatch sb, Layer l, int w)
        {
            Color c = Mix(l.Lit, l.AstralLit);
            int r = Math.Max(6, l.MaxH / 2), cx = l.BaseY, cy = l.MinH;   // BaseY = x, MinH = y (screen fixed)
            for (int y = -r; y <= r; y++)
            {
                int half = (int)MathF.Sqrt(r * r - y * y);
                Gfx.Fill(sb, new Rectangle(cx - half, cy + y, half * 2, 1), c * 0.9f);
            }
        }

        // ---------- data ----------
        private static void EnsureLoaded()
        {
            if (_themes != null) return;
            _themes = new Dictionary<string, Theme>(StringComparer.Ordinal);
            _default = new Theme();
            string text = DataFiles.ReadText("backgrounds.json");
            if (text == null) return;
            try
            {
                using var doc = JsonDocument.Parse(text, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
                var root = doc.RootElement;
                if (root.TryGetProperty("default", out var d)) _default = ReadTheme(d);
                if (root.TryGetProperty("themes", out var ts))
                    foreach (var p in ts.EnumerateObject()) _themes[p.Name] = ReadTheme(p.Value);
            }
            catch (Exception ex) when (ex is JsonException || ex is InvalidOperationException)
            {
                Console.WriteLine($"[data] backgrounds.json could not be read: {ex.Message}");
            }
        }

        private static Theme ReadTheme(JsonElement e)
        {
            var t = new Theme();
            Col(e, "skyTop", ref t.SkyTopMat); Col(e, "skyBottom", ref t.SkyBotMat);
            if (e.TryGetProperty("skyImage", out var si) && si.ValueKind == JsonValueKind.String) t.SkyImage = si.GetString();
            Col(e, "astralSkyTop", ref t.SkyTopAstral); Col(e, "astralSkyBottom", ref t.SkyBotAstral);
            if (e.TryGetProperty("layers", out var ls))
                foreach (var le in ls.EnumerateArray())
                {
                    var l = new Layer();
                    if (le.TryGetProperty("kind", out var k)) l.Kind = k.GetString();
                    if (le.TryGetProperty("file", out var fl) && fl.ValueKind == JsonValueKind.String) l.File = fl.GetString();
                    Col(le, "tint", ref l.Tint); Col(le, "astralTint", ref l.AstralTint);
                    Num(le, "factor", ref l.Factor); Num(le, "yFactor", ref l.YFactor); Num(le, "drift", ref l.Drift);
                    Num(le, "windows", ref l.Windows); Num(le, "density", ref l.Density);
                    Int(le, "baseY", ref l.BaseY); Int(le, "spacing", ref l.Spacing); Int(le, "minH", ref l.MinH); Int(le, "maxH", ref l.MaxH);
                    Int(le, "minW", ref l.MinW); Int(le, "maxW", ref l.MaxW); Int(le, "seed", ref l.Seed);
                    Col(le, "color", ref l.Mat); Col(le, "astralColor", ref l.Astral);
                    Col(le, "lit", ref l.Lit); Col(le, "astralLit", ref l.AstralLit);
                    t.Layers.Add(l);
                }
            return t;
        }

        private static void Num(JsonElement e, string n, ref float v) { if (e.TryGetProperty(n, out var p) && p.TryGetSingle(out var f)) v = f; }
        private static void Int(JsonElement e, string n, ref int v) { if (e.TryGetProperty(n, out var p) && p.TryGetInt32(out var i)) v = i; }
        private static void Col(JsonElement e, string n, ref Color c)
        {
            if (!e.TryGetProperty(n, out var p) || p.ValueKind != JsonValueKind.Array || p.GetArrayLength() < 3) return;
            c = new Color(p[0].GetInt32(), p[1].GetInt32(), p[2].GetInt32());
        }
    }
}
