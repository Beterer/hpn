using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hpn.Modules.Admin.Internal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "admin");

            migrationBuilder.CreateTable(
                name: "admin_audit_log",
                schema: "admin",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    admin_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    target_ref = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admin_audit_log", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_admin_audit_log_admin_user_id_created_at",
                schema: "admin",
                table: "admin_audit_log",
                columns: new[] { "admin_user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_admin_audit_log_target_ref_created_at",
                schema: "admin",
                table: "admin_audit_log",
                columns: new[] { "target_ref", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_audit_log",
                schema: "admin");
        }
    }
}
