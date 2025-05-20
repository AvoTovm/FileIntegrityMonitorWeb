using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FileIntegrityMonitor.Models
{
    public class FileRecord
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        public string Sha256Hash { get; set; } = string.Empty;

        [Required]
        public DateTime LastModified { get; set; }

        public virtual ICollection<FileChangeLog> ChangeLogs { get; set; } = new List<FileChangeLog>();
    }
}