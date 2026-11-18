using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace ShiftJam
{
    /// <summary>
    /// Looping background music. Which track plays for which stage is in Data/music.json; the tracks themselves are
    /// the WAVs in ShiftJamContent/Music (made by tools/make_music.py, replaceable by any looping WAV of the same name).
    /// Tracks cross-fade when the stage changes, and the music ducks while the game is paused or someone is talking.
    /// Missing tracks or no audio device just mean silence.
    /// </summary>
    public static class Music
    {
        private sealed class Playing
        {
            public SoundEffectInstance Inst; public string Name; public float Level;
        }

        private static readonly Dictionary<string, SoundEffect> Tracks = new();
        private static Dictionary<string, string> _stages = new();
        private static string _title = "title", _default = "street";
        private static Playing _current;
        private static readonly List<Playing> Fading = new();
        private static ContentManager _content;
        private static float _duck = 1f;

        public static void Load(ContentManager content)
        {
            _content = content;
            ReadMapping();
        }

        /// <summary>Re-read music.json (the F11 reload).</summary>
        public static void Reload() { ReadMapping(); }

        private static void ReadMapping()
        {
            _stages = new Dictionary<string, string>(StringComparer.Ordinal);
            string text = DataFiles.ReadText("music.json");
            if (text == null) return;
            try
            {
                using var doc = JsonDocument.Parse(text, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
                var r = doc.RootElement;
                if (r.TryGetProperty("title", out var t)) _title = t.GetString();
                if (r.TryGetProperty("default", out var d)) _default = d.GetString();
                if (r.TryGetProperty("stages", out var s))
                    foreach (var p in s.EnumerateObject()) _stages[p.Name] = p.Value.GetString();
            }
            catch (Exception ex) when (ex is JsonException || ex is InvalidOperationException)
            {
                Console.WriteLine($"[data] music.json could not be read: {ex.Message}");
            }
        }

        private static SoundEffect Get(string name)
        {
            if (string.IsNullOrEmpty(name) || _content == null) return null;
            if (Tracks.TryGetValue(name, out var fx)) return fx;
            try { fx = _content.Load<SoundEffect>("Music/" + name); }
            catch (Exception e) { fx = null; Console.WriteLine($"[music] could not load {name}: {e.GetType().Name}"); }
            return Tracks[name] = fx;
        }

        public static void PlayTitle() => Play(_title);

        public static void PlayStage(string stageId) =>
            Play(stageId != null && _stages.TryGetValue(stageId, out var n) ? n : _default);

        public static void Play(string name)
        {
            if (_current != null && _current.Name == name) return;
            if (_current != null) Fading.Add(_current);
            _current = null;
            var fx = Get(name);
            if (fx == null) return;
            try
            {
                var inst = fx.CreateInstance();
                inst.IsLooped = true;
                inst.Volume = 0f;
                inst.Play();
                _current = new Playing { Inst = inst, Name = name, Level = 0f };
            }
            catch (Exception e) { Console.WriteLine($"[music] playback failed: {e.GetType().Name}: {e.Message}"); }
        }

        public static void Stop() { if (_current != null) { Fading.Add(_current); _current = null; } }

        /// <summary>Call every frame. quiet = paused or in dialogue; astral dims the music a little.</summary>
        public static void Update(float dt, bool quiet, bool astral)
        {
            float duckTarget = (quiet ? 0.45f : 1f) * (astral ? 0.75f : 1f);
            _duck += (duckTarget - _duck) * Math.Min(1f, dt * 6f);
            try
            {
                if (_current != null)
                {
                    _current.Level = Math.Min(1f, _current.Level + dt / 0.8f);
                    _current.Inst.Volume = Math.Clamp(_current.Level * _duck * Tuning.MusicVolume, 0f, 1f);
                }
                for (int i = Fading.Count - 1; i >= 0; i--)
                {
                    var f = Fading[i];
                    f.Level -= dt / 0.6f;
                    if (f.Level <= 0f) { f.Inst.Stop(); f.Inst.Dispose(); Fading.RemoveAt(i); }
                    else f.Inst.Volume = Math.Clamp(f.Level * Tuning.MusicVolume, 0f, 1f);
                }
            }
            catch (Exception) { /* audio device went away: stay silent */ }
        }
    }
}
