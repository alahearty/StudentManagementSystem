using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddResultComputation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CaScore",
                table: "Enrollments",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExamScore",
                table: "Enrollments",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsResultPublished",
                table: "Enrollments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResultPublishedAt",
                table: "Enrollments",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CaScore",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "ExamScore",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "IsResultPublished",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "ResultPublishedAt",
                table: "Enrollments");
        }
    }
}
