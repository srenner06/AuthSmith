using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthSmith.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddRequireEmailVerificationToApplication : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "RequireEmailVerification",
            table: "Applications",
            type: "boolean",
            nullable: false,
            defaultValue: true);  // Match entity default
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "RequireEmailVerification",
            table: "Applications");
    }
}
