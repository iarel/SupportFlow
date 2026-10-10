using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportFlow.Modules.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.CreateTable(
                name: "user_accounts",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_issuer = table.Column<string>(type: "text", nullable: false),
                    external_subject = table.Column<string>(type: "text", nullable: false),
                    account_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_accounts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_accounts_external_issuer_external_subject",
                schema: "identity",
                table: "user_accounts",
                columns: new[] { "external_issuer", "external_subject" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_accounts",
                schema: "identity");
        }
    }
}
