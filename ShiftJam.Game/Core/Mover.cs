using System;
using Microsoft.Xna.Framework;

namespace ShiftJam
{
    /// <summary>
    /// Axis-separated AABB move and collide against tiles. Positions are the top-left of the box, in pixels.
    /// </summary>
    public static class Mover
    {
        private const int T = Metrics.TileSize;
        private const float Eps = 0.001f;

        public static void Move(ITileWorld world, Plane plane, ref Vector2 pos, Point size, ref Vector2 vel, float dt,
            out bool onGround, out bool hitWall, out bool hitCeiling)
        {
            onGround = hitWall = hitCeiling = false;

            pos.X += vel.X * dt;
            if (Overlaps(world, plane, pos, size))
            {
                if (vel.X > 0) pos.X = MathF.Floor((pos.X + size.X - Eps) / T) * T - size.X;
                else if (vel.X < 0) pos.X = MathF.Floor(pos.X / T) * T + T;
                vel.X = 0;
                hitWall = true;
            }

            pos.Y += vel.Y * dt;
            if (Overlaps(world, plane, pos, size))
            {
                if (vel.Y > 0)
                {
                    pos.Y = MathF.Floor((pos.Y + size.Y - Eps) / T) * T - size.Y;
                    onGround = true;
                }
                else if (vel.Y < 0)
                {
                    pos.Y = MathF.Floor(pos.Y / T) * T + T;
                    hitCeiling = true;
                }
                vel.Y = 0;
            }

            if (!onGround && vel.Y >= 0 && Overlaps(world, plane, new Vector2(pos.X, pos.Y + 1), size))
                onGround = true;
        }

        public static bool Overlaps(ITileWorld world, Plane plane, Vector2 pos, Point size)
        {
            int x0 = (int)MathF.Floor(pos.X / T);
            int x1 = (int)MathF.Floor((pos.X + size.X - Eps) / T);
            int y0 = (int)MathF.Floor(pos.Y / T);
            int y1 = (int)MathF.Floor((pos.Y + size.Y - Eps) / T);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    if (world.IsSolid(x, y, plane))
                        return true;
            return false;
        }
    }
}
