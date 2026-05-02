using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using conda_infor_project.db;
using conda_infor_project.models;
using conda_infor_project.services;

namespace conda_infor_project.repository
{
    public class AuthRepository
    {
        private readonly HttpClient _client;
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public AuthRepository()
        {
            _client = DataBase.GetClient();
        }

        private static HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string requestUrl, string accessToken)
        {
            var request = new HttpRequestMessage(method, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return request;
        }

        private static string BuildAuthErrorMessage(string errorJson, string fallbackMessage)
        {
            AuthError? error = JsonSerializer.Deserialize<AuthError>(errorJson, JsonOptions);
            string errorMessage = error?.GetErrorMessage() ?? fallbackMessage;

            if (error?.Code == 429 ||
                string.Equals(error?.ErrorCode, "over_email_send_rate_limit", StringComparison.OrdinalIgnoreCase))
            {
                int? retrySeconds = ExtractRetrySeconds(errorMessage);
                return retrySeconds.HasValue
                    ? $"Слишком много запросов. Supabase разрешит повторить через {retrySeconds.Value} сек."
                    : "Слишком много запросов. Подождите немного и попробуйте снова.";
            }

            return errorMessage;
        }

        private static int? ExtractRetrySeconds(string message)
        {
            Match match = Regex.Match(message, @"after\s+(\d+)\s+seconds", RegexOptions.IgnoreCase);
            return match.Success && int.TryParse(match.Groups[1].Value, out int seconds)
                ? seconds
                : null;
        }

        public async Task<AuthResponse> SignUpAsync(string email, string password)
        {
            try
            {
                string requestUrl = "/auth/v1/signup";
                var request = new AuthRequest { Email = email, Password = password };
                string json = JsonSerializer.Serialize(request);

                Logger.LogInfo($"SignUp request: POST {requestUrl}");
                Logger.LogInfo($"Request body: {json}");

                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _client.PostAsync(requestUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    string errorJson = await response.Content.ReadAsStringAsync();
                    Logger.LogError($"SignUp error response (Status {response.StatusCode}): {errorJson}");

                    string errorMsg = BuildAuthErrorMessage(errorJson, "Sign up failed");
                    Logger.LogError($"SignUp failed: {errorMsg}");
                    throw new Exception(errorMsg);
                }

                string responseJson = await response.Content.ReadAsStringAsync();
                Logger.LogInfo($"SignUp response: {responseJson}");

                AuthResponse? authResponse = JsonSerializer.Deserialize<AuthResponse>(responseJson, JsonOptions);
                if (authResponse == null)
                {
                    throw new Exception("Sign up failed: empty auth response");
                }

                Logger.LogInfo($"SignUp successful, userId: {authResponse?.User?.Id}");
                return authResponse!;
            }
            catch (Exception ex)
            {
                Logger.LogError("SignUpAsync error", ex);
                throw;
            }
        }

        public async Task<AuthResponse> SignInAsync(string email, string password)
        {
            try
            {
                string requestUrl = "/auth/v1/token?grant_type=password";
                var request = new AuthRequest { Email = email, Password = password };
                string json = JsonSerializer.Serialize(request);

                Logger.LogInfo($"SignIn request: POST {requestUrl}");
                Logger.LogInfo($"Request body: {json}");

                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _client.PostAsync(requestUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    string errorJson = await response.Content.ReadAsStringAsync();
                    Logger.LogError($"SignIn error response (Status {response.StatusCode}): {errorJson}");

                    string errorMsg = BuildAuthErrorMessage(errorJson, "Sign in failed");
                    Logger.LogError($"SignIn failed: {errorMsg}");
                    throw new Exception(errorMsg);
                }

                string responseJson = await response.Content.ReadAsStringAsync();
                Logger.LogInfo($"SignIn response: {responseJson}");

                AuthResponse? authResponse = JsonSerializer.Deserialize<AuthResponse>(responseJson, JsonOptions);
                if (authResponse == null)
                {
                    throw new Exception("Sign in failed: empty auth response");
                }

                Logger.LogInfo($"SignIn successful for email: {email}");
                return authResponse;
            }
            catch (Exception ex)
            {
                Logger.LogError("SignInAsync error", ex);
                throw;
            }
        }

        public async Task<User> CreateUserProfileAsync(string userId, string email, string fullName, string role, string? accessToken = null)
        {
            try
            {
                string requestUrl = "/rest/v1/profiles";
                var user = new User
                {
                    Id = userId,
                    Email = email,
                    Login = email,
                    FullName = fullName,
                    Role = role
                };

                string json = JsonSerializer.Serialize(user);

                Logger.LogInfo($"CreateUserProfile request: POST {requestUrl}");
                Logger.LogInfo($"Request body: {json}");

                using StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                using HttpRequestMessage request = string.IsNullOrWhiteSpace(accessToken)
                    ? new HttpRequestMessage(HttpMethod.Post, requestUrl)
                    : CreateAuthorizedRequest(HttpMethod.Post, requestUrl, accessToken);
                request.Headers.Add("Prefer", "return=representation");
                request.Content = content;

                HttpResponseMessage response = await _client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    string errorJson = await response.Content.ReadAsStringAsync();
                    Logger.LogError($"CreateUserProfile error response (Status {response.StatusCode}): {errorJson}");
                    throw new Exception("Failed to create user profile");
                }

                string responseJson = await response.Content.ReadAsStringAsync();
                Logger.LogInfo($"CreateUserProfile response: {responseJson}");

                var profiles = JsonSerializer.Deserialize<List<User>>(responseJson, JsonOptions);

                Logger.LogInfo($"User profile created for userId: {userId}");
                return profiles?.FirstOrDefault()
                    ?? throw new Exception("Failed to create user profile: empty response");
            }
            catch (Exception ex)
            {
                Logger.LogError("CreateUserProfileAsync error", ex);
                throw;
            }
        }

        public async Task<User?> GetUserByEmailAsync(string email, string accessToken)
        {
            try
            {
                string requestUrl = $"/rest/v1/profiles?email=eq.{Uri.EscapeDataString(email)}&select=*";

                Logger.LogInfo($"GetUserByEmail request: GET {requestUrl}");

                using HttpRequestMessage request = CreateAuthorizedRequest(HttpMethod.Get, requestUrl, accessToken);
                HttpResponseMessage response = await _client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    string errorJson = await response.Content.ReadAsStringAsync();
                    Logger.LogWarning($"GetUserByEmail error response (Status {response.StatusCode}): {errorJson}");
                    return null;
                }

                string json = await response.Content.ReadAsStringAsync();
                Logger.LogInfo($"GetUserByEmail response: {json}");

                var users = JsonSerializer.Deserialize<List<User>>(json, JsonOptions);

                if (users?.Count > 0)
                {
                    Logger.LogInfo($"User profile found for email: {email}");
                    return users[0];
                }

                Logger.LogWarning($"No user profile found for email: {email}");
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError("GetUserByEmailAsync error", ex);
                throw;
            }
        }
    }
}

