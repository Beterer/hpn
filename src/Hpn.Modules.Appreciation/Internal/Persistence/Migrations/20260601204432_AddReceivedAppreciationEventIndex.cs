using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hpn.Modules.Appreciation.Internal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReceivedAppreciationEventIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_appreciation_events_receiver_profile_id",
                schema: "appreciation",
                table: "appreciation_events");

            migrationBuilder.CreateIndex(
                name: "ix_appreciation_events_receiver_profile_id_created_at",
                schema: "appreciation",
                table: "appreciation_events",
                columns: new[] { "receiver_profile_id", "created_at" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_appreciation_events_receiver_profile_id_created_at",
                schema: "appreciation",
                table: "appreciation_events");

            migrationBuilder.CreateIndex(
                name: "ix_appreciation_events_receiver_profile_id",
                schema: "appreciation",
                table: "appreciation_events",
                column: "receiver_profile_id");
        }
    }
}
