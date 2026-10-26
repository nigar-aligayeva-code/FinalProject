using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelTravel.Infrastructure.Migrations
{
    /// <summary>
    /// Ləğv cəriməsi + qonağın 🔔 bildirişləri.
    /// Hər addım "yoxdursa əlavə et" şəklindədir: bazada sütun / cədvəl artıq varsa
    /// (əvvəlki yarımçıq migration-dan qalıb), xəta vermir və keçir.
    /// </summary>
    public partial class AddCancellationFeesAndNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // REZERVASİYA: cərimə, qaytarılan məbləğ, ləğv tarixi
            AddColumnIfMissing(migrationBuilder, "Bookings", "CancellationFee", "decimal(18,2) NOT NULL DEFAULT 0.0");
            AddColumnIfMissing(migrationBuilder, "Bookings", "RefundAmount", "decimal(18,2) NOT NULL DEFAULT 0.0");
            AddColumnIfMissing(migrationBuilder, "Bookings", "CancelledAt", "datetime2 NULL");

            // UÇUŞ: cərimə, ləğv tarixi
            AddColumnIfMissing(migrationBuilder, "FlightBookings", "CancellationFee", "decimal(18,2) NOT NULL DEFAULT 0.0");
            AddColumnIfMissing(migrationBuilder, "FlightBookings", "CancelledAt", "datetime2 NULL");

            // QONAĞIN BİLDİRİŞLƏRİ (🔔)
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[UserNotifications]', N'U') IS NULL
BEGIN
    CREATE TABLE [UserNotifications] (
        [Id] int NOT NULL IDENTITY(1,1),
        [AppUserId] nvarchar(450) NOT NULL,
        [Kind] nvarchar(40) NOT NULL,
        [Place] nvarchar(200) NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [When] nvarchar(40) NULL,
        [Url] nvarchar(300) NULL,
        [Message] nvarchar(500) NOT NULL,
        [IsRead] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_UserNotifications] PRIMARY KEY ([Id])
    );

    CREATE INDEX [IX_UserNotifications_AppUserId_IsRead_CreatedAt]
        ON [UserNotifications] ([AppUserId], [IsRead], [CreatedAt]);
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF OBJECT_ID(N'[UserNotifications]', N'U') IS NOT NULL DROP TABLE [UserNotifications];");

            DropColumnIfExists(migrationBuilder, "FlightBookings", "CancelledAt");
            DropColumnIfExists(migrationBuilder, "FlightBookings", "CancellationFee");
            DropColumnIfExists(migrationBuilder, "Bookings", "CancelledAt");
            DropColumnIfExists(migrationBuilder, "Bookings", "RefundAmount");
            DropColumnIfExists(migrationBuilder, "Bookings", "CancellationFee");
        }

        private static void AddColumnIfMissing(MigrationBuilder mb, string table, string column, string definition)
            => mb.Sql($@"
IF COL_LENGTH(N'[{table}]', N'{column}') IS NULL
    ALTER TABLE [{table}] ADD [{column}] {definition};");

        // Sütunun DEFAULT constraint-i varsa əvvəl o silinir
        private static void DropColumnIfExists(MigrationBuilder mb, string table, string column)
            => mb.Sql($@"
IF COL_LENGTH(N'[{table}]', N'{column}') IS NOT NULL
BEGIN
    DECLARE @df sysname;
    SELECT @df = d.name FROM sys.default_constraints d
        JOIN sys.columns c ON c.default_object_id = d.object_id
        WHERE d.parent_object_id = OBJECT_ID(N'[{table}]') AND c.name = N'{column}';
    IF @df IS NOT NULL EXEC(N'ALTER TABLE [{table}] DROP CONSTRAINT [' + @df + N']');
    ALTER TABLE [{table}] DROP COLUMN [{column}];
END");
    }
}