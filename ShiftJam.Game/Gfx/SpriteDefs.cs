using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ShiftJam
{
    /// <summary>
    /// One animation strip: a PNG with FrameCount equal frames laid out left to right.
    /// Origin is the point INSIDE a frame (pixels from its top-left) that sits on the bottom centre
    /// of the entity's hitbox. For a character facing right with feet at the bottom edge, that is
    /// (FrameWidth / 2, FrameHeight).
    /// </summary>
    public sealed record SpriteDef(string Asset, int FrameWidth, int FrameHeight, int FrameCount,
        float Fps, bool Loop, int OriginX, int OriginY);

    /// <summary>
    /// The table of every animation the game knows about, read from Data/sprites.json.
    /// File = ShiftJamContent/Sprites/&lt;character&gt;/&lt;animation&gt;.png (the table key is character_animation). To change a frame size or count, edit that
    /// JSON (and press F11 in the game); no rebuild is needed for the numbers, only for new or changed PNGs.
    /// Art must be drawn FACING RIGHT; the game flips it for facing left.
    /// </summary>
    public static class SpriteDefs
    {
        public const string Joseph = "joseph";
        public const string Shell = "shell";
        public const string Wraith = "wraith";
        public const string Dummy = "dummy";
        public const string Hollowed = "hollowed";
        public const string Flicker = "flicker";
        public const string Soul = "soul";
        public const string Pell = "pell";
        public const string Dova = "dova";
        public const string Clerk = "clerk";

        private static Dictionary<string, SpriteDef> _table;

        /// <summary>All defs, keyed by "character_animation" (for example "joseph_idle").</summary>
        public static IReadOnlyDictionary<string, SpriteDef> All => _table ??= Read();

        /// <summary>Forget the table so it is read again (the F11 reload).</summary>
        public static void Reload() => _table = null;

        public static bool TryGet(string character, string animation, out SpriteDef def) =>
            All.TryGetValue(character + "_" + animation, out def);

        private static Dictionary<string, SpriteDef> Read()
        {
            var t = new Dictionary<string, SpriteDef>();
            string text = DataFiles.ReadText("sprites.json");
            if (text == null) { Console.WriteLine("[data] sprites.json is missing: no sprites will load."); return t; }
            try
            {
                using var doc = JsonDocument.Parse(text, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
                foreach (var e in doc.RootElement.GetProperty("sprites").EnumerateArray())
                {
                    string name = e.GetProperty("name").GetString();
                    int w = e.GetProperty("w").GetInt32(), h = e.GetProperty("h").GetInt32();
                    int frames = e.GetProperty("frames").GetInt32();
                    float fps = e.TryGetProperty("fps", out var f) ? (float)f.GetDouble() : 1f;
                    bool loop = e.TryGetProperty("loop", out var l) && l.GetBoolean();
                    int originY = e.TryGetProperty("originY", out var o) ? o.GetInt32() : h;
                    // "joseph_walk" is the file Sprites/joseph/walk.png: one folder per character.
                    int us = name.IndexOf('_');
                    string asset = us > 0 ? "Sprites/" + name[..us] + "/" + name[(us + 1)..] : "Sprites/" + name;
                    t[name] = new SpriteDef(asset, w, h, frames, fps, loop, w / 2, originY);
                }
            }
            catch (Exception ex) when (ex is JsonException || ex is InvalidOperationException || ex is KeyNotFoundException)
            {
                Console.WriteLine($"[data] sprites.json could not be read: {ex.Message}");
                t.Clear();
            }
            return t;
        }
    }
}
