using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSim.Infrastructure.Persistence.Trading.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderCreatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                schema: "trading",
                table: "orders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_at",
                schema: "trading",
                table: "orders");
        }
    }
}
