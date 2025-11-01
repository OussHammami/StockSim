using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StockSim.Infrastructure.Migrations.PortfolioDb
{
    /// <inheritdoc />
    public partial class InitPortfolio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "portfolio");

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "portfolio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DedupeKey = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    SeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                schema: "portfolio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    symbol = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    side = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    limit_price = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    state = table.Column<int>(type: "integer", nullable: false),
                    filled_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    avg_fill_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "portfolio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Data = table.Column<string>(type: "text", nullable: false),
                    SchemaVersion = table.Column<string>(type: "text", nullable: false),
                    DedupeKey = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "portfolios",
                schema: "portfolio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cash = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    reserved_cash = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_portfolios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "positions",
                schema: "portfolio",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    symbol = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    avg_cost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    portfolio_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_positions", x => x.id);
                    table.ForeignKey(
                        name: "FK_positions_portfolios_portfolio_id",
                        column: x => x.portfolio_id,
                        principalSchema: "portfolio",
                        principalTable: "portfolios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inbox_messages_DedupeKey",
                schema: "portfolio",
                table: "inbox_messages",
                column: "DedupeKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_DedupeKey",
                schema: "portfolio",
                table: "outbox_messages",
                column: "DedupeKey");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_SentAt_CreatedAt",
                schema: "portfolio",
                table: "outbox_messages",
                columns: new[] { "SentAt", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_positions_portfolio_id",
                schema: "portfolio",
                table: "positions",
                column: "portfolio_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "orders",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "positions",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "portfolios",
                schema: "portfolio");
        }
    }
}
