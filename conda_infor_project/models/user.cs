using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace conda_infor_project.models
{
    public class User
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("login")]
        public string Login { get; set; }

        [JsonPropertyName("full_name")]
        public string FullName { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }

        public List<Log> Logs { get; set; }
    }
}
