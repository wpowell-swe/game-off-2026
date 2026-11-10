using System;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace ShiftJam
{
    public enum SfxId { Jump, LightHit, HeavyHit, Swing, ShiftIn, ShiftOut, Hurt, EnemyDefeat, Block }

    /// <summary>Placeholder sound effects. Missing content or a missing audio device just means silence.</summary>
    public static class Sfx
    {
        private static readonly string[] Names =
            { "jump", "light_hit", "heavy_hit", "swing", "shift_in", "shift_out", "hurt", "enemy_defeat", "block" };
        private static readonly SoundEffect[] _fx = new SoundEffect[Names.Length];
        private static readonly Random _rng = new Random(7);
        private static bool _warned;

        public static void Load(ContentManager content)
        {
            for (int i = 0; i < Names.Length; i++)
            {
                try { _fx[i] = content.Load<SoundEffect>("Sfx/" + Names[i]); }
                catch (Exception e) { _fx[i] = null; Console.WriteLine($"[sfx] could not load {Names[i]}: {e.GetType().Name}"); }
            }
            int loaded = Array.FindAll(_fx, f => f != null).Length;
            Console.WriteLine($"[sfx] loaded {loaded} of {Names.Length} sound effects.");
        }

        /// <summary>pitchJitter: random pitch offset in +/- this range (0 to 1 is an octave).</summary>
        public static void Play(SfxId id, float volume = 1f, float pitchJitter = 0f)
        {
            var fx = _fx[(int)id];
            if (fx == null) return;
            try
            {
                float pitch = pitchJitter > 0 ? (float)(_rng.NextDouble() * 2 - 1) * pitchJitter : 0f;
                fx.Play(Math.Clamp(volume * Tuning.SfxMasterVolume, 0f, 1f), Math.Clamp(pitch, -1f, 1f), 0f);
            }
            catch (Exception e) when (!_warned) { _warned = true; Console.WriteLine($"[sfx] playback failed: {e.GetType().Name}: {e.Message}"); }
            catch (Exception) { /* already reported */ }
        }
    }
}
