using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace UnitTesting
{
    internal class lsdvd
    {

        public static lsdvd Load(string path)
        {
            string jsonString = File.ReadAllText(path);
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.AllowTrailingCommas = true;
            return JsonSerializer.Deserialize<lsdvd>(jsonString, options)!;
        }

        [JsonPropertyName("IfoFiles")]
        public List<IfoFile> IfoFiles { get; set; } = new();
    }

    internal class IfoFile
    {
        [JsonPropertyName("Name")]
        public string Name { get; set; }

        [JsonPropertyName("Angles")]
        public Dictionary<string, int> Angles { get; set; } = new();

        [JsonPropertyName("MenuPGCs")]
        public List<int> MenuPGCs { get; set; } = new();

        [JsonPropertyName("TitlePGCs")]
        public List<int> TitlePGCs {  get; set; } = new();

        [JsonPropertyName("MenuVIDs")]
        public List<int> MenuVIDs { get; set; } = new();

        [JsonPropertyName("TitleVIDs")]
        public List<int> TitleVIDs { get; set; } = new();

        [JsonPropertyName("MenuCIDs")]
        public List<List<int>> MenuCIDs { get; set; } = new();

        [JsonPropertyName("TitleCIDs")]
        public List<List<int>> TitleCIDs { get; set; } = new();
    }
}
