using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NAU.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlumniProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "alumni_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch = table.Column<int>(type: "integer", nullable: false),
                    house = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    roll_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    mobile = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    current_city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    current_country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    company = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    designation = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    industry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    education = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    bio = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    linked_in_url = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    git_hub_url = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    photo_key = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    completion_pct = table.Column<int>(type: "integer", nullable: false),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    directory_visible = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    privacy = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_alumni_profiles", x => x.id);
                    table.ForeignKey(
                        name: "fk_alumni_profiles_schools_school_id",
                        column: x => x.school_id,
                        principalTable: "schools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_alumni_profiles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "skills",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "citext", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_skills", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "alumni_skills",
                columns: table => new
                {
                    profiles_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skills_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_alumni_skills", x => new { x.profiles_id, x.skills_id });
                    table.ForeignKey(
                        name: "fk_alumni_skills_alumni_profiles_profiles_id",
                        column: x => x.profiles_id,
                        principalTable: "alumni_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_alumni_skills_skills_skills_id",
                        column: x => x.skills_id,
                        principalTable: "skills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_alumni_profiles_company",
                table: "alumni_profiles",
                column: "company");

            migrationBuilder.CreateIndex(
                name: "ix_alumni_profiles_current_city",
                table: "alumni_profiles",
                column: "current_city");

            migrationBuilder.CreateIndex(
                name: "ix_alumni_profiles_school_id_batch",
                table: "alumni_profiles",
                columns: new[] { "school_id", "batch" });

            migrationBuilder.CreateIndex(
                name: "ix_alumni_profiles_user_id",
                table: "alumni_profiles",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_alumni_skills_skills_id",
                table: "alumni_skills",
                column: "skills_id");

            migrationBuilder.CreateIndex(
                name: "ix_skills_name",
                table: "skills",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alumni_skills");

            migrationBuilder.DropTable(
                name: "alumni_profiles");

            migrationBuilder.DropTable(
                name: "skills");
        }
    }
}
