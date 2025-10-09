using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSim.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConvertDateTimeOffsetToUnix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "SubmittedUtc",
                table: "Orders",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<long>(
                name: "FilledUtc",
                table: "Orders",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId_SubmittedUtc",
                table: "Orders",
                columns: new[] { "UserId", "SubmittedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_UserId_SubmittedUtc",
                table: "Orders");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "SubmittedUtc",
                table: "Orders",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "FilledUtc",
                table: "Orders",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);
        }
    }
}
