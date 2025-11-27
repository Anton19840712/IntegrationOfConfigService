using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UserBehaviorProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserBehaviorProfiles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    KnownIpAddresses = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    TypicalActiveHoursUtc = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    KnownUserAgents = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBehaviorProfiles", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserBehaviorProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserBehaviorProfiles");
        }
    }
}
