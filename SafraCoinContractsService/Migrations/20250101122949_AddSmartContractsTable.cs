using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafraCoinContractsService.Migrations
{
    /// <inheritdoc />
    public partial class AddSmartContractsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SmartContracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    Abi = table.Column<string>(type: "text", nullable: false),
                    ByteCode = table.Column<string>(type: "text", nullable: false),
                    RawCodeHash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmartContracts", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SmartContracts");
        }
    }
}
