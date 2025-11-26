using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AI_Voice_Translator_SaaS.Models
{
    public class Output
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TranslationId { get; set; }

        [Required]
        [MaxLength(500)]
        public string OutputFileUrl { get; set; }

        [Required]
        [MaxLength(20)]
        public string VoiceType { get; set; } = "Female"; // Male, Female

        [Required]
        [MaxLength(50)]
        public string VoiceModel { get; set; } = "Google"; // Google, OpenAI

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        public int DownloadCount { get; set; } = 0;

        [Required]
        public DateTime ExpiryDate { get; set; }

        // Navigation properties
        [ForeignKey(nameof(TranslationId))]
        public virtual Translation Translation { get; set; }
    }
}
