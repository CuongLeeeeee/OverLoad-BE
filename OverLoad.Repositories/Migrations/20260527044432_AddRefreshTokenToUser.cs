using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OverLoad.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenToUser : Migration
    {
        /// <inheritdoc />
      protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<string>(
        name: "RefreshToken",
        table: "Users",
        type: "nvarchar(500)",
        maxLength: 500,
        nullable: true);

    migrationBuilder.AddColumn<DateTime>(
        name: "RefreshTokenExpiresAt",
        table: "Users",
        type: "datetime2",
        nullable: true);
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropColumn(
        name: "RefreshToken",
        table: "Users");

    migrationBuilder.DropColumn(
        name: "RefreshTokenExpiresAt",
        table: "Users");
}
    }
}
