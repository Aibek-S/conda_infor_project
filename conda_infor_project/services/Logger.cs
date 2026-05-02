using System;
using System.Diagnostics;

namespace conda_infor_project.services
{
    public static class Logger
    {
        public static void LogInfo(string message)
        {
            Log("INFO", message);
        }

        public static void LogError(string message, Exception ex = null)
        {
            string details = ex != null ? $"\n    Exception: {ex.GetType().Name}\n    Message: {ex.Message}\n    StackTrace: {ex.StackTrace}" : "";
            Log("ERROR", message + details);
        }

        public static void LogWarning(string message)
        {
            Log("WARNING", message);
        }

        private static void Log(string level, string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string logEntry = $"[{timestamp}] [{level}] {message}";

            Debug.WriteLine(logEntry);
            Console.WriteLine(logEntry);
        }
    }
}
