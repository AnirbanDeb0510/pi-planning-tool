using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PiPlanningBackend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Boards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Organization = table.Column<string>(type: "text", nullable: true),
                    Project = table.Column<string>(type: "text", nullable: true),
                    AzureStoryPointField = table.Column<string>(type: "text", nullable: true),
                    AzureDevStoryPointField = table.Column<string>(type: "text", nullable: true),
                    AzureTestStoryPointField = table.Column<string>(type: "text", nullable: true),
                    NumSprints = table.Column<int>(type: "integer", nullable: false),
                    SprintDuration = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    IsFinalized = table.Column<bool>(type: "boolean", nullable: false),
                    DevTestToggle = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Boards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CursorPresences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BoardId = table.Column<int>(type: "integer", nullable: false),
                    TeamMemberId = table.Column<int>(type: "integer", nullable: true),
                    X = table.Column<double>(type: "double precision", nullable: false),
                    Y = table.Column<double>(type: "double precision", nullable: false),
                    LastSeen = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CursorPresences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Features",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BoardId = table.Column<int>(type: "integer", nullable: false),
                    AzureId = table.Column<string>(type: "text", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: true),
                    ValueArea = table.Column<string>(type: "text", nullable: true),
                    IsFinalized = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Features", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Features_Boards_BoardId",
                        column: x => x.BoardId,
                        principalTable: "Boards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
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
                    table.PrimaryKey("PK_Sprints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sprints_Boards_BoardId",
                        column: x => x.BoardId,
                        principalTable: "Boards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
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
                    table.PrimaryKey("PK_TeamMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamMembers_Boards_BoardId",
                        column: x => x.BoardId,
                        principalTable: "Boards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserStories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FeatureId = table.Column<int>(type: "integer", nullable: false),
                    AzureId = table.Column<string>(type: "text", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StoryPoints = table.Column<double>(type: "double precision", nullable: true),
                    DevStoryPoints = table.Column<double>(type: "double precision", nullable: true),
                    TestStoryPoints = table.Column<double>(type: "double precision", nullable: true),
                    OriginalSprintId = table.Column<int>(type: "integer", nullable: true),
                    CurrentSprintId = table.Column<int>(type: "integer", nullable: true),
                    IsMoved = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    SprintId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserStories_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserStories_Sprints_SprintId",
                        column: x => x.SprintId,
                        principalTable: "Sprints",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TeamMemberSprints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamMemberId = table.Column<int>(type: "integer", nullable: false),
                    SprintId = table.Column<int>(type: "integer", nullable: false),
                    CapacityDev = table.Column<double>(type: "float", nullable: false),
                    CapacityTest = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMemberSprints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamMemberSprints_Sprints_SprintId",
                        column: x => x.SprintId,
                        principalTable: "Sprints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamMemberSprints_TeamMembers_TeamMemberId",
                        column: x => x.TeamMemberId,
                        principalTable: "TeamMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Features_AzureId",
                table: "Features",
                column: "AzureId");

            migrationBuilder.CreateIndex(
                name: "IX_Features_BoardId",
                table: "Features",
                column: "BoardId");

            migrationBuilder.CreateIndex(
                name: "IX_Sprints_BoardId",
                table: "Sprints",
                column: "BoardId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_BoardId",
                table: "TeamMembers",
                column: "BoardId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMemberSprints_SprintId",
                table: "TeamMemberSprints",
                column: "SprintId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMemberSprints_TeamMemberId_SprintId",
                table: "TeamMemberSprints",
                columns: new[] { "TeamMemberId", "SprintId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserStories_AzureId",
                table: "UserStories",
                column: "AzureId");

            migrationBuilder.CreateIndex(
                name: "IX_UserStories_CurrentSprintId",
                table: "UserStories",
                column: "CurrentSprintId");

            migrationBuilder.CreateIndex(
                name: "IX_UserStories_FeatureId",
                table: "UserStories",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_UserStories_SprintId",
                table: "UserStories",
                column: "SprintId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CursorPresences");

            migrationBuilder.DropTable(
                name: "TeamMemberSprints");

            migrationBuilder.DropTable(
                name: "UserStories");

            migrationBuilder.DropTable(
                name: "TeamMembers");

            migrationBuilder.DropTable(
                name: "Features");

            migrationBuilder.DropTable(
                name: "Sprints");

            migrationBuilder.DropTable(
                name: "Boards");
        }
    }
}
