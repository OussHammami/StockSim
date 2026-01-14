using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using StockSim.Infrastructure.Persistence.Portfolioing;

#nullable disable

namespace StockSim.Infrastructure.Migrations.PortfolioDb
{
    [DbContext(typeof(PortfolioDbContext))]
    [Migration("20260114160010_AddOutboxTraceContext")]
    public partial class AddOutboxTraceContext : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TraceParent",
                schema: "portfolio",
                table: "outbox_messages",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TraceState",
                schema: "portfolio",
                table: "outbox_messages",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Baggage",
                schema: "portfolio",
                table: "outbox_messages",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TraceParent",
                schema: "portfolio",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "TraceState",
                schema: "portfolio",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "Baggage",
                schema: "portfolio",
                table: "outbox_messages");
        }
    }
}
