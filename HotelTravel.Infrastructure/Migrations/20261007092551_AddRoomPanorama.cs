using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelTravel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomPanorama : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PanoramaImage",
                table: "Rooms",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PanoramaImage",
                table: "Rooms");
        }
    }
}
