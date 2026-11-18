using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ShiftJam
{
    /// <summary>One upgrade, from Data/charms.json: what it is called, how it is earned and what it changes.</summary>
    public sealed record Charm(string Id, string Name, string Desc, string From, IReadOnlyDictionary<string, float> Effects);

    /// <summary>
    /// Charms are permanent upgrades earned during a run (freeing a boss, clearing a level) and kept until the run ends.
    /// Effects are read by the player: maxHealth, maxKi (added) and shiftCostMult, astralDrainMult, kiRegenMult, damageMult (multiplied).
    /// </summary>
    public static class Charms
    {
        private static List<Charm> _all;
        private static readonly HashSet<string> Owned = new();

        public static IReadOnlyList<Charm> All => _all ??= Load();
        public static void Reload() => _all = null;
        public static void Reset() => Owned.Clear();

        public static IEnumerable<Charm> Held()
        {
            foreach (var c in All) if (Owned.Contains(c.Id)) yield return c;
        }

        /// <summary>Hands out every charm earned by this event ("freed:pell", "clear:level:rooftops"). Returns the new ones.</summary>
        public static List<Charm> Grant(string from)
        {
            var got = new List<Charm>();
            foreach (var c in All)
                if (c.From == from && Owned.Add(c.Id)) got.Add(c);
            return got;
        }

        public static float Sum(string key)
        {
            float v = 0;
            foreach (var c in Held()) if (c.Effects.TryGetValue(key, out var x)) v += x;
            return v;
        }

        public static float Mult(string key)
        {
            float v = 1;
            foreach (var c in Held()) if (c.Effects.TryGetValue(key, out var x)) v *= x;
            return v;
        }

        private static List<Charm> Load()
        {
            var list = new List<Charm>();
            string text = DataFiles.ReadText("charms.json");
            if (text == null) return list;
            try
            {
                using var doc = JsonDocument.Parse(text, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
                foreach (var e in doc.RootElement.GetProperty("charms").EnumerateArray())
                {
                    var fx = new Dictionary<string, float>();
                    if (e.TryGetProperty("effects", out var ef))
                        foreach (var p in ef.EnumerateObject()) fx[p.Name] = p.Value.GetSingle();
                    list.Add(new Charm(e.GetProperty("id").GetString(), e.GetProperty("name").GetString(),
                        e.TryGetProperty("desc", out var d) ? d.GetString() : "", e.GetProperty("from").GetString(), fx));
                }
            }
            catch (Exception ex) when (ex is JsonException || ex is InvalidOperationException || ex is KeyNotFoundException)
            {
                Console.WriteLine($"[data] charms.json could not be read: {ex.Message}");
                list.Clear();
            }
            return list;
        }
    }
}
