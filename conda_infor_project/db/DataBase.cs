using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;

namespace conda_infor_project.db
{
    public static class DataBase
    {
        public static readonly string Url = "https://eqyuifxlyeolgonuaylb.supabase.co";
        public static readonly string ApiKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImVxeXVpZnhseWVvbGdvbnVheWxiIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzU3OTc5MDAsImV4cCI6MjA5MTM3MzkwMH0.3FOCWBZCah3q8uVJqJElNFK1M9btO3sF1f2KvsZjpQM";

        private static readonly HttpClient client = new HttpClient()
        {
            BaseAddress = new Uri(Url),
        };
        static DataBase()
        {
            client.DefaultRequestHeaders.Add("apikey", ApiKey);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
        }
        public static HttpClient GetClient()
        {
            return client;
        }
    }
}
