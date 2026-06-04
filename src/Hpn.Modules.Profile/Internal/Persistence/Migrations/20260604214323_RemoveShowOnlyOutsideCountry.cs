using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hpn.Modules.Profile.Internal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveShowOnlyOutsideCountry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "show_only_outside_country",
                schema: "profile",
                table: "visibility_preferences");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "show_only_outside_country",
                schema: "profile",
                table: "visibility_preferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
