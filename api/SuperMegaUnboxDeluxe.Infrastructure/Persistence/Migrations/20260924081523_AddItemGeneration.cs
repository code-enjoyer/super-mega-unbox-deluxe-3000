using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SuperMegaUnboxDeluxe.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddItemGeneration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Details",
                table: "item_modifiers",
                type: "jsonb",
                nullable: true);

            migrationBuilder.InsertData(
                table: "item_bases",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { new Guid("fa000000-0000-0000-0000-000000000001"), "Finger Guns" },
                    { new Guid("fa000000-0000-0000-0000-000000000002"), "Petrified Salamander" },
                    { new Guid("fa000000-0000-0000-0000-000000000003"), "Turtle-Shell Shield" },
                    { new Guid("fa000000-0000-0000-0000-000000000004"), "Suspicious Trench Coat" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "item_bases",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "item_bases",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "item_bases",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "item_bases",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000004"));

            migrationBuilder.DropColumn(
                name: "Details",
                table: "item_modifiers");
        }
    }
}
