using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ShiftJam
{
    /// <summary>
    /// Main game type, shared by every platform. Renders the world at a small internal
    /// resolution and scales it up with point sampling for the 1980s look.
    /// </summary>
    public class ShiftJamGame : Game
    {
        protected GraphicsDeviceManager graphics;
        protected SpriteBatch spriteBatch;

        private RenderTarget2D _screen;
        private readonly InputReader _input = new();
        private GameWorld _world;
        private Action<GameWorld> _buildLevel = TestRoom.Build;
        private KeyboardState _prevKeys;

        public ShiftJamGame()
        {
            graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = Metrics.ScreenWidth * 3,
                PreferredBackBufferHeight = Metrics.ScreenHeight * 3,
            };
            Content.RootDirectory = "Content";
            Window.Title = "Threshold";
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            Gfx.Init(GraphicsDevice);
            SpriteStore.Load(Content);
            Sfx.Load(Content);
            _screen = new RenderTarget2D(GraphicsDevice, Metrics.ScreenWidth, Metrics.ScreenHeight);

            LoadLevel(TestRoom.Build);
        }

        private void LoadLevel(Action<GameWorld> build)
        {
            _buildLevel = build;
            _world = new GameWorld();
            build(_world);
        }

        protected override void Update(GameTime gameTime)
        {
            var keys = Keyboard.GetState();
            if (keys.IsKeyDown(Keys.Escape) || GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            {
                try { Exit(); }
                catch (PlatformNotSupportedException) { /* the web build cannot exit */ }
            }

            // Debug level select: F1 wide level, F2 descent (Hollowed and Flicker), F3 boss arena (Pell), R restart.
            if (keys.IsKeyDown(Keys.F1) && !_prevKeys.IsKeyDown(Keys.F1)) LoadLevel(TestRoom.Build);
            else if (keys.IsKeyDown(Keys.F2) && !_prevKeys.IsKeyDown(Keys.F2)) LoadLevel(TestRoom.BuildDescent);
            else if (keys.IsKeyDown(Keys.F3) && !_prevKeys.IsKeyDown(Keys.F3)) LoadLevel(BossArena.Build);
            else if (keys.IsKeyDown(Keys.R) && !_prevKeys.IsKeyDown(Keys.R)) LoadLevel(_buildLevel);
            _prevKeys = keys;

            float dt = Math.Min((float)gameTime.ElapsedGameTime.TotalSeconds, 1f / 30f);
            _world.Input = _input.Read();
            _world.Update(dt);

            // Fell out of the bottom of the level: restart it (real respawn comes later).
            if (_world.Player.Position.Y > (_world.Tiles.Height + 4) * Metrics.TileSize || !_world.Player.IsAlive && _world.Player.Position.Y > _world.Tiles.Height * Metrics.TileSize)
                LoadLevel(_buildLevel);

            base.Update(gameTime);
        }

        /// <summary>Level-cleared and defeated banners, drawn in screen space.</summary>
        private void DrawEndBanner()
        {
            bool cleared = _world.LevelCleared;
            bool defeated = !_world.Player.IsAlive;
            if (!cleared && !defeated) return;

            Gfx.Fill(spriteBatch, new Rectangle(0, 60, Metrics.ScreenWidth, 60), new Color(0, 0, 0, 170));
            if (cleared)
            {
                PixelFont.DrawCentered(spriteBatch, "LEVEL CLEARED", 72, new Color(255, 225, 140), 3);
                PixelFont.DrawCentered(spriteBatch, "SOULS FREED: " + _world.SoulsFreed, 96, Color.White, 2);
                PixelFont.DrawCentered(spriteBatch, "R TO REPLAY", 108, new Color(180, 200, 255), 1);
            }
            else
            {
                PixelFont.DrawCentered(spriteBatch, "DEFEATED", 72, new Color(255, 110, 110), 3);
                PixelFont.DrawCentered(spriteBatch, "R TO TRY AGAIN", 100, Color.White, 1);
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.SetRenderTarget(_screen);
            GraphicsDevice.Clear(_world.Player.Plane == Plane.Astral ? new Color(12, 18, 44) : new Color(20, 16, 28));

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                null, null, null, _world.Camera.Transform);
            _world.Draw(spriteBatch);
            spriteBatch.End();

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            Hud.Draw(spriteBatch, _world.Player);
            foreach (var e in _world.Entities)
                if (e is IBoss boss) BossBar.Draw(spriteBatch, boss);
            DrawEndBanner();
            _world.DrawScreenFx(spriteBatch);
            spriteBatch.End();

            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);

            // Integer scale, centred, so pixels stay square.
            var vp = GraphicsDevice.Viewport;
            int scale = Math.Max(1, Math.Min(vp.Width / Metrics.ScreenWidth, vp.Height / Metrics.ScreenHeight));
            var dest = new Rectangle(
                (vp.Width - Metrics.ScreenWidth * scale) / 2,
                (vp.Height - Metrics.ScreenHeight * scale) / 2,
                Metrics.ScreenWidth * scale, Metrics.ScreenHeight * scale);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp);
            spriteBatch.Draw(_screen, dest, Color.White);
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
