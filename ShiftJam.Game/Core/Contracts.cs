using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftJam
{
    /// <summary>Which plane something exists in. The whole game is built around this.</summary>
    public enum Plane { Material, Astral }

    /// <summary>Fixed sizes. Internal resolution is small on purpose (1980s look), scaled up to the window.</summary>
    public static class Metrics
    {
        public const int ScreenWidth = 320;
        public const int ScreenHeight = 180;
        public const int TileSize = 16;
    }

    /// <summary>One frame of player intent, already decoded from keyboard or gamepad.</summary>
    public struct InputFrame
    {
        public float MoveX;          // -1 to 1
        public bool JumpPressed;     // went down this frame
        public bool JumpHeld;
        public bool LightPressed;
        public bool HeavyPressed;
        public bool BlockHeld;
        public bool ShiftPressed;    // plane shift (wired up in the shift milestone)
    }

    /// <summary>An attack. Only hits targets that exist in the same Plane.</summary>
    public readonly record struct Hit(Rectangle Area, int Damage, int Facing, Plane Plane, float Knockback, bool BreaksGuard);

    /// <summary>Anything that can be hit.</summary>
    public interface IHittable
    {
        Rectangle Hurtbox { get; }
        Plane Plane { get; }
        bool IsAlive { get; }
        void TakeHit(in Hit hit);
    }

    /// <summary>Anything the world updates and draws every frame.</summary>
    public interface IEntity
    {
        void Update(float dt, GameWorld world);
        void Draw(SpriteBatch spriteBatch);
    }

    /// <summary>Tile data the physics asks about. Out-of-range rules live in the implementation.</summary>
    public interface ITileWorld
    {
        int Width { get; }   // in tiles
        int Height { get; }  // in tiles
        bool IsSolid(int tx, int ty, Plane plane);
    }
}
