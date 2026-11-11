using System;

namespace ShiftJam
{
    /// <summary>
    /// The first two real levels. Legend: # solid, w Material-only wall (Astral passes), P player start,
    /// S Shell, R Wraith, H Hollowed, F Flicker, X level exit, . empty. The exit is a single tile at the far end.
    /// </summary>
    public static class StageLevels
    {
        public static Level Streets => LevelFile.Get("streets");

        public static Level Cellars => LevelFile.Get("cellars");

        public static Level Rooftops => LevelFile.Get("rooftops");

        public static Level TitheHall => LevelFile.Get("tithe_hall");
    }
}
