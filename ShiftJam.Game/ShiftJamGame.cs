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
        private readonly Campaign _campaign = new();
        private KeyboardState _prevKeys;
        private float _stageIntro;     // seconds left on the stage-name banner
        private bool _ending;          // the whole campaign is finished

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

            LoadStage();
        }

        /// <summary>Builds the current campaign stage from scratch.</summary>
        private void LoadStage()
        {
            _world = new GameWorld();
            _campaign.Current.Build(_world);
            _stageIntro = 2.5f;
            _ending = false;
        }

        /// <summary>Loads a standalone test level (debug keys), outside the campaign flow.</summary>
        private void LoadDebugLevel(Action<GameWorld> build)
        {
            _world = new GameWorld();
            build(_world);
            _stageIntro = 0;
            _ending = false;
        }

        private bool Pressed(KeyboardState keys, Keys key) => keys.IsKeyDown(key) && !_prevKeys.IsKeyDown(key);

        protected override void Update(GameTime gameTime)
        {
            var keys = Keyboard.GetState();
            if (keys.IsKeyDown(Keys.Escape) || GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            {
                try { Exit(); }
                catch (PlatformNotSupportedException) { /* the web build cannot exit */ }
            }

            // Debug: F1-F4 jump to campaign stages, F5/F6 the old test levels, R restarts the stage.
            for (int n = 0; n < Campaign.Stages.Length; n++)
                if (Pressed(keys, Keys.F1 + n)) { _campaign.GoTo(n); LoadStage(); }
            if (Pressed(keys, Keys.F5)) LoadDebugLevel(TestRoom.Build);
            else if (Pressed(keys, Keys.F6)) LoadDebugLevel(TestRoom.BuildDescent);

            bool confirm = Pressed(keys, Keys.Enter);
            if (_ending)
            {
                if (confirm || Pressed(keys, Keys.R)) { _campaign.Restart(); LoadStage(); }
            }
            else if (_world.LevelCleared && confirm)
            {
                if (_campaign.Advance(_world.SoulsFreed)) LoadStage();
                else _ending = true;
            }
            else if (!_world.Player.IsAlive && confirm) LoadStage();
            else if (Pressed(keys, Keys.R)) LoadStage();
            _prevKeys = keys;

            float dt = Math.Min((float)gameTime.ElapsedGameTime.TotalSeconds, 1f / 30f);
            if (_stageIntro > 0) _stageIntro -= dt;
            _world.Input = _ending ? default : _input.Read();
            _world.Update(dt);

            // Fell out of the bottom of the level: restart it (real respawn comes later).
            if (_world.Player.Position.Y > (_world.Tiles.Height + 4) * Metrics.TileSize || !_world.Player.IsAlive && _world.Player.Position.Y > _world.Tiles.Height * Metrics.TileSize)
                LoadStage();

            base.Update(gameTime);
        }

        /// <summary>Stage intro, level-cleared, defeated and ending banners, drawn in screen space.</summary>
        private void DrawEndBanner()
        {
            if (_ending)
            {
                Gfx.Fill(spriteBatch, new Rectangle(0, 0, Metrics.ScreenWidth, Metrics.ScreenHeight), new Color(0, 0, 0, 200));
                PixelFont.DrawCentered(spriteBatch, "THE END", 44, new Color(255, 225, 140), 4);
                PixelFont.DrawCentered(spriteBatch, "BUT THE TITHE IS STILL OUT THERE.", 82, Color.White, 1);
                PixelFont.DrawCentered(spriteBatch, "TOTAL SOULS FREED: " + _campaign.TotalSouls, 98, new Color(150, 220, 255), 2);
                PixelFont.DrawCentered(spriteBatch, "THANKS FOR PLAYING", 124, Color.White, 1);
                PixelFont.DrawCentered(spriteBatch, "ENTER TO PLAY AGAIN", 140, new Color(180, 200, 255), 1);
                return;
            }

            bool cleared = _world.LevelCleared;
            bool defeated = !_world.Player.IsAlive;
            if (!cleared && !defeated)
            {
                if (_stageIntro > 0)
                {
                    float a = Math.Clamp(_stageIntro / 0.6f, 0f, 1f);   // fades out over the last 0.6 s
                    int n = Array.IndexOf(Campaign.Stages, _campaign.Current) + 1;
                    PixelFont.DrawCentered(spriteBatch, "STAGE " + n, 40, new Color(180, 200, 255) * a, 1);
                    PixelFont.DrawCentered(spriteBatch, _campaign.Current.Name, 50, new Color(255, 225, 140) * a, 2);
                }
                return;
            }

            Gfx.Fill(spriteBatch, new Rectangle(0, 60, Metrics.ScreenWidth, 60), new Color(0, 0, 0, 170));
            if (cleared)
            {
                bool last = _campaign.IsLast;
                PixelFont.DrawCentered(spriteBatch, _campaign.Current.Kind == StageKind.Boss ? "BOSS FREED" : "LEVEL CLEARED", 70, new Color(255, 225, 140), 3);
                PixelFont.DrawCentered(spriteBatch, "SOULS FREED: " + _world.SoulsFreed, 94, Color.White, 2);
                PixelFont.DrawCentered(spriteBatch, last ? "ENTER FOR THE ENDING" : "ENTER TO CONTINUE", 108, new Color(180, 200, 255), 1);
            }
            else
            {
                PixelFont.DrawCentered(spriteBatch, "DEFEATED", 72, new Color(255, 110, 110), 3);
                PixelFont.DrawCentered(spriteBatch, "ENTER TO TRY AGAIN", 100, Color.White, 1);
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
