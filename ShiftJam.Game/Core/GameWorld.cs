using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftJam
{
    /// <summary>Owns everything in the current level and runs it.</summary>
    public sealed class GameWorld
    {
        public ITileWorld Tiles { get; set; }
        public Player Player { get; set; }
        public Camera Camera { get; } = new Camera();
        public InputFrame Input;

        public readonly List<IEntity> Entities = new();
        public readonly List<IHittable> Hittables = new();

        public void Add(IEntity entity)
        {
            Entities.Add(entity);
            if (entity is IHittable h) Hittables.Add(h);
        }

        /// <summary>Applies an attack to every live target in the same plane that overlaps its area.</summary>
        public void SpawnHit(in Hit hit, IHittable source = null)
        {
            foreach (var target in Hittables)
            {
                if (target == source || !target.IsAlive || target.Plane != hit.Plane) continue;
                if (hit.Area.Intersects(target.Hurtbox)) target.TakeHit(hit);
            }
        }

        public void Update(float dt)
        {
            foreach (var e in Entities.ToArray()) e.Update(dt, this);
            Hittables.RemoveAll(h => !h.IsAlive);
            Camera.Follow(Player.Hurtbox, Tiles, dt);
        }

        public void Draw(SpriteBatch sb)
        {
            if (Tiles is IDrawableTiles tiles) tiles.Draw(sb, Camera);
            foreach (var e in Entities) e.Draw(sb);
        }
    }

    /// <summary>Optional: a tile world that can draw itself.</summary>
    public interface IDrawableTiles
    {
        void Draw(SpriteBatch sb, Camera camera);
    }
}
