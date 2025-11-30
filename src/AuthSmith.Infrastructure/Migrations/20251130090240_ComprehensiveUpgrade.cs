using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthSmith.Infrastructure.Migrations;

/// <inheritdoc />
public partial class ComprehensiveUpgrade : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "EmailVerified",
            table: "Users",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTime>(
            name: "EmailVerifiedAt",
            table: "Users",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DeviceInfo",
            table: "RefreshTokens",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "IpAddress",
            table: "RefreshTokens",
            type: "text",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "AuditLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                EventType = table.Column<int>(type: "integer", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: true),
                ApplicationId = table.Column<Guid>(type: "uuid", nullable: true),
                UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                ApplicationKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                Details = table.Column<string>(type: "jsonb", nullable: true),
                Success = table.Column<bool>(type: "boolean", nullable: false),
                ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditLogs", x => x.Id);
                table.ForeignKey(
                    name: "FK_AuditLogs_Applications_ApplicationId",
                    column: x => x.ApplicationId,
                    principalTable: "Applications",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_AuditLogs_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "EmailVerificationTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Token = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                IsUsed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EmailVerificationTokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_EmailVerificationTokens_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "PasswordResetTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Token = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                IsUsed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_PasswordResetTokens_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_ApplicationId",
            table: "AuditLogs",
            column: "ApplicationId");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_CreatedAt",
            table: "AuditLogs",
            column: "CreatedAt");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_EventType",
            table: "AuditLogs",
            column: "EventType");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_EventType_CreatedAt",
            table: "AuditLogs",
            columns: ["EventType", "CreatedAt"]);

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_UserId",
            table: "AuditLogs",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_UserId_CreatedAt",
            table: "AuditLogs",
            columns: ["UserId", "CreatedAt"]);

        migrationBuilder.CreateIndex(
            name: "IX_EmailVerificationTokens_ExpiresAt",
            table: "EmailVerificationTokens",
            column: "ExpiresAt");

        migrationBuilder.CreateIndex(
            name: "IX_EmailVerificationTokens_Token",
            table: "EmailVerificationTokens",
            column: "Token",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_EmailVerificationTokens_UserId",
            table: "EmailVerificationTokens",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_PasswordResetTokens_ExpiresAt",
            table: "PasswordResetTokens",
            column: "ExpiresAt");

        migrationBuilder.CreateIndex(
            name: "IX_PasswordResetTokens_Token",
            table: "PasswordResetTokens",
            column: "Token",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PasswordResetTokens_UserId",
            table: "PasswordResetTokens",
            column: "UserId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AuditLogs");

        migrationBuilder.DropTable(
            name: "EmailVerificationTokens");

        migrationBuilder.DropTable(
            name: "PasswordResetTokens");

        migrationBuilder.DropColumn(
            name: "EmailVerified",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "EmailVerifiedAt",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "DeviceInfo",
            table: "RefreshTokens");

        migrationBuilder.DropColumn(
            name: "IpAddress",
            table: "RefreshTokens");
    }
}
