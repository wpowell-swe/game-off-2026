using System;
using Microsoft.Xna.Framework;

namespace ShiftJam
{
    /// <summary>
    /// The Catacombs: four rooms side by side for the Clerk's four health bars. Gates ('g') in the doorways stay shut
    /// until the Clerk runs through them. Dying restarts at the room of the bar you reached (Checkpoint).
    /// </summary>
    public static class ClerkArena
    {
        private const int T = Metrics.TileSize;
        public static int RoomWidth => Catacombs.Meta.TryGetValue("roomwidth", out var w) ? int.Parse(w) : 30;
        public static int RoomCount => Catacombs.Meta.TryGetValue("rooms", out var n) ? int.Parse(n) : 4;

        /// <summary>The bar (room) to restart in. Reset whenever the stage is entered fresh.</summary>
        public static int Checkpoint;

        public static Level Catacombs => LevelFile.Get("clerk_catacombs");

        public static int RoomStart(int room) => room * RoomWidth;   // the rooms are equal width, set by @roomwidth in the file

        /// <summary>Opens the gate into a room (the doorway column), once.</summary>
        public static void OpenGate(GameWorld world, int room)
        {
            if (world.Tiles is not TileMap map || room <= 0) return;
            int x = RoomStart(room);
            bool any = false;
            for (int y = 0; y < map.Height; y++)
                if (map.At(x, y) == 'g')
                {
                    map.Set(x, y, '.'); any = true;
                    world.Sparks.Sparks(new Vector2(x * T + 8, y * T + 8), Plane.Material, 3, 0);
                }
            if (any) { Sfx.Play(SfxId.HeavyHit, 0.6f); world.Camera.Shake(2f, 0.2f); }
        }

        public static void Build(GameWorld world)
        {
            TestRoom.Load(world, Catacombs);
            int room = Math.Clamp(Checkpoint, 0, RoomCount - 1);
            for (int i = 1; i <= room; i++) OpenGate(world, i);
            int start = RoomStart(room);
            if (room > 0)
                world.Player.Position = new Vector2((start + 3) * T + (T - Player.Size.X) / 2f, 9 * T - Player.Size.Y);

            // The Clerk waits on the right of its room; Astral bars start it hovering.
            var pos = new Vector2((start + 24) * T, 9 * T - Clerk.BodySize.Y);
            var clerk = new Clerk(pos, room);
            clerk.TriggerArea = new Rectangle((start + 5) * T, 0, (RoomWidth - 5) * T, Catacombs.Rows.Length * T);
            world.Add(clerk);
        }
    }
}
