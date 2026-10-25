using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelTravel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHotelReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HotelComment",
                table: "Reviews",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HotelRating",
                table: "Reviews",
                type: "int",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Reviews_HotelRating",
                table: "Reviews",
                sql: "[HotelRating] IS NULL OR [HotelRating] BETWEEN 1 AND 5");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Reviews_HotelRating",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "HotelComment",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "HotelRating",
                table: "Reviews");
        }
    }
}
