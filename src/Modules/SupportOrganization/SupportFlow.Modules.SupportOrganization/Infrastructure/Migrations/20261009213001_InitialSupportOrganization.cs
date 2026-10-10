using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportFlow.Modules.SupportOrganization.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSupportOrganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "organization");

            migrationBuilder.CreateTable(
                name: "teams",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_teams", x => x.id);
                });

            migrationBuilder.InsertData(
                schema: "organization",
                table: "teams",
                columns: new[] { "id", "is_default", "name" },
                values: new object[] { new Guid("0199c6a0-0000-7000-8000-000000000001"), true, "Default" });

            migrationBuilder.CreateIndex(
                name: "ix_teams_is_default",
                schema: "organization",
                table: "teams",
                column: "is_default",
                unique: true,
                filter: "is_default");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "teams",
                schema: "organization");
        }
    }
}
