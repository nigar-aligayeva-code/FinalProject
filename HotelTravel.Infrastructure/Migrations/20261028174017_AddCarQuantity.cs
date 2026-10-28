using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelTravel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCarQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "RentalCars",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "RentalCars");
        }
    }
}
