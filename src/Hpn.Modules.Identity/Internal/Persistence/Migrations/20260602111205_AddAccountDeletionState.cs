using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hpn.Modules.Identity.Internal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountDeletionState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deletion_requested_at",
                schema: "identity",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "purge_after",
                schema: "identity",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "deletion_requested_at",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "purge_after",
                schema: "identity",
                table: "users");
        }
    }
}
