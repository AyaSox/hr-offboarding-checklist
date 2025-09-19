using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OffboardingChecklist.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnhancedTaskFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OffboardingDocuments_FileType",
                table: "OffboardingDocuments");

            migrationBuilder.DropIndex(
                name: "IX_ChecklistItems_Department",
                table: "ChecklistItems");

            migrationBuilder.AlterColumn<string>(
                name: "JobTitle",
                table: "OffboardingProcesses",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "EmployeeName",
                table: "OffboardingProcesses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "FileType",
                table: "OffboardingDocuments",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Department",
                table: "ChecklistItems",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "ChecklistItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "ChecklistItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "TaskComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChecklistItemId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskComments_ChecklistItems_ChecklistItemId",
                        column: x => x.ChecklistItemId,
                        principalTable: "ChecklistItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistItems_DueDate",
                table: "ChecklistItems",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistItems_Priority",
                table: "ChecklistItems",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_TaskComments_ChecklistItemId",
                table: "TaskComments",
                column: "ChecklistItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskComments_CreatedOn",
                table: "TaskComments",
                column: "CreatedOn");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskComments");

            migrationBuilder.DropIndex(
                name: "IX_ChecklistItems_DueDate",
                table: "ChecklistItems");

            migrationBuilder.DropIndex(
                name: "IX_ChecklistItems_Priority",
                table: "ChecklistItems");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "ChecklistItems");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "ChecklistItems");

            migrationBuilder.AlterColumn<string>(
                name: "JobTitle",
                table: "OffboardingProcesses",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "EmployeeName",
                table: "OffboardingProcesses",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "FileType",
                table: "OffboardingDocuments",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Department",
                table: "ChecklistItems",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_OffboardingDocuments_FileType",
                table: "OffboardingDocuments",
                column: "FileType");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistItems_Department",
                table: "ChecklistItems",
                column: "Department");
        }
    }
}
