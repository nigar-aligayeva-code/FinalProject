using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelTravel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppUserId",
                table: "Bookings",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_AppUserId",
                table: "Bookings",
                column: "AppUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_AspNetUsers_AppUserId",
                table: "Bookings",
                column: "AppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_AspNetUsers_AppUserId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_AppUserId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "AppUserId",
                table: "Bookings");
        }
    }
}
