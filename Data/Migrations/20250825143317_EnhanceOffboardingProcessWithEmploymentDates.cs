using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OffboardingChecklist.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceOffboardingProcessWithEmploymentDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClosedBy",
                table: "OffboardingProcesses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedOn",
                table: "OffboardingProcesses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmploymentStartDate",
                table: "OffboardingProcesses",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastWorkingDay",
                table: "OffboardingProcesses",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClosedBy",
                table: "OffboardingProcesses");

            migrationBuilder.DropColumn(
                name: "ClosedOn",
                table: "OffboardingProcesses");

            migrationBuilder.DropColumn(
                name: "EmploymentStartDate",
                table: "OffboardingProcesses");

            migrationBuilder.DropColumn(
                name: "LastWorkingDay",
                table: "OffboardingProcesses");
        }
    }
}
