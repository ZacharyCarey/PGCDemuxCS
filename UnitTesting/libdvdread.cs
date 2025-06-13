using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace UnitTesting
{
    public class libdvdread
    {

        public static libdvdread Load(string path)
        {
            string jsonString = File.ReadAllText(path);
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.AllowTrailingCommas = true;
            options.IncludeFields = true;
            return JsonSerializer.Deserialize<libdvdread>(jsonString, options)!;
        }

        [JsonPropertyName("device")]
        public string FilePath;

        [JsonPropertyName("title")]
        public string Title;

        [JsonPropertyName("vmg_id")]
        public string VMG_ID;

        [JsonPropertyName("provider_id")]
        public string ProviderID;

        [JsonPropertyName("longtest_track")]
        public int LongestTrack;

        [JsonPropertyName("track")]
        public List<Track> Tracks = new();
    }

    public class Track
    {
        [JsonPropertyName("ix")]
        public int Index;

        [JsonPropertyName("length")]
        public double Length;

        [JsonPropertyName("vts_id")]
        public string VtsID;

        [JsonPropertyName("vts")]
        public int VTS;

        [JsonPropertyName("ttn")]
        public int TTN;

        [JsonPropertyName("fps")]
        public double FPS;

        [JsonPropertyName("format")]
        public string Format;

        [JsonPropertyName("aspect")]
        public string Aspect;

        [JsonPropertyName("width")]
        public int Width;

        [JsonPropertyName("height")]
        public int Height;

        [JsonPropertyName("df")]
        public string DF;

        [JsonPropertyName("palette")]
        public List<string> Palette = new();

        [JsonPropertyName("angles")]
        public int Angles;

        [JsonPropertyName("audio")]
        public List<AudioTrack> AudioTracks = new();

        [JsonPropertyName("chapter")]
        public List<Chapter> Chapters = new();

        [JsonPropertyName("cell")]
        public List<Cell> Cells = new();

        [JsonPropertyName("subp")]
        public List<Subpicture> Subpictures = new();
    }

    public class AudioTrack
    {
        [JsonPropertyName("ix")]
        public int Index;

        [JsonPropertyName("langcode")]
        public string LanguageCode;

        [JsonPropertyName("language")]
        public string Language;

        [JsonPropertyName("format")]
        public string Format;

        [JsonPropertyName("frequency")]
        public uint Frequency;

        [JsonPropertyName("quantization")]
        public string Quantization;

        [JsonPropertyName("channels")]
        public int Channels;

        [JsonPropertyName("ap_mode")]
        public int ApMode;

        [JsonPropertyName("content")]
        public string Content;

        [JsonPropertyName("streamid")]
        public string StreamID; // Stored as hex
    }

    public class Chapter
    {
        [JsonPropertyName("ix")]
        public int Index;

        [JsonPropertyName("length")]
        public double Length;

        [JsonPropertyName("startcell")]
        public int StartCell;
    }

    public class Cell
    {
        [JsonPropertyName("ix")]
        public int Index;

        [JsonPropertyName("length")]
        public double Length;

        [JsonPropertyName("first_sector")]
        public int FirstSector;

        [JsonPropertyName("last_sector")]
        public int LastSector;
    }

    public class Subpicture
    {
        [JsonPropertyName("ix")]
        public int Index;

        [JsonPropertyName("langcode")]
        public string LanguageCode;

        [JsonPropertyName("language")]
        public string Language;

        [JsonPropertyName("content")]
        public string Content;

        [JsonPropertyName("streamid")]
        public string StreamID;
    }
}
