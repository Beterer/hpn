using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hpn.Modules.Appreciation.Internal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialAppreciation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "appreciation");

            migrationBuilder.CreateTable(
                name: "appreciation_events",
                schema: "appreciation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receiver_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    photo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_appreciation_events", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_appreciation_events_receiver_profile_id",
                schema: "appreciation",
                table: "appreciation_events",
                column: "receiver_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_appreciation_events_sender_user_id_created_at",
                schema: "appreciation",
                table: "appreciation_events",
                columns: new[] { "sender_user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_appreciation_events_sender_user_id_receiver_profile_id_cate",
                schema: "appreciation",
                table: "appreciation_events",
                columns: new[] { "sender_user_id", "receiver_profile_id", "category_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "appreciation_events",
                schema: "appreciation");
        }
    }
}
