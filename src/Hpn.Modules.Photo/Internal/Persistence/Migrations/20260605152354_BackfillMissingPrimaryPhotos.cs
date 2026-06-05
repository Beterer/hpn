using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hpn.Modules.Photo.Internal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BackfillMissingPrimaryPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE photo.photos AS photo
                SET is_primary = TRUE
                WHERE photo.id IN (
                    SELECT DISTINCT ON (candidate.profile_id) candidate.id
                    FROM photo.photos AS candidate
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM photo.photos AS primary_photo
                        WHERE primary_photo.profile_id = candidate.profile_id
                          AND primary_photo.is_primary = TRUE
                    )
                    ORDER BY candidate.profile_id, candidate.position
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
