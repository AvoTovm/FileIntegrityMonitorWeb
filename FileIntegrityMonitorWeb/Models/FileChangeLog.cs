using System;
using System.ComponentModel.DataAnnotations;

namespace FileIntegrityMonitor.Models
{
    public class FileChangeLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FileRecordId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public DateTime ChangeTimestamp { get; set; }

        [Required]
        public string PreviousHash { get; set; } = string.Empty;

        [Required]
        public string NewHash { get; set; } = string.Empty;

        [Required]
        public ChangeType ChangeType { get; set; }

        public virtual FileRecord FileRecord { get; set; } = null!;
        public virtual ApplicationUser User { get; set; } = null!;
    }

    public enum ChangeType
    {
        New,
        Modified,
        Deleted
    }
}