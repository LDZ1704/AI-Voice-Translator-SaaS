using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AI_Voice_Translator_SaaS.Migrations
{
    /// <inheritdoc />
    public partial class ClearAllRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM [Outputs]");
            migrationBuilder.Sql("DELETE FROM [Translations]");
            migrationBuilder.Sql("DELETE FROM [Transcripts]");
            migrationBuilder.Sql("DELETE FROM [AudioFiles]");
            migrationBuilder.Sql("DELETE FROM [AuditLogs]");

            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[Subscriptions]', N'U') IS NOT NULL
                DELETE FROM [Subscriptions]
            ");

            migrationBuilder.Sql("DELETE FROM [Users]");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('Users'))
                    DBCC CHECKIDENT('[Users]', RESEED, 0);
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('AudioFiles'))
                    DBCC CHECKIDENT('[AudioFiles]', RESEED, 0);
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('Transcripts'))
                    DBCC CHECKIDENT('[Transcripts]', RESEED, 0);
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('Translations'))
                    DBCC CHECKIDENT('[Translations]', RESEED, 0);
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('Outputs'))
                    DBCC CHECKIDENT('[Outputs]', RESEED, 0);
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('AuditLogs'))
                    DBCC CHECKIDENT('[AuditLogs]', RESEED, 0);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Cannot restore deleted data
        }
    }
}
