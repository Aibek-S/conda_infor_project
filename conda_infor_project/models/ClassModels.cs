using System.Text.Json.Serialization;

namespace conda_infor_project.models
{
    public class SchoolClass
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("teacher_id")]
        public string TeacherId { get; set; } = string.Empty;

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateClassRequest
    {
        [JsonPropertyName("className")]
        public string ClassName { get; set; } = string.Empty;

        [JsonPropertyName("studentPassword")]
        public string StudentPassword { get; set; } = string.Empty;

        [JsonPropertyName("students")]
        public List<string> Students { get; set; } = new List<string>();
    }

    public class CreateClassResponse
    {
        [JsonPropertyName("classId")]
        public string ClassId { get; set; } = string.Empty;

        [JsonPropertyName("className")]
        public string ClassName { get; set; } = string.Empty;

        [JsonPropertyName("teacherId")]
        public string TeacherId { get; set; } = string.Empty;

        [JsonPropertyName("createdStudents")]
        public List<StudentCredential> CreatedStudents { get; set; } = new List<StudentCredential>();

        [JsonPropertyName("failedStudents")]
        public List<FailedStudent> FailedStudents { get; set; } = new List<FailedStudent>();
    }

    public class StudentCredential
    {
        [JsonPropertyName("fullName")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;

        [JsonPropertyName("profileId")]
        public string ProfileId { get; set; } = string.Empty;
    }

    public class FailedStudent
    {
        [JsonPropertyName("fullName")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;
    }
}
