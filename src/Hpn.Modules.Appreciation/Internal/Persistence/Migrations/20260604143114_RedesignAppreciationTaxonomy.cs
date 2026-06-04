using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Hpn.Modules.Appreciation.Internal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RedesignAppreciationTaxonomy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ADR-025: breaking taxonomy change with no data migration path. Clear the
            // event log and its category-keyed projections so the old categories can be
            // removed (the events FK would otherwise block the delete) and trait_id can
            // be added NOT NULL. Acceptable pre-launch.
            migrationBuilder.Sql(
                "TRUNCATE TABLE appreciation.appreciation_events, " +
                "appreciation.received_appreciation_stats, " +
                "appreciation.given_appreciation_stats;");

            migrationBuilder.DeleteData(
                schema: "appreciation",
                table: "appreciation_categories",
                keyColumn: "id",
                keyValue: new Guid("0f93fb39-2e34-4c90-bf9d-28df31447201"));

            migrationBuilder.DeleteData(
                schema: "appreciation",
                table: "appreciation_categories",
                keyColumn: "id",
                keyValue: new Guid("0f93fb39-2e34-4c90-bf9d-28df31447202"));

            migrationBuilder.DeleteData(
                schema: "appreciation",
                table: "appreciation_categories",
                keyColumn: "id",
                keyValue: new Guid("0f93fb39-2e34-4c90-bf9d-28df31447203"));

            migrationBuilder.DeleteData(
                schema: "appreciation",
                table: "appreciation_categories",
                keyColumn: "id",
                keyValue: new Guid("0f93fb39-2e34-4c90-bf9d-28df31447204"));

            migrationBuilder.DeleteData(
                schema: "appreciation",
                table: "appreciation_categories",
                keyColumn: "id",
                keyValue: new Guid("0f93fb39-2e34-4c90-bf9d-28df31447205"));

            migrationBuilder.DeleteData(
                schema: "appreciation",
                table: "appreciation_categories",
                keyColumn: "id",
                keyValue: new Guid("0f93fb39-2e34-4c90-bf9d-28df31447206"));

            migrationBuilder.DeleteData(
                schema: "appreciation",
                table: "appreciation_categories",
                keyColumn: "id",
                keyValue: new Guid("0f93fb39-2e34-4c90-bf9d-28df31447207"));

            migrationBuilder.DeleteData(
                schema: "appreciation",
                table: "appreciation_categories",
                keyColumn: "id",
                keyValue: new Guid("0f93fb39-2e34-4c90-bf9d-28df31447208"));

            migrationBuilder.DeleteData(
                schema: "appreciation",
                table: "appreciation_categories",
                keyColumn: "id",
                keyValue: new Guid("0f93fb39-2e34-4c90-bf9d-28df31447209"));

            migrationBuilder.DeleteData(
                schema: "appreciation",
                table: "appreciation_categories",
                keyColumn: "id",
                keyValue: new Guid("0f93fb39-2e34-4c90-bf9d-28df31447210"));

            migrationBuilder.DeleteData(
                schema: "appreciation",
                table: "appreciation_categories",
                keyColumn: "id",
                keyValue: new Guid("0f93fb39-2e34-4c90-bf9d-28df31447211"));

            migrationBuilder.DeleteData(
                schema: "appreciation",
                table: "appreciation_categories",
                keyColumn: "id",
                keyValue: new Guid("0f93fb39-2e34-4c90-bf9d-28df31447212"));

            migrationBuilder.AddColumn<Guid>(
                name: "trait_id",
                schema: "appreciation",
                table: "appreciation_events",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "hue",
                schema: "appreciation",
                table: "appreciation_categories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "appreciation_traits",
                schema: "appreciation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    label = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_appreciation_traits", x => x.id);
                    table.ForeignKey(
                        name: "fk_appreciation_traits_appreciation_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "appreciation",
                        principalTable: "appreciation_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "appreciation",
                table: "appreciation_categories",
                columns: new[] { "id", "active", "hue", "label", "slug", "sort_order" },
                values: new object[,]
                {
                    { new Guid("0f93fb39-2e34-4c90-bf9d-28df31447301"), true, 38, "Physical", "physical", 1 },
                    { new Guid("0f93fb39-2e34-4c90-bf9d-28df31447302"), true, 78, "Energy", "energy", 2 },
                    { new Guid("0f93fb39-2e34-4c90-bf9d-28df31447303"), true, 350, "Style", "style", 3 },
                    { new Guid("0f93fb39-2e34-4c90-bf9d-28df31447304"), true, 142, "Humor", "humor", 4 },
                    { new Guid("0f93fb39-2e34-4c90-bf9d-28df31447305"), true, 264, "Mind", "mind", 5 },
                    { new Guid("0f93fb39-2e34-4c90-bf9d-28df31447306"), true, 200, "Authentic", "authentic", 6 }
                });

            migrationBuilder.InsertData(
                schema: "appreciation",
                table: "appreciation_traits",
                columns: new[] { "id", "active", "category_id", "label", "slug", "sort_order" },
                values: new object[,]
                {
                    { new Guid("2a1d7f00-0000-4000-8000-000000000001"), true, new Guid("0f93fb39-2e34-4c90-bf9d-28df31447301"), "Warm smile", "warm_smile", 1 },
                    { new Guid("2a1d7f00-0000-4000-8000-000000000002"), true, new Guid("0f93fb39-2e34-4c90-bf9d-28df31447301"), "Kind eyes", "kind_eyes", 2 },
                    { new Guid("2a1d7f00-0000-4000-8000-000000000003"), true, new Guid("0f93fb39-2e34-4c90-bf9d-28df31447301"), "Great hair", "great_hair", 3 },
                    { new Guid("2a1d7f00-0000-4000-8000-000000000004"), true, new Guid("0f93fb39-2e34-4c90-bf9d-28df31447301"), "Natural glow", "natural_glow", 4 },
                    { new Guid("2a1d7f00-0000-4000-8000-000000000005"), true, new Guid("0f93fb39-2e34-4c90-bf9d-28df31447302"), "Good vibe", "good_vibe", 5 },
                    { new Guid("2a1d7f00-0000-4000-8000-000000000006"), true, new Guid("0f93fb39-2e34-4c90-bf9d-28df31447302"), "Confident", "confident", 6 },
                    { new Guid("2a1d7f00-0000-4000-8000-000000000007"), true, new Guid("0f93fb39-2e34-4c90-bf9d-28df31447302"), "Calm presence", "calm_presence", 7 },
                    { new Guid("2a1d7f00-0000-4000-8000-000000000008"), true, new Guid("0f93fb39-2e34-4c90-bf9d-28df31447302"), "Magnetic", "magnetic", 8 },
                    { new Guid("2a1d7f00-0000-4000-8000-000000000009"), true, new Guid("0f93fb39-2e34-4c90-bf9d-28df31447303"), "Great fit", "great_fit", 9 },
                    { new Guid("2a1d7f00-0000-4000-8000-000000000010"), true, new Guid("0f93fb39-2e34-4c90-bf9d-28df31447303"), "Effortless", "effortless", 10 },
                    { new Guid("2a1d7f00-0000-4000-8000-000000000011"), true, new Guid("0f93fb39-2e34-4c90-bf9d-28df31447303"), "Signature look", "signature_look", 11 },
                    { new Guid("2a1d7f00-0000-4000-8000-000000000012"), true, new Guid("0f93fb39-2e34-4c90-bf9d-28df31447304"), "Made me grin", "made_me_grin", 12 },
                    { new Guid("2a1d7f00-0000-4000-8000-000000000013"), true, new Guid("0f93fb39-2e34-4c90-bf9d-28df31447304"), "Quick wit", "quick_wit", 13 },
                    { new Guid("2a1d7f00-0000-4000-8000-000000000014"), true, new Guid("0f93fb39-2e34-4c90-bf9d-28df31447304"), "Wonderfully odd", "wonderfully_odd", 14 },
                    { new Guid("2a1d7f00-0000-4000-8000-000000000015"), true, new Guid("0f93fb39-2e34-4c90-bf9d-28df31447305"), "Curious", "curious", 15 },
                    { new Guid("2a1d7f00-0000-4000-8000-000000000016"), true, new Guid("0f93fb39-2e34-4c90-bf9d-28df31447305"), "Thoughtful", "thoughtful", 16 },
                    { new Guid("2a1d7f00-0000-4000-8000-000000000017"), true, new Guid("0f93fb39-2e34-4c90-bf9d-28df31447305"), "Sharp", "sharp", 17 },
                    { new Guid("2a1d7f00-0000-4000-8000-000000000018"), true, new Guid("0f93fb39-2e34-4c90-bf9d-28df31447306"), "Genuine", "genuine", 18 },
                    { new Guid("2a1d7f00-0000-4000-8000-000000000019"), true, new Guid("0f93fb39-2e34-4c90-bf9d-28df31447306"), "Grounded", "grounded", 19 },
                    { new Guid("2a1d7f00-0000-4000-8000-000000000020"), true, new Guid("0f93fb39-2e34-4c90-bf9d-28df31447306"), "True to themselves", "true_to_themselves", 20 }
                });

            migrationBuilder.CreateIndex(
                name: "ix_appreciation_events_trait_id",
                schema: "appreciation",
                table: "appreciation_events",
                column: "trait_id");

            migrationBuilder.CreateIndex(
                name: "ix_appreciation_traits_category_id",
                schema: "appreciation",
                table: "appreciation_traits",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_appreciation_traits_slug",
                schema: "appreciation",
                table: "appreciation_traits",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_appreciation_traits_sort_order",
                schema: "appreciation",
                table: "appreciation_traits",
                column: "sort_order",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_appreciation_events_appreciation_traits_trait_id",
                schema: "appreciation",
                table: "appreciation_events",
                column: "trait_id",
                principalSchema: "appreciation",
                principalTable: "appreciation_traits",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_appreciation_events_appreciation_traits_trait_id",
                schema: "appreciation",
                table: "appreciation_events");

            migrationBuilder.DropTable(
                name: "appreciation_traits",
                schema: "appreciation");

            migrationBuilder.DropIndex(
                name: "ix_appreciation_events_trait_id",
                schema: "appreciation",
                table: "appreciation_events");

            migrationBuilder.DeleteData(
                schema: "appreciation",
                table: "appreciation_categories",
                keyColumn: "id",
                keyValue: new Guid("0f93fb39-2e34-4c90-bf9d-28df31447301"));

            migrationBuilder.DeleteData(
                schema: "appreciation",
                table: "appreciation_categories",
                keyColumn: "id",
                keyValue: new Guid("0f93fb39-2e34-4c90-bf9d-28df31447302"));

            migrationBuilder.DeleteData(
                schema: "appreciation",
                table: "appreciation_categories",
                keyColumn: "id",
                keyValue: new Guid("0f93fb39-2e34-4c90-bf9d-28df31447303"));

            migrationBuilder.DeleteData(
                schema: "appreciation",
                table: "appreciation_categories",
                keyColumn: "id",
                keyValue: new Guid("0f93fb39-2e34-4c90-bf9d-28df31447304"));

            migrationBuilder.DeleteData(
                schema: "appreciation",
                table: "appreciation_categories",
                keyColumn: "id",
                keyValue: new Guid("0f93fb39-2e34-4c90-bf9d-28df31447305"));

            migrationBuilder.DeleteData(
                schema: "appreciation",
                table: "appreciation_categories",
                keyColumn: "id",
                keyValue: new Guid("0f93fb39-2e34-4c90-bf9d-28df31447306"));

            migrationBuilder.DropColumn(
                name: "trait_id",
                schema: "appreciation",
                table: "appreciation_events");

            migrationBuilder.DropColumn(
                name: "hue",
                schema: "appreciation",
                table: "appreciation_categories");

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
        }
    }
}
