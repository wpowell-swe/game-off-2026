using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftJam
{
    /// <summary>
    /// Level data: text rows plus an optional name. Spawn markers are parsed from the rows:
    /// P player, D dummy, S Shell (future), R Wraith (future). Positions are tile top-left, in pixels.
    /// </summary>
    public sealed record Level(string[] Rows, string Name = "")
    {
        public IReadOnlyList<(char Kind, Vector2 Position)> Spawns { get; } = ParseSpawns(Rows);

        private static List<(char, Vector2)> ParseSpawns(string[] rows)
        {
            var list = new List<(char, Vector2)>();
            for (int y = 0; y < rows.Length; y++)
                for (int x = 0; x < rows[y].Length; x++)
                {
                    char c = rows[y][x];
                    if (c == 'P' || c == 'D' || c == 'S' || c == 'R')
                        list.Add((c, new Vector2(x * Metrics.TileSize, y * Metrics.TileSize)));
                }
            return list;
        }
    }

    /// <summary>
    /// Tile grid built from text rows.
    /// '#' solid in both planes, 'w' solid in Material but passable in Astral, anything else empty
    /// (spawn markers are empty). Left, right and top edges are walls; the bottom is open.
    /// </summary>
    public sealed class TileMap : ITileWorld, IDrawableTiles
    {
        private const int T = Metrics.TileSize;
        private readonly char[][] _rows;

        public int Width { get; }
        public int Height { get; }

        public TileMap(Level level) : this(level.Rows) { }

        public TileMap(string[] rows)
        {
            Height = rows.Length;
            Width = 0;
            foreach (var r in rows) if (r.Length > Width) Width = r.Length;
            _rows = new char[Height][];
            for (int y = 0; y < Height; y++)
            {
                _rows[y] = new char[Width];
                for (int x = 0; x < Width; x++)
                    _rows[y][x] = x < rows[y].Length ? rows[y][x] : '.';
            }
        }

        public char At(int tx, int ty) =>
            tx < 0 || tx >= Width || ty < 0 || ty >= Height ? '.' : _rows[ty][tx];

        public bool IsSolid(int tx, int ty, Plane plane)
        {
            if (tx < 0 || tx >= Width || ty < 0) return true;
            if (ty >= Height) return false;
            char c = _rows[ty][tx];
            if (c == '#') return true;
            if (c == 'w') return plane == Plane.Material;
            return false;
        }

        public void Draw(SpriteBatch sb, Camera camera)
        {
            int x0 = Math.Max(0, (int)MathF.Floor(camera.Position.X / T));
            int x1 = Math.Min(Width - 1, (int)MathF.Floor((camera.Position.X + Metrics.ScreenWidth) / T));
            int y0 = Math.Max(0, (int)MathF.Floor(camera.Position.Y / T));
            int y1 = Math.Min(Height - 1, (int)MathF.Floor((camera.Position.Y + Metrics.ScreenHeight) / T));

            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    char c = _rows[y][x];
                    if (c == '#') DrawSolid(sb, x, y);
                    else if (c == 'w') DrawWall(sb, x, y);
                }
        }

        private void DrawSolid(SpriteBatch sb, int x, int y)
        {
            var body = new Color(78, 60, 52);
            var top = new Color(150, 118, 92);
            var dark = new Color(52, 40, 36);
            int px = x * T, py = y * T;
            Gfx.Fill(sb, new Rectangle(px, py, T, T), body);
            // Lighter edge only where it meets open air above
            if (!IsSolid(x, y - 1, Plane.Material) || At(x, y - 1) == 'w')
            {
                Gfx.Fill(sb, new Rectangle(px, py, T, 3), top);
                Gfx.Fill(sb, new Rectangle(px, py + 3, T, 1), dark);
            }
            // Darker bottom shade and a brick-ish seam
            Gfx.Fill(sb, new Rectangle(px, py + T - 2, T, 2), dark);
            Gfx.Fill(sb, new Rectangle(px + ((x + y) % 2 == 0 ? 8 : 4), py + 6, 1, 4), dark);
        }

        private static void DrawWall(SpriteBatch sb, int x, int y)
        {
            var body = new Color(104, 70, 150);
            var stripe = new Color(138, 100, 190);
            var edge = new Color(66, 42, 100);
            int px = x * T, py = y * T;
            Gfx.Fill(sb, new Rectangle(px, py, T, T), body);
            // Diagonal pattern (stair-stepped stripes), phase offset per tile so it flows across tiles
            for (int i = 0; i < T; i += 4)
            {
                int sx = (i + y * 4) % T;
                Gfx.Fill(sb, new Rectangle(px + sx, py + i, 4, 2), stripe);
            }
            Gfx.Fill(sb, new Rectangle(px, py, 1, T), edge);
            Gfx.Fill(sb, new Rectangle(px + T - 1, py, 1, T), edge);
        }
    }
}
