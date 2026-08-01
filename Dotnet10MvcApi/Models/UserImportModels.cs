using System;
using System.Collections.Generic;

namespace Dotnet10MvcApi.Models
{
    public class UserImportRowDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UserImportResultDto
    {
        public bool Success { get; set; }
        public int TotalProcessed { get; set; }
        public int CreatedCount { get; set; }
        public int SkippedCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public string Message { get; set; } = string.Empty;
    }
}
