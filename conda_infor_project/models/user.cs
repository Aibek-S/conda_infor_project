using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace conda_infor_project.models
{
    public class User
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("login")]
        public string Login { get; set; } = string.Empty;

        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("class_id")]
        public string? ClassId { get; set; }

        public List<Log> Logs { get; set; } = new List<Log>();
    }
}
