using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIVoiceTranslator.Models
{
    public class Transcript
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid AudioFileId { get; set; }

        [Required]
        public string OriginalText { get; set; }

        [Required]
        [MaxLength(10)]
        public string DetectedLanguage { get; set; }

        [Range(0, 100)]
        public decimal? Confidence { get; set; }

        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(AudioFileId))]
        public virtual AudioFile AudioFile { get; set; }

        public virtual ICollection<Translation> Translations { get; set; } = new List<Translation>();
    }
}