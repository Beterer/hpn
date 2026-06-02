using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hpn.Modules.Profile.Internal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileLocationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "geo_lat",
                schema: "profile",
                table: "profiles",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "geo_lng",
                schema: "profile",
                table: "profiles",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "location_consent",
                schema: "profile",
                table: "profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "geo_lat",
                schema: "profile",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "geo_lng",
                schema: "profile",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "location_consent",
                schema: "profile",
                table: "profiles");
        }
    }
}
