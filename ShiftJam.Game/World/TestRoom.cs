using Microsoft.Xna.Framework;

namespace ShiftJam
{
    /// <summary>
    /// Prototype test levels as data. Legend: # solid, w Material-only wall (Astral passes),
    /// P player, D dummy, S Shell, R Wraith, H Hollowed, F Flicker, k Ki pickup, h health pickup, . empty.
    /// Reach limits: jump about 3 tiles up (keep steps to 2), about 4 tiles across at walk speed.
    /// </summary>
    public static class TestRoom
    {
        public static readonly Level Horizontal = new(new[]
        {
            "################################################################################",
            "#.................................................w............................#",
            "#.................................................w............................#",
            "#.................................................w............................#",
            "#.................................................w............................#",
            "#.................................................w............................#",
            "#.................................................w............................#",
            "#.................................................w............................#",
            "#.....................######......................w............######..........#",
            "#...............####..............................w.......####.................#",
            "#.P.......D.########..........S.........S.........w...########....R...###...S..#",
            "#################################...############################################",
        }, "Lantern Streets test");

        public static readonly Level Descent = new(new[]
        {
            "####################",
            "#..................#",
            "#..................#",
            "#..................#",
            "#..P...............#",
            "#############......#",
            "#..................#",
            "#..................#",
            "#..................#",
            "#........H.........#",
            "#......#######wwww##",
            "#..................#",
            "#..................#",
            "#..................#",
            "#...F..............#",
            "#############......#",
            "#..................#",
            "#..................#",
            "#..................#",
            "#..................#",
            "#......#############",
            "######.............#",
            "#....w.............#",
            "#....w.............#",
            "#.D..w.............#",
            "#############......#",
            "#..................#",
            "#..................#",
            "#.........D........#",
            "####################",
        }, "Cellars test");

        /// <summary>Wide horizontal level (about 80 tiles) with a pit and a Material-only wall.</summary>
        public static void Build(GameWorld world) => Load(world, Horizontal);

        /// <summary>Descending shaft (30 tiles tall) with ledges and an Astral-only route.</summary>
        public static void BuildDescent(GameWorld world) => Load(world, Descent);

        public static void Load(GameWorld world, Level level)
        {
            const int T = Metrics.TileSize;
            world.Tiles = new TileMap(level);
            foreach (var (kind, tile) in level.Spawns)
            {
                switch (kind)
                {
                    case 'P':
                        // Feet on the bottom of the marker tile, centred horizontally, so the box starts clear of the floor.
                        world.Player = new Player(new Vector2(tile.X + (T - Player.Size.X) / 2f, tile.Y + T - Player.Size.Y));
                        world.Add(world.Player);
                        break;
                    case 'D':
                        world.Add(new Dummy(new Vector2(tile.X + (T - Dummy.Size.X) / 2f, tile.Y + T - Dummy.Size.Y)));
                        break;
                    case 'S':
                        world.Add(new Shell(new Vector2(tile.X + (T - 14) / 2f, tile.Y + T - 28)));
                        break;
                    case 'H':
                        world.Add(new Hollowed(new Vector2(tile.X + (T - 14) / 2f, tile.Y + T - 28)));
                        break;
                    case 'F':
                        world.Add(new Flicker(new Vector2(tile.X, tile.Y)));
                        break;
                    case 'k':
                        world.Add(new Pickup(PickupKind.Ki, tile));
                        break;
                    case 'h':
                        world.Add(new Pickup(PickupKind.Health, tile));
                        break;
                    case 'R':
                        world.Add(new Wraith(new Vector2(tile.X, tile.Y)));
                        break;
                }
            }
            world.Camera.Reset();
        }
    }
}
