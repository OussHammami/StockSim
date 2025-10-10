using Microsoft.AspNetCore.Components.Server;
using Microsoft.EntityFrameworkCore;
using StockSim.Infrastructure.Messaging;
using StockSim.Infrastructure.Persistence;
using StockSim.Web;
using StockSim.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// services
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAppIdentity(builder.Configuration);
builder.Services.AddDomainServices(builder.Configuration);
builder.Services.AddUiServices();
builder.Services.Configure<CircuitOptions>(builder.Configuration.GetSection("CircuitOptions"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// pipeline
app.UseAppPipeline();

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();

app.MapHub<OrderHub>("/hubs/orders");

app.Run();
