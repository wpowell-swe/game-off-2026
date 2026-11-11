using System.Collections.Generic;

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
    /// The table of every animation the game knows about. File name = ShiftJamContent/Sprites/&lt;character&gt;_&lt;animation&gt;.png.
    /// To change a frame size or count, edit the line here (and tell the coder if you are not them).
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

        private static readonly Dictionary<string, SpriteDef> Table = Build();

        /// <summary>All defs, keyed by "character_animation" (for example "joseph_idle").</summary>
        public static IReadOnlyDictionary<string, SpriteDef> All => Table;

        public static bool TryGet(string character, string animation, out SpriteDef def) =>
            Table.TryGetValue(character + "_" + animation, out def);

        private static Dictionary<string, SpriteDef> Build()
        {
            var t = new Dictionary<string, SpriteDef>();

            // Character feet at the bottom centre of the frame.
            void Feet(string who, string anim, int w, int h, int frames, float fps, bool loop, int originY = -1) =>
                t[who + "_" + anim] = new SpriteDef("Sprites/" + who + "_" + anim, w, h, frames, fps, loop, w / 2, originY < 0 ? h : originY);

            // Joseph: 32x48 frames, hitbox 12x28.
            Feet(Joseph, "idle", 32, 48, 2, 3, true);
            Feet(Joseph, "walk", 32, 48, 4, 8, true);
            Feet(Joseph, "jump", 32, 48, 1, 1, true);
            Feet(Joseph, "light", 32, 48, 3, 1, false);   // paced by the attack timing, not by fps
            Feet(Joseph, "heavy", 32, 48, 4, 1, false);   // paced by the attack timing, not by fps
            Feet(Joseph, "block", 32, 48, 1, 1, true);
            Feet(Joseph, "hurt", 32, 48, 1, 1, false);
            Feet(Joseph, "shift", 32, 48, 2, 10, false);
            Feet(Joseph, "dead", 32, 48, 1, 1, false);

            // Shell: 32x48 frames, hitbox 14x28.
            Feet(Shell, "walk", 32, 48, 4, 5, true);
            Feet(Shell, "windup", 32, 48, 2, 1, false);   // paced by the windup timer
            Feet(Shell, "attack", 32, 48, 2, 1, false);   // paced by the recovery timer
            Feet(Shell, "hurt", 32, 48, 1, 1, false);

            // Wraith: 24x32 frames, hitbox 12x16. Hitbox bottom centre is at (12, 24); the wisp tail hangs below.
            Feet(Wraith, "float", 24, 32, 2, 4, true, 24);
            Feet(Wraith, "lunge", 24, 32, 2, 8, true, 24);

            // Hollowed: 32x48 frames, hitbox 16x30. broken = the guard-broken slump, freed = sitting, relieved.
            Feet(Hollowed, "idle", 32, 48, 2, 3, true);
            Feet(Hollowed, "walk", 32, 48, 4, 5, true);
            Feet(Hollowed, "windup", 32, 48, 2, 1, false);   // paced by the windup timer
            Feet(Hollowed, "attack", 32, 48, 2, 1, false);   // paced by the recovery timer
            Feet(Hollowed, "hurt", 32, 48, 1, 1, false);
            Feet(Hollowed, "broken", 32, 48, 2, 6, true);
            Feet(Hollowed, "freed", 32, 48, 1, 1, false);

            // The Hollowed's soul (Astral only, shown while the body's guard is broken): 24x32, bottom centre is the wisp tail.
            Feet(Soul, "float", 24, 32, 2, 4, true);

            // Flicker: 32x48 frames, hitbox 14x28. It swaps plane every couple of seconds; the game tints the Astral half.
            Feet(Flicker, "idle", 32, 48, 2, 3, true);
            Feet(Flicker, "walk", 32, 48, 4, 6, true);
            Feet(Flicker, "windup", 32, 48, 2, 6, false);
            Feet(Flicker, "attack", 32, 48, 2, 8, false);
            Feet(Flicker, "hurt", 32, 48, 1, 1, false);

            // Dummy: 24x32 frames, hitbox 14x28.
            Feet(Dummy, "idle", 24, 32, 1, 1, true);
            Feet(Dummy, "hurt", 24, 32, 1, 1, false);

            // Bosses share one animation set. Frames are larger than the hitbox so weapons and hats fit.
            // Pell: hitbox 18x36. Dova: hitbox 20x34. Clerk: hitbox 18x30. Feet at the bottom centre.
            void Boss(string who, int w, int h)
            {
                Feet(who, "idle", w, h, 2, 3, true);
                Feet(who, "walk", w, h, 4, 7, true);
                Feet(who, "windup", w, h, 2, 6, false);   // the telegraph: raising a weapon, gathering up
                Feet(who, "attack", w, h, 2, 8, false);   // the strike and its follow-through
                Feet(who, "hurt", w, h, 1, 1, false);     // flinch between phases
                Feet(who, "freed", w, h, 1, 1, false);    // relieved, as the boss is freed
            }
            Boss(Pell, 48, 56);
            Boss(Dova, 48, 56);
            Boss(Clerk, 40, 56);

            return t;
        }
    }
}
