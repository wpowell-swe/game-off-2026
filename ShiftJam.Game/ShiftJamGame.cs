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
        private bool _title = true;    // showing the title screen
        private Texture2D _titleArt;
        private float _titleClock;

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

            try { _titleArt = Content.Load<Texture2D>("Ui/title"); }
            catch (Exception) { /* no art yet: the title falls back to a plain background */ }

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

            // Debug: F1-F7 jump to campaign stages, F9/F10 the old test levels, R restarts the stage.
            for (int n = 0; n < Campaign.Stages.Length; n++)
                if (Pressed(keys, Keys.F1 + n)) { _title = false; _campaign.GoTo(n); LoadStage(); }
            if (Pressed(keys, Keys.F9)) { _title = false; LoadDebugLevel(TestRoom.Build); }
            else if (Pressed(keys, Keys.F10)) { _title = false; LoadDebugLevel(TestRoom.BuildDescent); }

            bool confirm = Pressed(keys, Keys.Enter) || Pressed(keys, Keys.Space);
            if (_title)
            {
                _titleClock += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (Pressed(keys, Keys.Enter) || Pressed(keys, Keys.Space)) { _title = false; _campaign.Restart(); LoadStage(); }
                _prevKeys = keys;
                base.Update(gameTime);
                return;
            }
            if (_ending)
            {
                if (confirm || Pressed(keys, Keys.R)) { _campaign.Restart(); _title = true; _titleClock = 0; }
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

            // Took a door in the Keep: load where it leads (the same stage, a different part of it).
            if (_world.PortalRequest is int target)
            {
                KeepState.Go(target, _world.SoulsFreed);
                LoadStage();
                _stageIntro = 0;
                base.Update(gameTime);
                return;
            }

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
                PixelFont.DrawCentered(spriteBatch, "DESIGN BY WILLIAM POWELL. MADE WITH AI ASSISTANCE:", 156, new Color(150, 150, 170), 1);
                PixelFont.DrawCentered(spriteBatch, "CODE, STORY AND TITLE ART. ENGINE: KNI. SEE CREDITS.MD", 164, new Color(150, 150, 170), 1);
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

        /// <summary>Title art with the game name and a blinking prompt, drawn in screen space.</summary>
        private void DrawTitle()
        {
            Gfx.Fill(spriteBatch, new Rectangle(0, 0, Metrics.ScreenWidth, Metrics.ScreenHeight), new Color(20, 16, 28));
            if (_titleArt != null)
                spriteBatch.Draw(_titleArt, new Rectangle(0, 0, Metrics.ScreenWidth, Metrics.ScreenHeight), Color.White);
            // Darken the top and bottom so the text reads over the art.
            Gfx.Fill(spriteBatch, new Rectangle(0, 0, Metrics.ScreenWidth, 62), new Color(0, 0, 0, 110));

            PixelFont.DrawCentered(spriteBatch, "THRESHOLD", 12, new Color(255, 225, 140), 6);
            PixelFont.DrawCentered(spriteBatch, "A GAME OF TWO PLANES", 48, new Color(150, 220, 255), 1);
            if ((int)(_titleClock * 1.6f) % 2 == 0)
                PixelFont.DrawCentered(spriteBatch, "PRESS ENTER", 114, Color.White, 2);

            // Controls: an action label with keycaps beside it, two columns of three.
            Gfx.Fill(spriteBatch, new Rectangle(0, 132, Metrics.ScreenWidth, 48), new Color(0, 0, 0, 170));
            DrawBinding("MOVE", 20, 138, "A", "D");
            DrawBinding("JUMP", 20, 150, "SPACE");
            DrawBinding("SWAP PLANE", 20, 162, "SHIFT");
            DrawBinding("LIGHT", 176, 138, "J");
            DrawBinding("HEAVY", 176, 150, "K");
            DrawBinding("BLOCK", 176, 162, "L");
        }

        /// <summary>One controls row: a dim action label, then each key drawn as a small keycap.</summary>
        private void DrawBinding(string action, int x, int y, params string[] keys)
        {
            PixelFont.Draw(spriteBatch, action, x, y + 2, new Color(150, 220, 255), 1);
            int kx = x + 46;
            foreach (var key in keys)
            {
                int w = PixelFont.Width(key) + 5;
                Gfx.Fill(spriteBatch, new Rectangle(kx, y, w, 9), new Color(230, 220, 200));
                Gfx.Fill(spriteBatch, new Rectangle(kx, y + 8, w, 1), new Color(140, 120, 100));
                PixelFont.Draw(spriteBatch, key, kx + 2, y + 2, new Color(30, 24, 40), 1);
                kx += w + 3;
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            if (_title)
            {
                GraphicsDevice.SetRenderTarget(_screen);
                GraphicsDevice.Clear(new Color(20, 16, 28));
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
                DrawTitle();
                spriteBatch.End();
                PresentScreen();
                base.Draw(gameTime);
                return;
            }

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
            if (!string.IsNullOrEmpty(_world.StatusText))
                PixelFont.Draw(spriteBatch, _world.StatusText, Metrics.ScreenWidth - 6 - PixelFont.Width(_world.StatusText), 8, new Color(255, 225, 140), 1);
            DrawEndBanner();
            _world.DrawScreenFx(spriteBatch);
            spriteBatch.End();

            PresentScreen();
            base.Draw(gameTime);
        }

        /// <summary>Scales the 320x180 render target to the window with integer scaling.</summary>
        private void PresentScreen()
        {
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
        }
    }
}
