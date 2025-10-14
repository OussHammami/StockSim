using StockSim.Infrastructure.Messaging;
using StockSim.Infrastructure.Persistence;
using StockSim.Web;
using StockSim.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// UI
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

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

app.Run();
