using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftJam
{
    /// <summary>
    /// Fades to black, runs a change at the darkest moment (loading a stage, going to the title), then fades back in.
    /// While fading out the game holds still, so nothing can happen between the decision and the change.
    /// </summary>
    public sealed class ScreenFade
    {
        private static float OutTime = Cfg.F("Fade.OutTime", 0.28f, v => OutTime = v);
        private static float InTime = Cfg.F("Fade.InTime", 0.4f, v => InTime = v);

        private enum S { None, Out, In }
        private S _s = S.In;
        private float _t;
        private Action _midpoint;

        public ScreenFade() { _t = 1f; }   // start by fading in from black

        /// <summary>True while fading out: the caller should hold the game still.</summary>
        public bool Blocking => _s == S.Out;
        public bool Active => _s != S.None;

        /// <summary>Starts a fade; the action runs once the screen is black. Ignored if a fade-out is already running.</summary>
        public void Go(Action atBlack)
        {
            if (_s == S.Out) return;
            _s = S.Out; _t = 0; _midpoint = atBlack;
        }

        public void Update(float dt)
        {
            if (_s == S.Out)
            {
                _t += dt / Math.Max(0.01f, OutTime);
                if (_t >= 1f)
                {
                    _t = 1f;
                    var action = _midpoint; _midpoint = null;
                    _s = S.In;
                    action?.Invoke();
                }
            }
            else if (_s == S.In)
            {
                _t -= dt / Math.Max(0.01f, InTime);
                if (_t <= 0f) { _t = 0f; _s = S.None; }
            }
        }

        public void Draw(SpriteBatch sb)
        {
            if (_s == S.None || _t <= 0f) return;
            Gfx.Fill(sb, new Rectangle(0, 0, Metrics.ScreenWidth, Metrics.ScreenHeight), Color.Black * _t);
        }
    }
}
