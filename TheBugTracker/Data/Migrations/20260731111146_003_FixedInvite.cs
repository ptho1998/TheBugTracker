using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheBugTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class _003_FixedInvite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "InviteeFistName",
                table: "Invites",
                newName: "InviteeFirstName");

            migrationBuilder.RenameColumn(
                name: "InviteEmail",
                table: "Invites",
                newName: "InviteeEmail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "InviteeFirstName",
                table: "Invites",
                newName: "InviteeFistName");

            migrationBuilder.RenameColumn(
                name: "InviteeEmail",
                table: "Invites",
                newName: "InviteEmail");
        }
    }
}
