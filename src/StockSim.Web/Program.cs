using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server; 
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using MudBlazor.Services;
using StockSim.Web.Components;
using StockSim.Web.Components.Account;
using StockSim.Web.Data;
using StockSim.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddScoped<IPortfolioServiceAsync, EfPortfolioService>();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.User.RequireUniqueEmail = true; // recommended
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.Configure<RabbitOptions>(builder.Configuration.GetSection("Rabbit"));
builder.Services.AddSingleton<RabbitConnection>();
builder.Services.AddSingleton<OrderPublisher, OrderPublisher>();
builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
builder.Services.AddSingleton<LastQuotesCache>();
builder.Services.AddSingleton<IOrderPublisher, OrderPublisher>();
builder.Services.AddHostedService<OrderConsumer>();

builder.Services.AddMudServices(o =>
{
    o.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    o.SnackbarConfiguration.VisibleStateDuration = 2500;
});
builder.Services.AddHttpClient("MarketFeed", (sp, client) =>
{
    var baseUrl = builder.Configuration["MarketFeed:BaseUrl"] ?? "https://localhost:7173";
    client.BaseAddress = new Uri(baseUrl);
});

// bind CircuitOptions from configuration (Development file overrides)
builder.Services.Configure<CircuitOptions>(builder.Configuration.GetSection("CircuitOptions"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();

app.Run();
