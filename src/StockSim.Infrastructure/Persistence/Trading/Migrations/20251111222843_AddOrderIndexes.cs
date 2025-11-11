using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSim.Infrastructure.Persistence.Trading.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expires_at",
                schema: "trading",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "time_in_force",
                schema: "trading",
                table: "orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_orders_state_created_at",
                schema: "trading",
                table: "orders",
                columns: new[] { "state", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_orders_symbol_state_limit_price",
                schema: "trading",
                table: "orders",
                columns: new[] { "symbol", "state", "limit_price" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_state_created_at",
                schema: "trading",
                table: "orders");
                
            migrationBuilder.DropIndex(
                name: "ix_orders_symbol_state_limit_price",
                schema: "trading",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "expires_at",
                schema: "trading",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "time_in_force",
                schema: "trading",
                table: "orders");
        }
    }
}
