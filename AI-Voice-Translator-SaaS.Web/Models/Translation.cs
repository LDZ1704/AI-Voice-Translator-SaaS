using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AI_Voice_Translator_SaaS.Models
{
    public class Translation
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TranscriptId { get; set; }

        [Required]
        [MaxLength(10)]
        public string TargetLanguage { get; set; }

        [Required]
        public string TranslatedText { get; set; }

        [Required]
        [MaxLength(50)]
        public string TranslationEngine { get; set; } = "Gemini";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Range(1, 5)]
        public int? UserRating { get; set; }

        // Navigation properties
        [ForeignKey(nameof(TranscriptId))]
        public virtual Transcript Transcript { get; set; }

        public virtual Output? Output { get; set; }
    }
}
