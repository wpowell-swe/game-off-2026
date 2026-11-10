using System;
using Microsoft.Xna.Framework;

namespace ShiftJam
{
    public enum StageKind { Level, Boss }

    /// <summary>One step of the campaign: a name for the intro banner, how it is won, and how to build it.</summary>
    public sealed record Stage(string Name, StageKind Kind, Action<GameWorld> Build);

    /// <summary>
    /// The order the game is played in: Streets, Pell, Cellars, Dova, then the end.
    /// Normal levels are cleared by reaching their exit door; boss arenas by freeing the boss.
    /// </summary>
    public sealed class Campaign
    {
        public static readonly Stage[] Stages =
        {
            new("The Lantern Streets", StageKind.Level, w => LoadWithExit(w, StageLevels.Streets)),
            new("Pell, the Lantern-Keeper", StageKind.Boss, BossArena.Build),
            new("The Cellars", StageKind.Level, w => LoadWithExit(w, StageLevels.Cellars)),
            new("Dova, the Stall Owner", StageKind.Boss, DovaArena.Build),
        };

        public int Index { get; private set; }
        public int TotalSouls { get; private set; }

        public Stage Current => Stages[Index];
        public bool IsLast => Index >= Stages.Length - 1;

        /// <summary>Jump straight to a stage (debug keys) without banking souls.</summary>
        public void GoTo(int index) => Index = Math.Clamp(index, 0, Stages.Length - 1);

        /// <summary>Bank the souls freed in the stage just cleared and move on. Returns false after the last stage.</summary>
        public bool Advance(int soulsThisStage)
        {
            TotalSouls += soulsThisStage;
            if (IsLast) return false;
            Index++;
            return true;
        }

        public void Restart()
        {
            Index = 0;
            TotalSouls = 0;
        }

        /// <summary>Loads a level like TestRoom does, then turns its 'X' tile into the exit door.</summary>
        public static void LoadWithExit(GameWorld world, Level level)
        {
            TestRoom.Load(world, level);
            const int T = Metrics.TileSize;
            for (int y = 0; y < level.Rows.Length; y++)
                for (int x = 0; x < level.Rows[y].Length; x++)
                    if (level.Rows[y][x] == 'X')
                        world.ExitArea = new Rectangle(x * T, y * T, T, T);
        }
    }
}
