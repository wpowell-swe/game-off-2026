using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ShiftJam
{
    public enum PauseChoice { None, Resume, Restart, Title }

    /// <summary>The pause menu: Resume, Restart stage, Volume, Quit to title. Up/Down (W/S) to move, Enter to choose, A/D or Left/Right for volume.</summary>
    public sealed class PauseMenu
    {
        private int _index;
        private float _clock;

        public void Open() { _index = 0; _clock = 0; }

        /// <summary>Handles one frame of input. Returns a choice when the player picks one (Escape counts as Resume).</summary>
        public PauseChoice Update(float dt, KeyboardState keys, KeyboardState prev)
        {
            _clock += dt;
            bool Pressed(Keys k) => keys.IsKeyDown(k) && !prev.IsKeyDown(k);
            if (Pressed(Keys.Escape) || Pressed(Keys.P)) return PauseChoice.Resume;
            if (Pressed(Keys.Up) || Pressed(Keys.W)) _index = (_index + 3) % 4;
            if (Pressed(Keys.Down) || Pressed(Keys.S)) _index = (_index + 1) % 4;
            if (_index == 2)
            {
                int step = (Pressed(Keys.Left) || Pressed(Keys.A) ? -1 : 0) + (Pressed(Keys.Right) || Pressed(Keys.D) ? 1 : 0);
                if (step != 0)
                {
                    Tuning.SfxMasterVolume = Math.Clamp((float)Math.Round((Tuning.SfxMasterVolume + step * 0.1f) * 10f) / 10f, 0f, 1f);
                    Sfx.Play(SfxId.LightHit, 1f);   // a sample at the new volume
                }
            }
            if (Pressed(Keys.Enter) || Pressed(Keys.Space))
            {
                switch (_index)
                {
                    case 0: return PauseChoice.Resume;
                    case 1: return PauseChoice.Restart;
                    case 3: return PauseChoice.Title;
                }
            }
            return PauseChoice.None;
        }

        public void Draw(SpriteBatch sb)
        {
            Gfx.Fill(sb, new Rectangle(0, 0, Metrics.ScreenWidth, Metrics.ScreenHeight), new Color(0, 0, 10, 170));
            var gold = new Color(255, 225, 140);
            PixelFont.DrawCentered(sb, Txt.S("pause.title", "PAUSED"), 30, gold, 3);

            string[] items =
            {
                Txt.S("pause.resume", "RESUME"),
                Txt.S("pause.restart", "RESTART STAGE"),
                Txt.S("pause.volume", "VOLUME") + "  < " + (int)Math.Round(Tuning.SfxMasterVolume * 100) + "% >",
                Txt.S("pause.title_screen", "QUIT TO TITLE"),
            };
            for (int i = 0; i < items.Length; i++)
            {
                int y = 70 + i * 16;
                bool sel = i == _index;
                PixelFont.DrawCentered(sb, items[i], y, sel ? Color.White : new Color(150, 160, 190), 1 + (sel ? 1 : 0));
                if (sel && ((int)(_clock * 4f) & 1) == 0)
                {
                    int w = PixelFont.Width(items[i], 2);
                    int x = (Metrics.ScreenWidth - w) / 2 - 10;
                    Gfx.Fill(sb, new Rectangle(x, y + 1, 5, 1), gold);
                    Gfx.Fill(sb, new Rectangle(x, y + 3, 7, 1), gold);
                    Gfx.Fill(sb, new Rectangle(x, y + 5, 5, 1), gold);
                    Gfx.Fill(sb, new Rectangle(x, y + 7, 3, 1), gold);
                }
            }
            int cy = 157;
            foreach (var c in Charms.Held())
            {
                PixelFont.DrawCentered(sb, c.Name + " - " + c.Desc, cy, new Color(255, 210, 100), 1);
                cy += 6;
            }
            PixelFont.DrawCentered(sb, Txt.S("pause.hint", "W S MOVE  ENTER CHOOSE  ESC RESUME"), 150, new Color(150, 160, 190), 1);
        }
    }
}
