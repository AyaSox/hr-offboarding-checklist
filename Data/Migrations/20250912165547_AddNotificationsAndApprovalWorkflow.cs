using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OffboardingChecklist.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationsAndApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                table: "OffboardingProcesses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedOn",
                table: "OffboardingProcesses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectedBy",
                table: "OffboardingProcesses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedOn",
                table: "OffboardingProcesses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "OffboardingProcesses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "OffboardingProcesses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    RecipientUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RecipientEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActionUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActionText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RelatedProcessId = table.Column<int>(type: "int", nullable: true),
                    RelatedTaskId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OffboardingProcesses_Status",
                table: "OffboardingProcesses",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedOn",
                table: "Notifications",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IsRead",
                table: "Notifications",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientUserId",
                table: "Notifications",
                column: "RecipientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Type",
                table: "Notifications",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_OffboardingProcesses_Status",
                table: "OffboardingProcesses");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "OffboardingProcesses");

            migrationBuilder.DropColumn(
                name: "ApprovedOn",
                table: "OffboardingProcesses");

            migrationBuilder.DropColumn(
                name: "RejectedBy",
                table: "OffboardingProcesses");

            migrationBuilder.DropColumn(
                name: "RejectedOn",
                table: "OffboardingProcesses");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "OffboardingProcesses");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "OffboardingProcesses");
        }
    }
}
