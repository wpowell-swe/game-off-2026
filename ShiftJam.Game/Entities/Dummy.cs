using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftJam
{
    /// <summary>Training dummy: takes hits, flashes, gets knocked back, vanishes when dead.</summary>
    public sealed class Dummy : IEntity, IHittable
    {
        public static readonly Point Size = new(14, 28);
        private static float Gravity = Cfg.F("Dummy.Gravity", 600f, v => Gravity = v);
        private static float FlashTime = Cfg.F("Dummy.FlashTime", 0.12f, v => FlashTime = v);

        public Vector2 Position;
        public Vector2 Velocity;
        public int Health = 60;
        private float _flash;

        public Plane Plane => Plane.Material;
        public bool IsAlive => Health > 0;
        public Rectangle Hurtbox => new((int)Position.X, (int)Position.Y, Size.X, Size.Y);

        public Dummy(Vector2 position) { Position = position; }

        public void Update(float dt, GameWorld world)
        {
            if (!IsAlive) return;
            if (_flash > 0) _flash -= dt;
            Velocity.Y += Gravity * dt;
            Mover.Move(world.Tiles, Plane, ref Position, Size, ref Velocity, dt, out bool ground, out _, out _);
            if (ground) Velocity.X = MoveToward(Velocity.X, 0, 500f * dt);
        }

        private readonly Animator _anim = new(SpriteDefs.Dummy);

        public void Draw(SpriteBatch sb)
        {
            if (!IsAlive) return;
            var r = new Rectangle((int)MathF.Round(Position.X), (int)MathF.Round(Position.Y), Size.X, Size.Y);
            if (_flash > 0 ? _anim.Play("hurt", "idle") : _anim.Play("idle"))
            {
                _anim.Draw(sb, new Point(r.X + Size.X / 2, r.Bottom), 1, false, Color.White);
                return;
            }
            Gfx.Fill(sb, r, _flash > 0 ? Color.White : Color.Gray);
        }

        public void TakeHit(in Hit hit)
        {
            if (!IsAlive) return;
            Health -= hit.Damage;
            _flash = FlashTime;
            Velocity.X = hit.Facing * hit.Knockback;
            Velocity.Y = -60f;
        }

        private static float MoveToward(float v, float target, float step) =>
            v < target ? MathF.Min(v + step, target) : MathF.Max(v - step, target);
    }
}
