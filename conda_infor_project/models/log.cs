using System;
using System.Collections.Generic;
using System.Text;

namespace conda_infor_project.models
{
    public class Log
    {
        public int Id { get; set; }
        public string StudentId { get; set; }
        public User User { get; set; }
        public string ActiveWindow { get; set; }
        public List<string> ProcessList { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
