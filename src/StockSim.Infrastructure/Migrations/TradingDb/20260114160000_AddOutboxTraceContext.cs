using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using StockSim.Infrastructure.Persistence.Trading;

#nullable disable

namespace StockSim.Infrastructure.Migrations.TradingDb
{
    [DbContext(typeof(TradingDbContext))]
    [Migration("20260114160000_AddOutboxTraceContext")]
    public partial class AddOutboxTraceContext : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TraceParent",
                schema: "trading",
                table: "outbox_messages",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TraceState",
                schema: "trading",
                table: "outbox_messages",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Baggage",
                schema: "trading",
                table: "outbox_messages",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TraceParent",
                schema: "trading",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "TraceState",
                schema: "trading",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "Baggage",
                schema: "trading",
                table: "outbox_messages");
        }
    }
}
