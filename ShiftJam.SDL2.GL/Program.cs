using System;
using Microsoft.Xna.Framework;

namespace ShiftJam
{
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (Game game = new PlatformShiftJamGame())
                game.Run();
        }
    }
}
