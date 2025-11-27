using System;
namespace FileSystem_Honeywell.Model
{
    public class FileRecord
    {
        public int Id { get; set; }

        public string UserId { get; set; } = default!; 

        public string OriginalFileName { get; set; } = default!;
        public string StoredFileName { get; set; } = default!;
        public string ContentType { get; set; } = default!;
        public long Size { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}

