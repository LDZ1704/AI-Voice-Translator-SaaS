using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AI_Voice_Translator_SaaS.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Index for AudioFiles queries
            migrationBuilder.CreateIndex(
                name: "IX_AudioFiles_UserId_Status",
                table: "AudioFiles",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AudioFiles_UploadedAt",
                table: "AudioFiles",
                column: "UploadedAt");

            // Index for Users queries
            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                table: "Users",
                column: "IsActive");

            // Index for AuditLogs queries
            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId_Timestamp",
                table: "AuditLogs",
                columns: new[] { "UserId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Action",
                table: "AuditLogs",
                column: "Action");

            // Index for Translations
            migrationBuilder.CreateIndex(
                name: "IX_Translations_TranscriptId",
                table: "Translations",
                column: "TranscriptId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_AudioFiles_UserId_Status", table: "AudioFiles");
            migrationBuilder.DropIndex(name: "IX_AudioFiles_UploadedAt", table: "AudioFiles");
            migrationBuilder.DropIndex(name: "IX_Users_Email", table: "Users");
            migrationBuilder.DropIndex(name: "IX_Users_IsActive", table: "Users");
            migrationBuilder.DropIndex(name: "IX_AuditLogs_UserId_Timestamp", table: "AuditLogs");
            migrationBuilder.DropIndex(name: "IX_AuditLogs_Action", table: "AuditLogs");
            migrationBuilder.DropIndex(name: "IX_Translations_TranscriptId", table: "Translations");
        }
    }
}
