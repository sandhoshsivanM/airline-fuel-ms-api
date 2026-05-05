using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AirlineFuelMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Airlines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Model = table.Column<string>(type: "TEXT", nullable: false),
                    PassengerCapacity = table.Column<int>(type: "INTEGER", nullable: false),
                    FuelTankCapacityLiters = table.Column<int>(type: "INTEGER", nullable: false),
                    Country = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Airlines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    CurrencyCode = table.Column<string>(type: "TEXT", nullable: false),
                    CurrencySymbol = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FuelProviders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    ContactInfo = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelProviders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "NormalUser"),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FuelPrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FuelProviderId = table.Column<int>(type: "INTEGER", nullable: false),
                    PricePerLiter = table.Column<decimal>(type: "TEXT", precision: 10, scale: 4, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuelPrices_FuelProviders_FuelProviderId",
                        column: x => x.FuelProviderId,
                        principalTable: "FuelProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FuelProviderAddresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FuelProviderId = table.Column<int>(type: "INTEGER", nullable: false),
                    CountryId = table.Column<int>(type: "INTEGER", nullable: false),
                    AddressLine1 = table.Column<string>(type: "TEXT", nullable: false),
                    City = table.Column<string>(type: "TEXT", nullable: false),
                    PostalCode = table.Column<string>(type: "TEXT", nullable: false),
                    IsHeadOffice = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelProviderAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuelProviderAddresses_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FuelProviderAddresses_FuelProviders_FuelProviderId",
                        column: x => x.FuelProviderId,
                        principalTable: "FuelProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FuelTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AirlineId = table.Column<int>(type: "INTEGER", nullable: false),
                    FuelProviderId = table.Column<int>(type: "INTEGER", nullable: false),
                    FuelProviderAddressId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    QuantityLiters = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    PricePerLiter = table.Column<decimal>(type: "TEXT", precision: 10, scale: 4, nullable: false),
                    TransactionRef = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuelTransactions_Airlines_AirlineId",
                        column: x => x.AirlineId,
                        principalTable: "Airlines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FuelTransactions_FuelProviderAddresses_FuelProviderAddressId",
                        column: x => x.FuelProviderAddressId,
                        principalTable: "FuelProviderAddresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FuelTransactions_FuelProviders_FuelProviderId",
                        column: x => x.FuelProviderId,
                        principalTable: "FuelProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FuelTransactions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FuelTransactionId = table.Column<int>(type: "INTEGER", nullable: false),
                    AirlineId = table.Column<int>(type: "INTEGER", nullable: false),
                    FuelProviderId = table.Column<int>(type: "INTEGER", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PaidDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_Airlines_AirlineId",
                        column: x => x.AirlineId,
                        principalTable: "Airlines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invoices_FuelProviders_FuelProviderId",
                        column: x => x.FuelProviderId,
                        principalTable: "FuelProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invoices_FuelTransactions_FuelTransactionId",
                        column: x => x.FuelTransactionId,
                        principalTable: "FuelTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Countries_Code",
                table: "Countries",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Countries_Name",
                table: "Countries",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FuelPrices_FuelProviderId",
                table: "FuelPrices",
                column: "FuelProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelProviderAddresses_CountryId",
                table: "FuelProviderAddresses",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelProviderAddresses_FuelProviderId_CountryId",
                table: "FuelProviderAddresses",
                columns: new[] { "FuelProviderId", "CountryId" });

            migrationBuilder.CreateIndex(
                name: "IX_FuelProviders_Code",
                table: "FuelProviders",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FuelTransactions_AirlineId",
                table: "FuelTransactions",
                column: "AirlineId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelTransactions_CreatedByUserId",
                table: "FuelTransactions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelTransactions_FuelProviderAddressId",
                table: "FuelTransactions",
                column: "FuelProviderAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelTransactions_FuelProviderId",
                table: "FuelTransactions",
                column: "FuelProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_AirlineId",
                table: "Invoices",
                column: "AirlineId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_FuelProviderId",
                table: "Invoices",
                column: "FuelProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_FuelTransactionId",
                table: "Invoices",
                column: "FuelTransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "Invoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FuelPrices");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "FuelTransactions");

            migrationBuilder.DropTable(
                name: "Airlines");

            migrationBuilder.DropTable(
                name: "FuelProviderAddresses");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Countries");

            migrationBuilder.DropTable(
                name: "FuelProviders");
        }
    }
}
