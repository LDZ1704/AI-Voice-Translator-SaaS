using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIVoiceTranslator.Models
{
    public class AudioFile
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; }

        [Required]
        [MaxLength(500)]
        public string OriginalFileUrl { get; set; }

        [Required]
        public long FileSizeBytes { get; set; }

        public int? DurationSeconds { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; }

        public virtual Transcript? Transcript { get; set; }
    }
}