using Microsoft.Xna.Framework.Input;

namespace ShiftJam
{
    /// <summary>Turns keyboard and gamepad state into an InputFrame.</summary>
    public sealed class InputReader
    {
        private KeyboardState _prevKeys;
        private GamePadState _prevPad;

        public InputFrame Read()
        {
            var keys = Keyboard.GetState();
            var pad = GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.One);
            var f = new InputFrame();

            if (keys.IsKeyDown(Keys.Left) || keys.IsKeyDown(Keys.A)) f.MoveX -= 1;
            if (keys.IsKeyDown(Keys.Right) || keys.IsKeyDown(Keys.D)) f.MoveX += 1;
            if (f.MoveX == 0) f.MoveX = pad.ThumbSticks.Left.X;

            f.JumpHeld = keys.IsKeyDown(Keys.Space) || keys.IsKeyDown(Keys.W) || pad.Buttons.A == ButtonState.Pressed;
            f.JumpPressed = f.JumpHeld && !(_prevKeys.IsKeyDown(Keys.Space) || _prevKeys.IsKeyDown(Keys.W) || _prevPad.Buttons.A == ButtonState.Pressed);
            f.LightPressed = Pressed(keys, Keys.J, pad.Buttons.X, _prevPad.Buttons.X);
            f.HeavyPressed = Pressed(keys, Keys.K, pad.Buttons.Y, _prevPad.Buttons.Y);
            f.ShiftPressed = Pressed(keys, Keys.LeftShift, pad.Buttons.B, _prevPad.Buttons.B);
            f.BlockHeld = keys.IsKeyDown(Keys.L) || pad.Buttons.RightShoulder == ButtonState.Pressed;

            _prevKeys = keys;
            _prevPad = pad;
            return f;
        }

        private bool Pressed(KeyboardState keys, Keys key, ButtonState pad, ButtonState prevPad) =>
            (keys.IsKeyDown(key) && !_prevKeys.IsKeyDown(key)) ||
            (pad == ButtonState.Pressed && prevPad != ButtonState.Pressed);
    }
}
