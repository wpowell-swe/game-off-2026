using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace ShiftJam
{
    public record DialogueLine(string Id, string Speaker, string Text, string Mood);

    public record Item(string Id, string Name, string Description, string ShiftEffect);

    // Source-generated serializer so loading stays trim-safe for the web build.
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(List<DialogueLine>))]
    [JsonSerializable(typeof(List<Item>))]
    internal partial class GeneratedContentJson : JsonSerializerContext
    {
    }

    /// <summary>
    /// Loads the JSON files written by the pipeline (see /pipeline) from Content/Generated.
    /// </summary>
    public static class GeneratedContent
    {
        public static List<DialogueLine> LoadDialogue() =>
            Load("dialogue", GeneratedContentJson.Default.ListDialogueLine);

        public static List<Item> LoadItems() =>
            Load("item", GeneratedContentJson.Default.ListItem);

        private static List<T> Load<T>(string kind, System.Text.Json.Serialization.Metadata.JsonTypeInfo<List<T>> info)
        {
            string path = $"Content/Generated/{kind}.json";
            try
            {
                using Stream stream = TitleContainer.OpenStream(path);
                return JsonSerializer.Deserialize(stream, info) ?? new List<T>();
            }
            catch (FileNotFoundException)
            {
                return new List<T>(); // nothing generated yet
            }
            catch (DirectoryNotFoundException)
            {
                return new List<T>();
            }
        }
    }
}
