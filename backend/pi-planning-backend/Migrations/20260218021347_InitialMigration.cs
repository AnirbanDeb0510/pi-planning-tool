using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PiPlanningBackend.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.CreateTable(
                name: "Boards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Organization = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Project = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AzureStoryPointField = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AzureDevStoryPointField = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AzureTestStoryPointField = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NumSprints = table.Column<int>(type: "integer", nullable: false),
                    SprintDuration = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    IsFinalized = table.Column<bool>(type: "boolean", nullable: false),
                    FinalizedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DevTestToggle = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_Boards", x => x.Id);
                });

            _ = migrationBuilder.CreateTable(
                name: "Features",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BoardId = table.Column<int>(type: "integer", nullable: false),
                    AzureId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: true),
                    ValueArea = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsFinalized = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_Features", x => x.Id);
                    _ = table.ForeignKey(
                        name: "FK_Features_Boards_BoardId",
                        column: x => x.BoardId,
                        principalTable: "Boards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            _ = migrationBuilder.CreateTable(
                name: "Sprints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BoardId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_Sprints", x => x.Id);
                    _ = table.ForeignKey(
                        name: "FK_Sprints_Boards_BoardId",
                        column: x => x.BoardId,
                        principalTable: "Boards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            _ = migrationBuilder.CreateTable(
                name: "TeamMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsDev = table.Column<bool>(type: "boolean", nullable: false),
                    IsTest = table.Column<bool>(type: "boolean", nullable: false),
                    BoardId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_TeamMembers", x => x.Id);
                    _ = table.ForeignKey(
                        name: "FK_TeamMembers_Boards_BoardId",
                        column: x => x.BoardId,
                        principalTable: "Boards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            _ = migrationBuilder.CreateTable(
                name: "UserStories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FeatureId = table.Column<int>(type: "integer", nullable: false),
                    AzureId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StoryPoints = table.Column<double>(type: "double precision", nullable: true),
                    DevStoryPoints = table.Column<double>(type: "double precision", nullable: true),
                    TestStoryPoints = table.Column<double>(type: "double precision", nullable: true),
                    OriginalSprintId = table.Column<int>(type: "integer", nullable: true),
                    SprintId = table.Column<int>(type: "integer", nullable: false),
                    IsMoved = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_UserStories", x => x.Id);
                    _ = table.ForeignKey(
                        name: "FK_UserStories_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    _ = table.ForeignKey(
                        name: "FK_UserStories_Sprints_SprintId",
                        column: x => x.SprintId,
                        principalTable: "Sprints",
                        principalColumn: "Id");
                });

            _ = migrationBuilder.CreateTable(
                name: "TeamMemberSprints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamMemberId = table.Column<int>(type: "integer", nullable: false),
                    SprintId = table.Column<int>(type: "integer", nullable: false),
                    CapacityDev = table.Column<int>(type: "integer", nullable: false),
                    CapacityTest = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_TeamMemberSprints", x => x.Id);
                    _ = table.ForeignKey(
                        name: "FK_TeamMemberSprints_Sprints_SprintId",
                        column: x => x.SprintId,
                        principalTable: "Sprints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    _ = table.ForeignKey(
                        name: "FK_TeamMemberSprints_TeamMembers_TeamMemberId",
                        column: x => x.TeamMemberId,
                        principalTable: "TeamMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            _ = migrationBuilder.CreateIndex(
                name: "IX_Features_AzureId",
                table: "Features",
                column: "AzureId");

            _ = migrationBuilder.CreateIndex(
                name: "IX_Features_BoardId",
                table: "Features",
                column: "BoardId");

            _ = migrationBuilder.CreateIndex(
                name: "IX_Sprints_BoardId",
                table: "Sprints",
                column: "BoardId");

            _ = migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_BoardId",
                table: "TeamMembers",
                column: "BoardId");

            _ = migrationBuilder.CreateIndex(
                name: "IX_TeamMemberSprints_SprintId",
                table: "TeamMemberSprints",
                column: "SprintId");

            _ = migrationBuilder.CreateIndex(
                name: "IX_TeamMemberSprints_TeamMemberId_SprintId",
                table: "TeamMemberSprints",
                columns: new[] { "TeamMemberId", "SprintId" });

            _ = migrationBuilder.CreateIndex(
                name: "IX_UserStories_AzureId",
                table: "UserStories",
                column: "AzureId");

            _ = migrationBuilder.CreateIndex(
                name: "IX_UserStories_FeatureId",
                table: "UserStories",
                column: "FeatureId");

            _ = migrationBuilder.CreateIndex(
                name: "IX_UserStories_SprintId",
                table: "UserStories",
                column: "SprintId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.DropTable(
                name: "TeamMemberSprints");

            _ = migrationBuilder.DropTable(
                name: "UserStories");

            _ = migrationBuilder.DropTable(
                name: "TeamMembers");

            _ = migrationBuilder.DropTable(
                name: "Features");

            _ = migrationBuilder.DropTable(
                name: "Sprints");

            _ = migrationBuilder.DropTable(
                name: "Boards");
        }
    }
}
