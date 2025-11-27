using AIVoiceTranslator.Models;
using Microsoft.EntityFrameworkCore;

namespace AIVoiceTranslator.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<AudioFile> AudioFiles { get; set; }
        public DbSet<Transcript> Transcripts { get; set; }
        public DbSet<Translation> Translations { get; set; }
        public DbSet<Output> Outputs { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // AudioFile
            modelBuilder.Entity<AudioFile>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.User)
                    .WithMany(u => u.AudioFiles)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Transcript
            modelBuilder.Entity<Transcript>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.AudioFile)
                    .WithOne(a => a.Transcript)
                    .HasForeignKey<Transcript>(e => e.AudioFileId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Translation
            modelBuilder.Entity<Translation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Transcript)
                    .WithMany(t => t.Translations)
                    .HasForeignKey(e => e.TranscriptId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Output
            modelBuilder.Entity<Output>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Translation)
                    .WithOne(t => t.Output)
                    .HasForeignKey<Output>(e => e.TranslationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // AuditLog
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.User)
                    .WithMany(u => u.AuditLogs)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}