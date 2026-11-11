using System;
using System.Collections.Generic;

namespace ShiftJam
{
    /// <summary>
    /// Loads a level from Data/levels/&lt;id&gt;.txt. Format: a line starting with ';' is a comment, a line starting with
    /// '@' is a setting ("@name The Lantern Streets"), and every other non-empty line is one row of the map, top to bottom.
    /// The tile legend is at the top of TileMap.cs and TestRoom.cs. Loaded fresh each time, so edits show after a reload (F11).
    /// </summary>
    public static class LevelFile
    {
        private static readonly Dictionary<string, Level> Cache = new();

        /// <summary>Forget everything read so far, so the next Get reads the files again (the F11 reload).</summary>
        public static void Reload() => Cache.Clear();

        public static Level Get(string id)
        {
            if (Cache.TryGetValue(id, out var cached)) return cached;
            return Cache[id] = Read(id);
        }

        private static Level Read(string id)
        {
            string text = DataFiles.ReadText("levels/" + id + ".txt");
            if (text == null)
            {
                Console.WriteLine($"[data] missing level file levels/{id}.txt; using a tiny empty room.");
                return new Level(new[] { "####", "#P.#", "####" }, id);
            }
            var rows = new List<string>();
            var meta = new Dictionary<string, string>();
            foreach (string raw in text.Replace("\r\n", "\n").Split('\n'))
            {
                string line = raw.TrimEnd();
                if (line.Length == 0 || line[0] == ';') continue;
                if (line[0] == '@')
                {
                    int sp = line.IndexOf(' ');
                    string key = (sp < 0 ? line[1..] : line[1..sp]).ToLowerInvariant();
                    meta[key] = sp < 0 ? "" : line[(sp + 1)..].Trim();
                    continue;
                }
                rows.Add(line);
            }
            meta.TryGetValue("name", out var name);
            return new Level(rows.ToArray(), name ?? id) { Meta = meta };
        }
    }
}
