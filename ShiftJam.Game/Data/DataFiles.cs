using System;
using System.IO;
using Microsoft.Xna.Framework;

namespace ShiftJam
{
    /// <summary>
    /// Reads the game's data files (balance numbers, levels, text, the campaign list, sprite sizes).
    /// Normally they are the copies built into Content/Data. On desktop, setting the SHIFTJAM_DATA environment
    /// variable to a folder (for example ShiftJamContent/Data) reads them from there instead, so they can be
    /// edited and reloaded (F11) without building anything. The web build always uses the built-in copies.
    /// </summary>
    public static class DataFiles
    {
        /// <summary>The folder being read instead of the built-in data, or null.</summary>
        public static string OverrideDir { get; } = FindOverride();

        private static string FindOverride()
        {
            try
            {
                string dir = Environment.GetEnvironmentVariable("SHIFTJAM_DATA");
                return !string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir) ? Path.GetFullPath(dir) : null;
            }
            catch (Exception) { return null; }   // the web build has no environment
        }

        /// <summary>The text of a data file (a path like "levels/streets.txt"), or null if there is no such file.</summary>
        public static string ReadText(string relativePath)
        {
            try
            {
                if (OverrideDir != null)
                {
                    string path = Path.Combine(OverrideDir, relativePath);
                    if (File.Exists(path)) return File.ReadAllText(path);
                }
                using var stream = TitleContainer.OpenStream("Content/Data/" + relativePath);
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            catch (FileNotFoundException) { return null; }
            catch (DirectoryNotFoundException) { return null; }
            catch (Exception e)
            {
                Console.WriteLine($"[data] could not read {relativePath}: {e.GetType().Name}: {e.Message}");
                return null;
            }
        }
    }
}
