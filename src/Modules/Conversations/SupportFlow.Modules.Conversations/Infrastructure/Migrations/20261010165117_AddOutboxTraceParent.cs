using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportFlow.Modules.Conversations.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxTraceParent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "trace_parent",
                schema: "conversations",
                table: "outbox",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_failed_at",
                schema: "conversations",
                table: "outbox",
                column: "failed_at",
                filter: "failed_at IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_outbox_failed_at",
                schema: "conversations",
                table: "outbox");

            migrationBuilder.DropColumn(
                name: "trace_parent",
                schema: "conversations",
                table: "outbox");
        }
    }
}
