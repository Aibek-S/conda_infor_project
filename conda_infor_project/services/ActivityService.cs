using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using conda_infor_project.db;
using conda_infor_project.models;

namespace conda_infor_project.services
{
    public class ActivityService
    {
        private readonly HttpClient _client;
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public ActivityService()
        {
            _client = DataBase.GetClient();
        }

        public async Task SubmitActivityAsync(ActivitySnapshot snapshot, string accessToken)
        {
            string json = JsonSerializer.Serialize(snapshot, JsonOptions);
            using var request = new HttpRequestMessage(HttpMethod.Post, "/functions/v1/submit-activity");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _client.SendAsync(request);
            string responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(GetErrorMessage(responseJson, $"HTTP {(int)response.StatusCode}"));
            }
        }

        private static string GetErrorMessage(string json, string fallback)
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(json);
                if (document.RootElement.TryGetProperty("error", out JsonElement error))
                {
                    return error.GetString() ?? fallback;
                }
            }
            catch
            {
            }

            return fallback;
        }
    }
}
