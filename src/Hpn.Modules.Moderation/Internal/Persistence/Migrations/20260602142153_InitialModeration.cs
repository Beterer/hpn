using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hpn.Modules.Moderation.Internal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialModeration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "moderation");

            migrationBuilder.CreateTable(
                name: "account_trust",
                schema: "moderation",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    score = table.Column<double>(type: "numeric(4,3)", nullable: false),
                    signals = table.Column<string>(type: "jsonb", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_trust", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "moderation_actions",
                schema: "moderation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    actor = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_moderation_actions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reports",
                schema: "moderation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reporter_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reports", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_moderation_actions_target_user_id_created_at",
                schema: "moderation",
                table: "moderation_actions",
                columns: new[] { "target_user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_reports_reporter_user_id_target_profile_id_type",
                schema: "moderation",
                table: "reports",
                columns: new[] { "reporter_user_id", "target_profile_id", "type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reports_target_profile_id_created_at",
                schema: "moderation",
                table: "reports",
                columns: new[] { "target_profile_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_reports_target_profile_id_status",
                schema: "moderation",
                table: "reports",
                columns: new[] { "target_profile_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_trust",
                schema: "moderation");

            migrationBuilder.DropTable(
                name: "moderation_actions",
                schema: "moderation");

            migrationBuilder.DropTable(
                name: "reports",
                schema: "moderation");
        }
    }
}
