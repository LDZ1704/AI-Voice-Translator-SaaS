using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AI_Voice_Translator_SaaS.Models
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
        public string DetectedLanguage { get; set; } // en, vi, ja, zh, fr

        [Range(0, 100)]
        public decimal? Confidence { get; set; }

        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(AudioFileId))]
        public virtual AudioFile AudioFile { get; set; }

        public virtual ICollection<Translation> Translations { get; set; } = new List<Translation>();
    }
}
