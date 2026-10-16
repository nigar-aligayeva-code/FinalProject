using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelTravel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFlights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FlightRoutes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FlightNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Airline = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    FromCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FromCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ToCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ToCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DepartureTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    EconomyPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BusinessPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EconomySeats = table.Column<int>(type: "int", nullable: false),
                    BusinessSeats = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlightRoutes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FlightBookings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConfirmationCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FlightRouteId = table.Column<int>(type: "int", nullable: false),
                    FlightDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FlightNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Airline = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    FromCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FromCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ToCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ToCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DepartureTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArrivalTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Cabin = table.Column<int>(type: "int", nullable: false),
                    Passengers = table.Column<int>(type: "int", nullable: false),
                    PassengerNames = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: false),
                    FarePerPassenger = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AppUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlightBookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlightBookings_AspNetUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FlightBookings_FlightRoutes_FlightRouteId",
                        column: x => x.FlightRouteId,
                        principalTable: "FlightRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FlightBookings_AppUserId",
                table: "FlightBookings",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FlightBookings_ConfirmationCode",
                table: "FlightBookings",
                column: "ConfirmationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlightBookings_FlightRouteId_FlightDate",
                table: "FlightBookings",
                columns: new[] { "FlightRouteId", "FlightDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FlightRoutes_FromCity_ToCity",
                table: "FlightRoutes",
                columns: new[] { "FromCity", "ToCity" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FlightBookings");

            migrationBuilder.DropTable(
                name: "FlightRoutes");
        }
    }
}
