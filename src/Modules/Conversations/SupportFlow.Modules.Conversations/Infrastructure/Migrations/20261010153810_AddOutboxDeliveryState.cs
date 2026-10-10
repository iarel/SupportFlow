using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportFlow.Modules.Conversations.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxDeliveryState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_outbox_created_at",
                schema: "conversations",
                table: "outbox");

            migrationBuilder.AddColumn<int>(
                name: "attempts",
                schema: "conversations",
                table: "outbox",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "failed_at",
                schema: "conversations",
                table: "outbox",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_error",
                schema: "conversations",
                table: "outbox",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "locked_until",
                schema: "conversations",
                table: "outbox",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "next_attempt_at",
                schema: "conversations",
                table: "outbox",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "ix_outbox_next_attempt_at",
                schema: "conversations",
                table: "outbox",
                column: "next_attempt_at",
                filter: "processed_at IS NULL AND failed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_processed_at",
                schema: "conversations",
                table: "outbox",
                column: "processed_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_outbox_next_attempt_at",
                schema: "conversations",
                table: "outbox");

            migrationBuilder.DropIndex(
                name: "ix_outbox_processed_at",
                schema: "conversations",
                table: "outbox");

            migrationBuilder.DropColumn(
                name: "attempts",
                schema: "conversations",
                table: "outbox");

            migrationBuilder.DropColumn(
                name: "failed_at",
                schema: "conversations",
                table: "outbox");

            migrationBuilder.DropColumn(
                name: "last_error",
                schema: "conversations",
                table: "outbox");

            migrationBuilder.DropColumn(
                name: "locked_until",
                schema: "conversations",
                table: "outbox");

            migrationBuilder.DropColumn(
                name: "next_attempt_at",
                schema: "conversations",
                table: "outbox");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_created_at",
                schema: "conversations",
                table: "outbox",
                column: "created_at");
        }
    }
}
