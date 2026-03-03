using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PiPlanningBackend.Migrations.SqlServer.Migrations
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Organization = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Project = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AzureStoryPointField = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AzureDevStoryPointField = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AzureTestStoryPointField = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NumSprints = table.Column<int>(type: "int", nullable: false),
                    SprintDuration = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsFinalized = table.Column<bool>(type: "bit", nullable: false),
                    FinalizedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DevTestToggle = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Boards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Features",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BoardId = table.Column<int>(type: "int", nullable: false),
                    AzureId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: true),
                    ValueArea = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsFinalized = table.Column<bool>(type: "bit", nullable: false)
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BoardId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsDev = table.Column<bool>(type: "bit", nullable: false),
                    IsTest = table.Column<bool>(type: "bit", nullable: false),
                    BoardId = table.Column<int>(type: "int", nullable: false)
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeatureId = table.Column<int>(type: "int", nullable: false),
                    AzureId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StoryPoints = table.Column<double>(type: "float", nullable: true),
                    DevStoryPoints = table.Column<double>(type: "float", nullable: true),
                    TestStoryPoints = table.Column<double>(type: "float", nullable: true),
                    OriginalSprintId = table.Column<int>(type: "int", nullable: true),
                    SprintId = table.Column<int>(type: "int", nullable: false),
                    IsMoved = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeamMemberId = table.Column<int>(type: "int", nullable: false),
                    SprintId = table.Column<int>(type: "int", nullable: false),
                    CapacityDev = table.Column<int>(type: "int", nullable: false),
                    CapacityTest = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMemberSprints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamMemberSprints_Sprints_SprintId",
                        column: x => x.SprintId,
                        principalTable: "Sprints",
                        principalColumn: "Id");
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
