using Microsoft.EntityFrameworkCore;
using StockSim.Infrastructure.Messaging;
using StockSim.Infrastructure.Persistence;
using StockSim.Web;
using StockSim.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Observability (Serilog + OTel)
builder.AddObservability();

// App services
builder.Services.AddAppIdentity(builder.Configuration);
builder.Services.AddDomainServices(builder.Configuration);
builder.Services.AddUiServices();
builder.Services.AddSecurity(builder.Environment);

var app = builder.Build();

// DB migrate
app.ApplyMigrations<ApplicationDbContext>();

// Pipeline + endpoints
app.UseRequestPipeline();
app.MapAppEndpoints<App, OrderHub>();
app.MapPost("/admin/reset-demo", async (ApplicationDbContext db) =>
{
    await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Orders\",\"Positions\",\"Portfolios\",\"OutboxMessages\" RESTART IDENTITY CASCADE;");
    return Results.Ok();
}).RequireAuthorization(policy => policy.RequireRole("Admin"));


app.Run();
