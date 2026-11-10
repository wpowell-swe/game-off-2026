using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftJam
{
    /// <summary>Placeholder drawing: coloured rectangles from one white pixel. Real sprites replace these later.</summary>
    public static class Gfx
    {
        public static Texture2D Pixel { get; private set; }

        public static void Init(GraphicsDevice device)
        {
            Pixel = new Texture2D(device, 1, 1);
            Pixel.SetData(new[] { Color.White });
        }

        public static void Fill(SpriteBatch sb, Rectangle r, Color c) => sb.Draw(Pixel, r, c);
    }
}
