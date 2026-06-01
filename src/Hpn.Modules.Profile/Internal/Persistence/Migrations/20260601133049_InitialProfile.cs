using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Hpn.Modules.Profile.Internal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "profile");

            migrationBuilder.CreateTable(
                name: "interests",
                schema: "profile",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    label = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_interests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "profiles",
                schema: "profile",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    gender = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    self_describe_text = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    country_code = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: true),
                    bio = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_blocks",
                schema: "profile",
                columns: table => new
                {
                    blocker_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    blocked_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_blocks", x => new { x.blocker_user_id, x.blocked_user_id });
                });

            migrationBuilder.CreateTable(
                name: "profile_interests",
                schema: "profile",
                columns: table => new
                {
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    interest_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_profile_interests", x => new { x.profile_id, x.interest_id });
                    table.ForeignKey(
                        name: "fk_profile_interests_interests_interest_id",
                        column: x => x.interest_id,
                        principalSchema: "profile",
                        principalTable: "interests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_profile_interests_profiles_profile_id",
                        column: x => x.profile_id,
                        principalSchema: "profile",
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "visibility_preferences",
                schema: "profile",
                columns: table => new
                {
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    show_only_outside_country = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    hide_from_country = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    min_distance_km = table.Column<int>(type: "integer", nullable: true),
                    women_for_women = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    verified_only = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    paused = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_visibility_preferences", x => x.profile_id);
                    table.ForeignKey(
                        name: "fk_visibility_preferences_profiles_profile_id",
                        column: x => x.profile_id,
                        principalSchema: "profile",
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "profile",
                table: "interests",
                columns: new[] { "id", "label", "slug" },
                values: new object[,]
                {
                    { new Guid("018f0b65-2c2a-7a10-9d4d-47b6f6a10001"), "Books", "books" },
                    { new Guid("018f0b65-2c2a-7a10-9d4d-47b6f6a10002"), "Music", "music" },
                    { new Guid("018f0b65-2c2a-7a10-9d4d-47b6f6a10003"), "Art", "art" },
                    { new Guid("018f0b65-2c2a-7a10-9d4d-47b6f6a10004"), "Food", "food" },
                    { new Guid("018f0b65-2c2a-7a10-9d4d-47b6f6a10005"), "Nature", "nature" },
                    { new Guid("018f0b65-2c2a-7a10-9d4d-47b6f6a10006"), "Travel", "travel" },
                    { new Guid("018f0b65-2c2a-7a10-9d4d-47b6f6a10007"), "Movement", "movement" },
                    { new Guid("018f0b65-2c2a-7a10-9d4d-47b6f6a10008"), "Film", "film" },
                    { new Guid("018f0b65-2c2a-7a10-9d4d-47b6f6a10009"), "Technology", "technology" },
                    { new Guid("018f0b65-2c2a-7a10-9d4d-47b6f6a1000a"), "Learning", "learning" },
                    { new Guid("018f0b65-2c2a-7a10-9d4d-47b6f6a1000b"), "Volunteering", "volunteering" },
                    { new Guid("018f0b65-2c2a-7a10-9d4d-47b6f6a1000c"), "Craft", "craft" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_interests_slug",
                schema: "profile",
                table: "interests",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_profile_interests_interest_id",
                schema: "profile",
                table: "profile_interests",
                column: "interest_id");

            migrationBuilder.CreateIndex(
                name: "ix_profiles_user_id",
                schema: "profile",
                table: "profiles",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "profile_interests",
                schema: "profile");

            migrationBuilder.DropTable(
                name: "user_blocks",
                schema: "profile");

            migrationBuilder.DropTable(
                name: "visibility_preferences",
                schema: "profile");

            migrationBuilder.DropTable(
                name: "interests",
                schema: "profile");

            migrationBuilder.DropTable(
                name: "profiles",
                schema: "profile");
        }
    }
}
