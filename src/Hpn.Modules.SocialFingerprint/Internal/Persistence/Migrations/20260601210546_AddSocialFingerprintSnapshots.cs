using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hpn.Modules.SocialFingerprint.Internal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSocialFingerprintSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "social_fingerprint");

            migrationBuilder.CreateTable(
                name: "social_fingerprint_snapshots",
                schema: "social_fingerprint",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    sample_size = table.Column<int>(type: "integer", nullable: false),
                    distribution = table.Column<string>(type: "jsonb", nullable: false),
                    top_traits = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_social_fingerprint_snapshots", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_social_fingerprint_snapshots_profile_id_period_period_start",
                schema: "social_fingerprint",
                table: "social_fingerprint_snapshots",
                columns: new[] { "profile_id", "period", "period_start" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_social_fingerprint_snapshots_profile_id_period_start",
                schema: "social_fingerprint",
                table: "social_fingerprint_snapshots",
                columns: new[] { "profile_id", "period_start" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "social_fingerprint_snapshots",
                schema: "social_fingerprint");
        }
    }
}
