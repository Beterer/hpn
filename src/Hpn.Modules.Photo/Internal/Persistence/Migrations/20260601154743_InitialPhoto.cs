using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hpn.Modules.Photo.Internal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPhoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "photo");

            migrationBuilder.CreateTable(
                name: "photos",
                schema: "photo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<short>(type: "smallint", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    original_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    display_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    thumb_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    width = table.Column<int>(type: "integer", nullable: false),
                    height = table.Column<int>(type: "integer", nullable: false),
                    content_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    scan_result = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_photos", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_photos_content_hash",
                schema: "photo",
                table: "photos",
                column: "content_hash");

            migrationBuilder.CreateIndex(
                name: "ix_photos_profile_id_position",
                schema: "photo",
                table: "photos",
                columns: new[] { "profile_id", "position" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "photos",
                schema: "photo");
        }
    }
}
