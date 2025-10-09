using Microsoft.AspNetCore.Components.Server;
using StockSim.Infrastructure.Messaging;
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

// pipeline
app.UseAppPipeline();

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();

app.MapHub<OrderHub>("/hubs/orders");

app.Run();
