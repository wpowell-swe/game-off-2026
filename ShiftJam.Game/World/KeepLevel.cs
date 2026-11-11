using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftJam
{
    /// <summary>
    /// What the player has done in the Tithe Keep so far. Lives across the hub and its rooms, because each one is a
    /// separate world. Reset whenever the stage is entered fresh.
    /// Location: 0 is the hub, 1 to 4 the rooms.
    /// </summary>
    public static class KeepState
    {
        public const int RoomCount = 4;
        public static int Location;
        public static int LastDoor;                       // the door to put you beside when you are back in the hub
        public static int Souls;                          // souls freed so far, carried between worlds
        public static readonly bool[] Done = new bool[RoomCount];

        public static int Sigils => Done.Count(d => d);

        public static void Reset()
        {
            Location = 0; LastDoor = 0; Souls = 0;
            Array.Clear(Done);
        }

        public static void Go(int target, int soulsNow)
        {
            Souls = soulsNow;
            Location = target;
            if (target != 0) LastDoor = target;
        }
    }

    /// <summary>
    /// The Tithe Keep: a large hub with four doors, each leading to a small room. Finish a room's task (defeat
    /// everything in it, collect its shards, or both) to earn its sigil. With all four sigils the hub's exit gate opens.
    /// Legend, on top of TestRoom: 1-4 a door to that room (hub), E the way back out (rooms), P start, X the exit, g gate.
    /// </summary>
    public static class KeepLevel
    {
        private const int T = Metrics.TileSize;

        // Hub
        private static readonly Level Hub = new(new[]
        {
            "##########################################################################################",
            "#.....................................................................ww............#....#",
            "#.....................................................................ww............#....#",
            "#.....................................................................ww............#....#",
            "#...............................................................R.....ww............#....#",
            "#...........................................................3.........ww............#....#",
            "#.............................F.........................###########...ww............#....#",
            "#....................................................a................ww............#....#",
            "#.........................................R...........................ww............g....#",
            "#.................................................a...................ww............g....#",
            "#.....................................................................ww............g....#",
            "#..P....k.....1...h.S.k.xxxxxxx.k...2.......S...k...................k.ww..hS..4.....g..X.#",
            "##########################################################################################",
            "##########################################################################################",
        }, "The Tithe Keep: Hub");

        // Barracks
        private static readonly Level Room1 = new(new[]
        {
            "########################",
            "#......................#",
            "#......................#",
            "#......................#",
            "#.............R........#",
            "#......................#",
            "#............####......#",
            "#......................#",
            "#.E.P..k..S....S...S.h.#",
            "########################",
            "########################",
        }, "The Tithe Keep: Barracks");

        // Shard Vault
        private static readonly Level Room2 = new(new[]
        {
            "##############################",
            "#....................ww......#",
            "#....................ww......#",
            "#....................ww......#",
            "#.................o..ww....o.#",
            "#................aa..ww...####",
            "#....................ww......#",
            "#..........o....a....ww..#...#",
            "#.E.P.k.xxxxxxx....k.ww.o....#",
            "##############################",
            "##############################",
        }, "The Tithe Keep: Shard Vault");

        // Hollow Chapel
        private static readonly Level Room3 = new(new[]
        {
            "##########################",
            "#........................#",
            "#........................#",
            "#...........F............#",
            "#........................#",
            "#........................#",
            "#........................#",
            "#........................#",
            "#.E.P.k..H.....H.....H.h.#",
            "##########################",
            "##########################",
        }, "The Tithe Keep: Hollow Chapel");

        // Gauntlet
        private static readonly Level Room4 = new(new[]
        {
            "##############################",
            "#............................#",
            "#............................#",
            "#............................#",
            "#............R........R..o...#",
            "#.....................a.####.#",
            "#............................#",
            "#...................a........#",
            "#.E.P.k.S.....o.....S......h.#",
            "##########aaaaaaaaa###########",
            "##########xxxxxxxxx###########",
            "##############################",
        }, "The Tithe Keep: Gauntlet");

        private static readonly Level[] Rooms = { null, Room1, Room2, Room3, Room4 };
        private static readonly string[] RoomNames = { "", "BARRACKS", "SHARD VAULT", "HOLLOW CHAPEL", "GAUNTLET" };

        public static void Build(GameWorld world)
        {
            // A room already done (you died after finishing it) is not played again.
            if (KeepState.Location != 0 && KeepState.Done[KeepState.Location - 1]) KeepState.Location = 0;
            world.SoulsFreed = KeepState.Souls;
            if (KeepState.Location == 0) BuildHub(world); else BuildRoom(world, KeepState.Location);
        }

        private static Rectangle DoorArea(int tx, int ty) => new(tx * T, (ty + 1) * T - 3 * T, T, 3 * T);

        private static void BuildHub(GameWorld world)
        {
            Campaign.LoadWithExit(world, Hub);
            var doors = new Point[KeepState.RoomCount + 1];
            for (int y = 0; y < Hub.Rows.Length; y++)
                for (int x = 0; x < Hub.Rows[y].Length; x++)
                {
                    char c = Hub.Rows[y][x];
                    if (c < '1' || c > '4') continue;
                    int room = c - '0';
                    doors[room] = new Point(x, y);
                    world.Portals.Add(new GameWorld.Portal
                    {
                        Area = DoorArea(x, y), Target = room,
                        Done = KeepState.Done[room - 1], Label = RoomNames[room],
                    });
                }
            // Back from a room: stand beside the door you used, not on it.
            if (KeepState.LastDoor != 0)
            {
                var d = doors[KeepState.LastDoor];
                world.Player.Position = new Vector2((d.X + 2) * T + (T - Player.Size.X) / 2f, (d.Y + 1) * T - Player.Size.Y);
            }
            world.Add(new HubDirector());
        }

        private static void BuildRoom(GameWorld world, int room)
        {
            var level = Rooms[room];
            TestRoom.Load(world, level);
            GameWorld.Portal back = null;
            for (int y = 0; y < level.Rows.Length; y++)
                for (int x = 0; x < level.Rows[y].Length; x++)
                    if (level.Rows[y][x] == 'E')
                        world.Portals.Add(back = new GameWorld.Portal
                        { Area = DoorArea(x, y), Target = 0, Enabled = false, Label = "LEAVE" });
            world.Add(new RoomObjective(room, back));
        }
    }

    /// <summary>Hub rules: the sigil counter, and the gate that opens once every room is done.</summary>
    public sealed class HubDirector : IEntity
    {
        private bool _open;

        public void Update(float dt, GameWorld world)
        {
            int n = KeepState.Sigils;
            world.StatusText = n >= KeepState.RoomCount ? "THE GATE IS OPEN" : "SIGILS " + n + "/" + KeepState.RoomCount;
            if (_open || n < KeepState.RoomCount || world.Tiles is not TileMap map) return;
            _open = true;
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                    if (map.At(x, y) == 'g')
                    {
                        map.Set(x, y, '.');
                        world.Sparks.Sparks(new Vector2(x * Metrics.TileSize + 8, y * Metrics.TileSize + 8), Plane.Material, 4, 0);
                    }
            Sfx.Play(SfxId.ShiftOut, 0.8f);
            world.Camera.Shake(2f, 0.2f);
        }

        public void Draw(SpriteBatch sb) { }
    }

    /// <summary>
    /// A room's task: every enemy in it down and every shard collected. When done the sigil is earned and the
    /// room's door lets you leave.
    /// </summary>
    public sealed class RoomObjective : IEntity
    {
        private readonly int _room;
        private readonly GameWorld.Portal _exit;
        private System.Collections.Generic.List<IHittable> _foes;
        private int _shardsTotal;
        private bool _done;

        public RoomObjective(int room, GameWorld.Portal exit) { _room = room; _exit = exit; }

        public void Update(float dt, GameWorld world)
        {
            if (_foes == null)
            {
                _foes = world.Entities.OfType<IHittable>()
                    .Where(h => h != world.Player && h is not HollowedSoul).ToList();
                _shardsTotal = world.Entities.OfType<Pickup>().Count(p => p.Kind == PickupKind.Shard);
            }
            if (_done) return;

            int foes = _foes.Count(f => f.IsAlive && f is IEntity e && world.Entities.Contains(e));
            int left = world.Entities.OfType<Pickup>().Count(p => p.Kind == PickupKind.Shard);
            var parts = new System.Collections.Generic.List<string>();
            if (_foes.Count > 0) parts.Add("FOES " + foes);
            if (_shardsTotal > 0) parts.Add("SHARDS " + (_shardsTotal - left) + "/" + _shardsTotal);
            world.StatusText = string.Join("  ", parts);

            if (foes > 0 || left > 0 || world.Player == null || !world.Player.IsAlive) return;
            _done = true;
            KeepState.Done[_room - 1] = true;
            KeepState.Souls = world.SoulsFreed;
            if (_exit != null) _exit.Enabled = true;
            world.StatusText = "SIGIL EARNED";
            Sfx.Play(SfxId.ShiftOut, 0.9f);
            world.Camera.Shake(2f, 0.2f);
        }

        public void Draw(SpriteBatch sb) { }
    }
}
