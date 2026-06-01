using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Hpn.Modules.Appreciation.Internal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAppreciationSubmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                schema: "appreciation",
                table: "appreciation_events",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "appreciation_categories",
                schema: "appreciation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    label = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_appreciation_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "given_appreciation_stats",
                schema: "appreciation",
                columns: table => new
                {
                    sender_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_given_appreciation_stats", x => new { x.sender_user_id, x.category_id });
                    table.ForeignKey(
                        name: "fk_given_appreciation_stats_appreciation_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "appreciation",
                        principalTable: "appreciation_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "received_appreciation_stats",
                schema: "appreciation",
                columns: table => new
                {
                    receiver_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    count = table.Column<int>(type: "integer", nullable: false),
                    last_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_received_appreciation_stats", x => new { x.receiver_profile_id, x.category_id });
                    table.ForeignKey(
                        name: "fk_received_appreciation_stats_appreciation_categories_categor",
                        column: x => x.category_id,
                        principalSchema: "appreciation",
                        principalTable: "appreciation_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "appreciation",
                table: "appreciation_categories",
                columns: new[] { "id", "active", "label", "slug", "sort_order" },
                values: new object[,]
                {
                    { new Guid("0f93fb39-2e34-4c90-bf9d-28df31447201"), true, "Warm smile", "warm_smile", 1 },
                    { new Guid("0f93fb39-2e34-4c90-bf9d-28df31447202"), true, "Authentic", "authentic", 2 },
                    { new Guid("0f93fb39-2e34-4c90-bf9d-28df31447203"), true, "Stylish", "stylish", 3 },
                    { new Guid("0f93fb39-2e34-4c90-bf9d-28df31447204"), true, "Calming energy", "calming_energy", 4 },
                    { new Guid("0f93fb39-2e34-4c90-bf9d-28df31447205"), true, "Confident", "confident", 5 },
                    { new Guid("0f93fb39-2e34-4c90-bf9d-28df31447206"), true, "Expressive", "expressive", 6 },
                    { new Guid("0f93fb39-2e34-4c90-bf9d-28df31447207"), true, "Fun energy", "fun_energy", 7 },
                    { new Guid("0f93fb39-2e34-4c90-bf9d-28df31447208"), true, "Elegant", "elegant", 8 },
                    { new Guid("0f93fb39-2e34-4c90-bf9d-28df31447209"), true, "Trustworthy", "trustworthy", 9 },
                    { new Guid("0f93fb39-2e34-4c90-bf9d-28df31447210"), true, "Creative", "creative", 10 },
                    { new Guid("0f93fb39-2e34-4c90-bf9d-28df31447211"), true, "Kind", "kind", 11 },
                    { new Guid("0f93fb39-2e34-4c90-bf9d-28df31447212"), true, "Intelligent-looking", "intelligent", 12 }
                });

            migrationBuilder.Sql(
                """
                UPDATE appreciation.appreciation_events
                SET idempotency_key = 'legacy-' || id::text
                WHERE idempotency_key IS NULL
                """);

            migrationBuilder.Sql(
                """
                UPDATE appreciation.appreciation_events
                SET category_id = '0f93fb39-2e34-4c90-bf9d-28df31447201'
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM appreciation.appreciation_categories c
                    WHERE c.id = appreciation.appreciation_events.category_id
                )
                """);

            migrationBuilder.AlterColumn<string>(
                name: "idempotency_key",
                schema: "appreciation",
                table: "appreciation_events",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_appreciation_events_category_id",
                schema: "appreciation",
                table: "appreciation_events",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_appreciation_events_sender_user_id_idempotency_key",
                schema: "appreciation",
                table: "appreciation_events",
                columns: new[] { "sender_user_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_appreciation_categories_slug",
                schema: "appreciation",
                table: "appreciation_categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_appreciation_categories_sort_order",
                schema: "appreciation",
                table: "appreciation_categories",
                column: "sort_order",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_given_appreciation_stats_category_id",
                schema: "appreciation",
                table: "given_appreciation_stats",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_received_appreciation_stats_category_id",
                schema: "appreciation",
                table: "received_appreciation_stats",
                column: "category_id");

            migrationBuilder.AddForeignKey(
                name: "fk_appreciation_events_appreciation_categories_category_id",
                schema: "appreciation",
                table: "appreciation_events",
                column: "category_id",
                principalSchema: "appreciation",
                principalTable: "appreciation_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_appreciation_events_appreciation_categories_category_id",
                schema: "appreciation",
                table: "appreciation_events");

            migrationBuilder.DropTable(
                name: "given_appreciation_stats",
                schema: "appreciation");

            migrationBuilder.DropTable(
                name: "received_appreciation_stats",
                schema: "appreciation");

            migrationBuilder.DropTable(
                name: "appreciation_categories",
                schema: "appreciation");

            migrationBuilder.DropIndex(
                name: "ix_appreciation_events_category_id",
                schema: "appreciation",
                table: "appreciation_events");

            migrationBuilder.DropIndex(
                name: "ix_appreciation_events_sender_user_id_idempotency_key",
                schema: "appreciation",
                table: "appreciation_events");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                schema: "appreciation",
                table: "appreciation_events");
        }
    }
}
