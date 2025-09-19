using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OffboardingChecklist.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveChecklistItemPriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChecklistItems_Priority",
                table: "ChecklistItems");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "ChecklistItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "ChecklistItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistItems_Priority",
                table: "ChecklistItems",
                column: "Priority");
        }
    }
}
