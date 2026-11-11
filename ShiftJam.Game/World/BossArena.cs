using Microsoft.Xna.Framework;

namespace ShiftJam
{
    /// <summary>
    /// Boss arenas as data. Legend: # solid, w Material-only wall (Astral passes), P player, B boss, . empty.
    /// The Level spawn list does not know 'B', so Build scans the rows itself.
    /// </summary>
    public static class BossArena
    {
        private const int T = Metrics.TileSize;
        private static int TriggerFromTile = Cfg.I("BossArena.TriggerFromTile", 6, v => TriggerFromTile = v);   // the boss wakes once the player passes this column (start area is safe)

        public static Level Pell => LevelFile.Get("pell");

        public static void Build(GameWorld world) => Load(world, Pell);

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
                            // Same placement as TestRoom.Load: feet on the marker tile's bottom, centred horizontally.
                            world.Player = new Player(new Vector2(tx + (T - Player.Size.X) / 2f, ty + T - Player.Size.Y));
                            world.Add(world.Player);
                            break;
                        case 'B':
                        {
                            var boss = new ShiftJam.Pell(new Vector2(tx + (T - ShiftJam.Pell.BodySize.X) / 2f, ty + T - ShiftJam.Pell.BodySize.Y));
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
