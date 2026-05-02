using System.Text.Json.Serialization;

namespace conda_infor_project.models
{
    /// <summary>
    /// Represents the response from Supabase Auth Sign Up/Sign In
    /// </summary>
    public class AuthResponse
    {
        [JsonPropertyName("user")]
        public AuthUser? User { get; set; }

        [JsonPropertyName("session")]
        public AuthSession? Session { get; set; }

        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonIgnore]
        public AuthSession? CurrentSession =>
            Session ?? (string.IsNullOrWhiteSpace(AccessToken)
                ? null
                : new AuthSession
                {
                    AccessToken = AccessToken,
                    TokenType = TokenType,
                    ExpiresIn = ExpiresIn,
                    RefreshToken = RefreshToken
                });
    }

    public class AuthUser
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("user_metadata")]
        public object? UserMetadata { get; set; }

        [JsonPropertyName("app_metadata")]
        public object? AppMetadata { get; set; }
    }

    public class AuthSession
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }
    }

    public class AuthRequest
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;
    }

    public class AuthError
    {
        [JsonPropertyName("code")]
        public int? Code { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; set; }

        [JsonPropertyName("error_code")]
        public string? ErrorCode { get; set; }

        [JsonPropertyName("msg")]
        public string? Msg { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        public string GetErrorMessage()
        {
            return ErrorDescription ?? Msg ?? Message ?? Error ?? "Unknown error";
        }
    }
}
