using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hpn.Modules.Photo.Internal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeparatePrimaryPhotoFromOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_primary",
                schema: "photo",
                table: "photos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE photo.photos
                SET is_primary = TRUE
                WHERE position = 0;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_photos_profile_id_primary",
                schema: "photo",
                table: "photos",
                column: "profile_id",
                unique: true,
                filter: "is_primary");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_photos_profile_id_primary",
                schema: "photo",
                table: "photos");

            migrationBuilder.DropColumn(
                name: "is_primary",
                schema: "photo",
                table: "photos");
        }
    }
}
