using System;
using Microsoft.Xna.Framework;

namespace ShiftJam
{
    /// <summary>
    /// Scrolls to follow the player, clamped to the level. Supports horizontal and downward levels
    /// (and upward, which is just the same maths). Dead zone, smoothing, and look-ahead in the facing
    /// direction. Facing is inferred from target movement unless supplied via the overload.
    /// </summary>
    public sealed class Camera
    {
        private const float DeadX = 10f, DeadY = 20f;
        private static float LookAhead = Cfg.F("Camera.LookAhead", 36f, v => LookAhead = v);
        private const float RateX = 8f, RateY = 10f, RateLook = 4f;
        private const float MaxLagX = 70f, MaxLagY = 60f; // hard limit so the target never nears the screen edge

        private bool _init;
        private float _lastX;
        private int _facing = 1;
        private float _look;
        private float _cx, _cy; // view centre, unclamped

        public Vector2 Position;   // top-left of the view, in world pixels


        private float _shakeMag, _shakeTime, _shakeDur;
        private int _shakeStep;
        private Point _shakeOffset;

        public Matrix Transform => Matrix.CreateTranslation(-MathF.Round(Position.X) + _shakeOffset.X, -MathF.Round(Position.Y) + _shakeOffset.Y, 0);

        /// <summary>Screen shake: peak magnitude in pixels, decaying linearly over duration seconds. Keeps the stronger of overlapping shakes.</summary>
        public void Shake(float magnitude, float duration)
        {
            float current = _shakeDur > 0 ? _shakeMag * (_shakeTime / _shakeDur) : 0f;
            if (magnitude < current) return;
            _shakeMag = magnitude; _shakeTime = _shakeDur = MathF.Max(0.001f, duration);
        }

        /// <summary>Advance the shake. Call every frame, including during hit-stop.</summary>
        public void UpdateShake(float dt)
        {
            if (_shakeTime <= 0) { _shakeOffset = Point.Zero; return; }
            _shakeTime -= dt;
            if (_shakeTime <= 0) { _shakeOffset = Point.Zero; return; }
            float m = _shakeMag * (_shakeTime / _shakeDur);
            _shakeStep++;
            // Deterministic hash noise, pixel-snapped.
            uint h = (uint)_shakeStep * 2654435761u;
            float rx = ((h >> 8) & 0xFFFF) / 32767.5f - 1f;
            h = (h ^ (h >> 13)) * 1274126177u;
            float ry = ((h >> 8) & 0xFFFF) / 32767.5f - 1f;
            _shakeOffset = new Point((int)MathF.Round(rx * m), (int)MathF.Round(ry * m));
        }

        /// <summary>Snap straight to the target (call on level start or teleport).</summary>
        public void Reset() => _init = false;

        public void Follow(Rectangle target, ITileWorld world, float dt) => Follow(target, world, dt, 0);

        /// <summary>facing: -1 / +1 to force look-ahead direction, 0 to infer it from movement.</summary>
        public void Follow(Rectangle target, ITileWorld world, float dt, int facing)
        {
            float tx = target.Center.X, ty = target.Center.Y;
            if (!_init)
            {
                _init = true;
                _cx = tx; _cy = ty; _lastX = tx; _look = 0;
            }

            if (facing != 0) _facing = facing > 0 ? 1 : -1;
            else if (MathF.Abs(tx - _lastX) > 0.05f) _facing = tx > _lastX ? 1 : -1;
            _lastX = tx;

            _look += (_facing * LookAhead - _look) * (1f - MathF.Exp(-RateLook * dt));
            float fx = tx + _look;

            // Dead zone: only move the goal when the focus leaves the zone around the current centre.
            float gx = MathHelper.Clamp(_cx, fx - DeadX, fx + DeadX);
            float gy = MathHelper.Clamp(_cy, ty - DeadY, ty + DeadY);

            _cx += (gx - _cx) * (1f - MathF.Exp(-RateX * dt));
            _cy += (gy - _cy) * (1f - MathF.Exp(-RateY * dt));

            // Never let the player drift near the screen edge (fast falls)
            _cx = MathHelper.Clamp(_cx, tx - MaxLagX, tx + MaxLagX);
            _cy = MathHelper.Clamp(_cy, ty - MaxLagY, ty + MaxLagY);

            Position.X = ClampAxis(_cx - Metrics.ScreenWidth / 2f, world.Width * Metrics.TileSize, Metrics.ScreenWidth);
            Position.Y = ClampAxis(_cy - Metrics.ScreenHeight / 2f, world.Height * Metrics.TileSize, Metrics.ScreenHeight);
        }

        private static float ClampAxis(float pos, float levelSize, float viewSize) =>
            levelSize <= viewSize ? (levelSize - viewSize) / 2f : MathHelper.Clamp(pos, 0, levelSize - viewSize);
    }
}
