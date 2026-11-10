using Microsoft.Xna.Framework;

namespace ShiftJam
{
    /// <summary>
    /// The Cellars boss room. Legend: # solid, w Material-only wall (unused here), P player, B boss, . empty.
    /// The Level spawn list does not know 'B', so Build scans the rows itself.
    /// </summary>
    public static class DovaArena
    {
        private const int T = Metrics.TileSize;
        private const int TriggerFromTile = 9;   // the boss wakes once the player passes this column (start area is safe)

        /// <summary>
        /// Cellars boss room, 28x12. A completely flat, clear floor from start to boss. Two crate stacks (2 tiles high, so
        /// a normal jump reaches them) stand against the left and right walls as ledges; nothing blocks the walk between them.
        /// </summary>
        public static readonly Level Dova = new(new[]
        {
            "############################",
            "#..........................#",
            "#..........................#",
            "#..........................#",
            "#..........................#",
            "#..........................#",
            "#..........................#",
            "#..........................#",
            "#..........................#",
            "###......................###",
            "###..P................B..###",
            "############################",
        }, "Cellars: Dova");

        public static void Build(GameWorld world) => Load(world, Dova);

        public static void Load(GameWorld world, Level level)
        {
            world.Tiles = new TileMap(level);
            var rows = level.Rows;
            for (int y = 0; y < rows.Length; y++)
                for (int x = 0; x < rows[y].Length; x++)
                {
                    float tx = x * T, ty = y * T;
                    switch (rows[y][x])
                    {
                        case 'P':
                            world.Player = new Player(new Vector2(tx + (T - Player.Size.X) / 2f, ty + T - Player.Size.Y));
                            world.Add(world.Player);
                            break;
                        case 'B':
                        {
                            var boss = new ShiftJam.Dova(new Vector2(tx + (T - ShiftJam.Dova.BodySize.X) / 2f, ty + T - ShiftJam.Dova.BodySize.Y));
                            int w = new TileMap(level).Width, h = rows.Length;
                            boss.TriggerArea = new Rectangle(TriggerFromTile * T, 0, (w - TriggerFromTile) * T, h * T);
                            world.Add(boss);
                            break;
                        }
                    }
                }
            world.Camera.Reset();
        }
    }
}
