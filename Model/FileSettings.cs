using System;
namespace FileSystem_Honeywell.Model
{

    public class FileSettings
    {
        public long MaxFileSizeBytes { get; set; }
        public List<string> AllowedExtensions { get; set; } = new();
    }
}

