using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ShiftJam
{
    /// <summary>One spoken line: who says it (empty for narration) and what.</summary>
    public readonly record struct StoryLine(string Speaker, string Text);

    /// <summary>
    /// The story script, read from Data/story.json. A trigger name (for example "freed:pell") maps to a list of lines.
    /// A line is either { "speaker": "Joseph", "text": "..." } or { "ref": "joseph_pell_lantern" }, which pulls a line
    /// written by the content pipeline from Generated/dialogue.json. Triggers the game fires:
    ///   intro, start:&lt;stage&gt;, engage:&lt;boss&gt;, freed:&lt;boss&gt;, ending
    /// where &lt;stage&gt; is the stage's build name from campaign.json (level:streets, boss:pell, keep, ...) and
    /// &lt;boss&gt; is pell, dova or clerk. Each trigger plays once per run.
    /// </summary>
    public static class Story
    {
        private static Dictionary<string, List<StoryLine>> _triggers;
        private static Dictionary<string, StoryLine> _generated;

        public static void Reload() { _triggers = null; _generated = null; }

        public static IReadOnlyList<StoryLine> Get(string trigger)
        {
            _triggers ??= Read();
            return _triggers.TryGetValue(trigger, out var lines) ? lines : Array.Empty<StoryLine>();
        }

        private static StoryLine? Generated(string id)
        {
            if (_generated == null)
            {
                _generated = new Dictionary<string, StoryLine>();
                foreach (var d in GeneratedContent.LoadDialogue()) _generated[d.Id] = new StoryLine(d.Speaker, d.Text);
            }
            if (_generated.TryGetValue(id, out var line)) return line;
            Console.WriteLine($"[data] story.json: no generated dialogue line with id '{id}'.");
            return null;
        }

        private static Dictionary<string, List<StoryLine>> Read()
        {
            var map = new Dictionary<string, List<StoryLine>>();
            string text = DataFiles.ReadText("story.json");
            if (text == null) return map;
            try
            {
                using var doc = JsonDocument.Parse(text, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
                foreach (var trigger in doc.RootElement.GetProperty("triggers").EnumerateObject())
                {
                    var lines = new List<StoryLine>();
                    foreach (var e in trigger.Value.EnumerateArray())
                    {
                        if (e.TryGetProperty("ref", out var r))
                        {
                            var g = Generated(r.GetString());
                            if (g.HasValue) lines.Add(g.Value);
                        }
                        else if (e.TryGetProperty("text", out var t))
                            lines.Add(new StoryLine(e.TryGetProperty("speaker", out var s) ? s.GetString() ?? "" : "", t.GetString() ?? ""));
                    }
                    map[trigger.Name] = lines;
                }
            }
            catch (Exception ex) when (ex is JsonException || ex is InvalidOperationException || ex is KeyNotFoundException)
            {
                Console.WriteLine($"[data] story.json could not be read: {ex.Message}");
                map.Clear();
            }
            return map;
        }
    }
}
