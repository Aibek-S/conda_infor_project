using System.Text.Json;
using System.Text.Json.Serialization;

namespace conda_infor_project.models
{
    public class ActivitySnapshot
    {
        [JsonPropertyName("activeWindow")]
        public string ActiveWindow { get; set; } = string.Empty;

        [JsonPropertyName("processes")]
        public List<string> Processes { get; set; } = new List<string>();

        [JsonIgnore]
        public bool IsFallback { get; set; }

        [JsonIgnore]
        public string DebugMessage { get; set; } = string.Empty;

        [JsonIgnore]
        public string DebugSource { get; set; } = string.Empty;

        [JsonIgnore]
        public string ScriptPath { get; set; } = string.Empty;
    }

    public class PythonActivitySnapshot
    {
        [JsonPropertyName("activeWindow")]
        public string? ActiveWindow { get; set; }

        [JsonPropertyName("processes")]
        public List<string>? Processes { get; set; }

        [JsonPropertyName("debug")]
        public JsonElement? Debug { get; set; }
    }
}
