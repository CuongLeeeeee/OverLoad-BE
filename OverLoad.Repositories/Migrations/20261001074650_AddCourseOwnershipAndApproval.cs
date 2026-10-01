using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OverLoad.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseOwnershipAndApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: add new columns (with safe defaults)
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Courses",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Draft");

            migrationBuilder.AddColumn<int>(
                name: "InstructorId",
                table: "Courses",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Courses",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            // Step 2: migrate existing data before dropping the old column
            migrationBuilder.Sql(
                """UPDATE "Courses" SET "Status" = CASE WHEN "IsPublished" THEN 'Published' ELSE 'Draft' END""");

            // Step 3: drop the old column
            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Courses");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_InstructorId",
                table: "Courses",
                column: "InstructorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Users_InstructorId",
                table: "Courses",
                column: "InstructorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Users_InstructorId",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Courses_InstructorId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "InstructorId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Courses");

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Courses",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
