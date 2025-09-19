using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OffboardingChecklist.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvancedFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Department",
                table: "ChecklistItems",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "OffboardingDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OffboardingProcessId = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OffboardingDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OffboardingDocuments_OffboardingProcesses_OffboardingProcessId",
                        column: x => x.OffboardingProcessId,
                        principalTable: "OffboardingProcesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OffboardingProcesses_IsClosed",
                table: "OffboardingProcesses",
                column: "IsClosed");

            migrationBuilder.CreateIndex(
                name: "IX_OffboardingProcesses_StartDate",
                table: "OffboardingProcesses",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistItems_Department",
                table: "ChecklistItems",
                column: "Department");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistItems_IsCompleted",
                table: "ChecklistItems",
                column: "IsCompleted");

            migrationBuilder.CreateIndex(
                name: "IX_OffboardingDocuments_FileType",
                table: "OffboardingDocuments",
                column: "FileType");

            migrationBuilder.CreateIndex(
                name: "IX_OffboardingDocuments_OffboardingProcessId",
                table: "OffboardingDocuments",
                column: "OffboardingProcessId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OffboardingDocuments");

            migrationBuilder.DropIndex(
                name: "IX_OffboardingProcesses_IsClosed",
                table: "OffboardingProcesses");

            migrationBuilder.DropIndex(
                name: "IX_OffboardingProcesses_StartDate",
                table: "OffboardingProcesses");

            migrationBuilder.DropIndex(
                name: "IX_ChecklistItems_Department",
                table: "ChecklistItems");

            migrationBuilder.DropIndex(
                name: "IX_ChecklistItems_IsCompleted",
                table: "ChecklistItems");

            migrationBuilder.AlterColumn<string>(
                name: "Department",
                table: "ChecklistItems",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
