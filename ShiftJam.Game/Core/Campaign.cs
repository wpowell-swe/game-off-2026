using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace ShiftJam
{
    public enum StageKind { Level, Boss }

    /// <summary>One step of the campaign: a name for the intro banner, how it is won, and how to build it.</summary>
    public sealed record Stage(string Name, StageKind Kind, Action<GameWorld> Build);

    /// <summary>
    /// The order the game is played in, from Data/campaign.json (Streets, Pell, Cellars, Dova, Rooftops, Tithe Hall, Tithe Keep, the Clerk), then the end.
    /// Normal levels are cleared by reaching their exit door; boss arenas by freeing the boss.
    /// </summary>
    public sealed class Campaign
    {
        private static Stage[] _stages;

        /// <summary>The stages in play order, read from Data/campaign.json.</summary>
        public static Stage[] Stages => _stages ??= LoadStages();

        /// <summary>Forget the list so it is read again (the F11 reload).</summary>
        public static void Reload() => _stages = null;

        private static Stage[] LoadStages()
        {
            var list = new System.Collections.Generic.List<Stage>();
            string text = DataFiles.ReadText("campaign.json");
            if (text != null)
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(text, new System.Text.Json.JsonDocumentOptions { CommentHandling = System.Text.Json.JsonCommentHandling.Skip, AllowTrailingCommas = true });
                    foreach (var e in doc.RootElement.GetProperty("stages").EnumerateArray())
                    {
                        string name = e.GetProperty("name").GetString();
                        string build = e.GetProperty("build").GetString();
                        var kind = e.TryGetProperty("kind", out var k) && Enum.TryParse<StageKind>(k.GetString(), true, out var parsed) ? parsed : StageKind.Level;
                        var builder = MakeBuilder(build);
                        if (builder != null) list.Add(new Stage(name, kind, builder));
                        else Console.WriteLine($"[data] campaign.json: cannot build '{build}' for stage '{name}', skipped.");
                    }
                }
                catch (Exception ex) when (ex is System.Text.Json.JsonException || ex is InvalidOperationException || ex is KeyNotFoundException)
                {
                    Console.WriteLine($"[data] campaign.json could not be read: {ex.Message}");
                    list.Clear();
                }
            }
            if (list.Count == 0)
            {
                Console.WriteLine("[data] no usable campaign.json: falling back to the first level only.");
                list.Add(new Stage("The Lantern Streets", StageKind.Level, w => LoadWithExit(w, LevelFile.Get("streets"))));
            }
            return list.ToArray();
        }

        private static Action<GameWorld> MakeBuilder(string build)
        {
            if (build.StartsWith("level:", StringComparison.Ordinal))
            {
                string id = build["level:".Length..];
                return w => LoadWithExit(w, LevelFile.Get(id));
            }
            return build switch
            {
                "boss:pell" => BossArena.Build,
                "boss:dova" => DovaArena.Build,
                "boss:clerk" => ClerkArena.Build,
                "keep" => KeepLevel.Build,
                _ => null,
            };
        }

        public int Index { get; private set; }
        public int TotalSouls { get; private set; }

        public Stage Current => Stages[Index];
        public bool IsLast => Index >= Stages.Length - 1;

        /// <summary>Jump straight to a stage (debug keys) without banking souls.</summary>
        public void GoTo(int index)
        {
            KeepState.Reset();
            ClerkArena.Checkpoint = 0;
            Index = Math.Clamp(index, 0, Stages.Length - 1);
        }

        /// <summary>Keep the current stage index valid after the stage list was re-read.</summary>
        public void ClampIndex() => Index = Math.Clamp(Index, 0, Stages.Length - 1);

        /// <summary>Bank the souls freed in the stage just cleared and move on. Returns false after the last stage.</summary>
        public bool Advance(int soulsThisStage)
        {
            TotalSouls += soulsThisStage;
            KeepState.Reset();
            ClerkArena.Checkpoint = 0;
            if (IsLast) return false;
            Index++;
            return true;
        }

        public void Restart()
        {
            Index = 0;
            TotalSouls = 0;
            KeepState.Reset();
            ClerkArena.Checkpoint = 0;
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
