using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using conda_infor_project.db;
using conda_infor_project.models;
using conda_infor_project.services;

namespace conda_infor_project.repository
{
    public class ClassRepository
    {
        private readonly HttpClient _client;
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public ClassRepository()
        {
            _client = DataBase.GetClient();
        }

        public async Task<List<SchoolClass>> GetTeacherClassesAsync(string teacherId, string accessToken)
        {
            string requestUrl = $"/rest/v1/classes?teacher_id=eq.{Uri.EscapeDataString(teacherId)}&select=id,name,teacher_id,created_at,updated_at&order=created_at.desc";
            using HttpRequestMessage request = CreateAuthorizedRequest(HttpMethod.Get, requestUrl, accessToken);
            HttpResponseMessage response = await _client.SendAsync(request);

            string json = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                Logger.LogWarning($"GetTeacherClasses error (Status {response.StatusCode}): {json}");
                throw new Exception("Не удалось загрузить классы учителя.");
            }

            return JsonSerializer.Deserialize<List<SchoolClass>>(json, JsonOptions) ?? new List<SchoolClass>();
        }

        public async Task<List<User>> GetClassStudentsAsync(string classId, string accessToken)
        {
            string requestUrl = $"/rest/v1/class_students?class_id=eq.{Uri.EscapeDataString(classId)}&select=profiles(id,email,full_name,role,class_id)&order=display_name.asc";
            using HttpRequestMessage request = CreateAuthorizedRequest(HttpMethod.Get, requestUrl, accessToken);
            HttpResponseMessage response = await _client.SendAsync(request);

            string json = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                Logger.LogWarning($"GetClassStudents error (Status {response.StatusCode}): {json}");
                throw new Exception("Не удалось загрузить учеников класса.");
            }

            List<ClassStudentRow> rows = JsonSerializer.Deserialize<List<ClassStudentRow>>(json, JsonOptions) ?? new List<ClassStudentRow>();
            return rows
                .Where(row => row.Profile != null)
                .Select(row => row.Profile!)
                .ToList();
        }

        public async Task<CreateClassResponse> CreateClassAsync(CreateClassRequest payload, string accessToken)
        {
            string json = JsonSerializer.Serialize(payload, JsonOptions);
            using HttpRequestMessage request = CreateAuthorizedRequest(HttpMethod.Post, "/functions/v1/create-class", accessToken);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _client.SendAsync(request);
            string responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Logger.LogWarning($"CreateClass function error (Status {response.StatusCode}): {responseJson}");
                throw new Exception(ExtractErrorMessage(responseJson, "Не удалось создать класс."));
            }

            return JsonSerializer.Deserialize<CreateClassResponse>(responseJson, JsonOptions)
                ?? throw new Exception("Функция создала пустой ответ.");
        }

        private static HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string requestUrl, string accessToken)
        {
            var request = new HttpRequestMessage(method, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return request;
        }

        private static string ExtractErrorMessage(string json, string fallback)
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

    internal class ClassStudentRow
    {
        [System.Text.Json.Serialization.JsonPropertyName("profiles")]
        public User? Profile { get; set; }
    }
}
