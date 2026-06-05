using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hpn.Modules.Profile.Internal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropBio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bio",
                schema: "profile",
                table: "profiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "bio",
                schema: "profile",
                table: "profiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
