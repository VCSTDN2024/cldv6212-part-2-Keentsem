// Create this file as Models/BlobInfo.cs

using System;

namespace CLDV6212POE.Models
{
    public class BlobInfo
    {
        public string Name { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime? LastModified { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string ETag { get; set; } = string.Empty;

        // Formatted properties for display
        public string FormattedSize
        {
            get
            {
                string[] sizes = { "bytes", "KB", "MB", "GB" };
                double len = Size;
                int order = 0;
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len /= 1024;
                }
                return $"{len:0.##} {sizes[order]}";
            }
        }

        public string FormattedDate
        {
            get
            {
                return LastModified?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Unknown";
            }
        }
    }
}