using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ShiftJam
{
    /// <summary>
    /// Tunable numbers that live in Data/balance.json instead of the code. Every tunable is declared in code with its
    /// default, for example
    ///     private static float SwingWindup = Cfg.F("Pell.SwingWindup", 0.6f, v => SwingWindup = v);
    /// and balance.json only needs the values you want to change:  { "Pell": { "SwingWindup": 0.8 } }.
    /// Load() re-reads the file and updates every tunable that has already been declared (the F11 reload).
    /// </summary>
    public static class Cfg
    {
        private sealed record Entry(double Default, Action<double> Apply);
        private sealed record ListEntry(string[] Default, Action<string[]> Apply);

        private static readonly Dictionary<string, Entry> Entries = new();
        private static readonly Dictionary<string, ListEntry> Lists = new();
        private static Dictionary<string, double> _values = new();
        private static Dictionary<string, string[]> _lists = new();
        private static bool _loaded;

        private static void EnsureLoaded() { if (!_loaded) Load(); }

        /// <summary>Reads balance.json (if there is one) and applies it to everything declared so far.</summary>
        public static void Load()
        {
            _loaded = true;
            var values = new Dictionary<string, double>();
            var lists = new Dictionary<string, string[]>();
            string text = DataFiles.ReadText("balance.json");
            if (text != null)
            {
                try
                {
                    using var doc = JsonDocument.Parse(text, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
                    foreach (var group in doc.RootElement.EnumerateObject())
                    {
                        if (group.Value.ValueKind != JsonValueKind.Object) continue;
                        foreach (var item in group.Value.EnumerateObject())
                        {
                            string key = group.Name + "." + item.Name;
                            switch (item.Value.ValueKind)
                            {
                                case JsonValueKind.Number: values[key] = item.Value.GetDouble(); break;
                                case JsonValueKind.True: values[key] = 1; break;
                                case JsonValueKind.False: values[key] = 0; break;
                                case JsonValueKind.Array:
                                    var names = new List<string>();
                                    foreach (var e in item.Value.EnumerateArray()) names.Add(e.GetString());
                                    lists[key] = names.ToArray();
                                    break;
                            }
                        }
                    }
                }
                catch (JsonException e)
                {
                    Console.WriteLine($"[data] balance.json is not valid JSON, using defaults: {e.Message}");
                    values.Clear(); lists.Clear();
                }
            }
            _values = values; _lists = lists;
            foreach (var (key, entry) in Entries) entry.Apply(values.TryGetValue(key, out var v) ? v : entry.Default);
            foreach (var (key, entry) in Lists) entry.Apply(lists.TryGetValue(key, out var l) ? l : entry.Default);
            Console.WriteLine($"[data] balance: {values.Count + lists.Count} values from {(text == null ? "no file (defaults)" : "balance.json")}.");
        }

        public static float F(string key, float def, Action<float> apply)
        {
            EnsureLoaded();
            Entries[key] = new Entry(def, d => apply((float)d));
            return _values.TryGetValue(key, out var v) ? (float)v : def;
        }

        public static int I(string key, int def, Action<int> apply)
        {
            EnsureLoaded();
            Entries[key] = new Entry(def, d => apply((int)Math.Round(d)));
            return _values.TryGetValue(key, out var v) ? (int)Math.Round(v) : def;
        }

        public static bool B(string key, bool def, Action<bool> apply)
        {
            EnsureLoaded();
            Entries[key] = new Entry(def ? 1 : 0, d => apply(d != 0));
            return _values.TryGetValue(key, out var v) ? v != 0 : def;
        }

        /// <summary>A plain list of strings (for sequences whose items are more than one word).</summary>
        public static string[] Strings(string key, string[] def, Action<string[]> apply)
        {
            EnsureLoaded();
            Lists[key] = new ListEntry(def, names => apply(names));
            return _lists.TryGetValue(key, out var l) && l.Length > 0 ? l : def;
        }

        /// <summary>An attack sequence (or any list of enum names). Unknown names are skipped with a console message.</summary>
        public static T[] Seq<T>(string key, T[] def, Action<T[]> apply) where T : struct, Enum
        {
            EnsureLoaded();
            string[] defNames = Array.ConvertAll(def, x => x.ToString());
            T[] Parse(string[] names)
            {
                var list = new List<T>();
                foreach (var n in names)
                    if (Enum.TryParse<T>(n, out var t)) list.Add(t);
                    else Console.WriteLine($"[data] {key}: unknown name '{n}' (expected one of {string.Join(", ", Enum.GetNames<T>())}).");
                return list.Count > 0 ? list.ToArray() : def;
            }
            Lists[key] = new ListEntry(defNames, names => apply(Parse(names)));
            return _lists.TryGetValue(key, out var l) ? Parse(l) : def;
        }
    }

    /// <summary>On-screen words that live in Data/text.json. Looked up when drawn, so a reload shows edits at once.</summary>
    public static class Txt
    {
        private static Dictionary<string, string> _text = new();
        private static bool _loaded;

        public static void Load()
        {
            _loaded = true;
            var map = new Dictionary<string, string>();
            string text = DataFiles.ReadText("text.json");
            if (text != null)
            {
                try
                {
                    using var doc = JsonDocument.Parse(text, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
                    foreach (var p in doc.RootElement.EnumerateObject())
                        if (p.Value.ValueKind == JsonValueKind.String) map[p.Name] = p.Value.GetString();
                }
                catch (JsonException e) { Console.WriteLine($"[data] text.json is not valid JSON, using the built-in words: {e.Message}"); }
            }
            _text = map;
        }

        /// <summary>The text for a key, or the built-in default when text.json does not set it.</summary>
        public static string S(string key, string def)
        {
            if (!_loaded) Load();
            return _text.TryGetValue(key, out var s) ? s : def;
        }
    }
}
