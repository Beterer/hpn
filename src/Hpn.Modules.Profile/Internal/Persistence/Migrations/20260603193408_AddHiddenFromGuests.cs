using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hpn.Modules.Profile.Internal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHiddenFromGuests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "hidden_from_guests",
                schema: "profile",
                table: "visibility_preferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "hidden_from_guests",
                schema: "profile",
                table: "visibility_preferences");
        }
    }
}
