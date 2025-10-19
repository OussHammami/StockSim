using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StockSim.Infrastructure.Persistence;
using StockSim.Infrastructure.Persistence.Entities;
using StockSim.Infrastructure.Persistence.Identity;

public sealed class DemoOptions
{
    public bool Seed { get; init; }
    public string AdminEmail { get; init; } = "";
    public string AdminPassword { get; init; } = "";
    public string UserEmail { get; init; } = "";
    public string UserPassword { get; init; } = "";
}

public sealed class DemoSeedHostedService(
    IServiceProvider sp,
    IOptions<DemoOptions> opt,
    ILogger<DemoSeedHostedService> log) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        if (!opt.Value.Seed) return;

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var um = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await db.Database.MigrateAsync(ct);

        async Task<ApplicationUser> EnsureUser(string email, string pwd, string role)
        {
            var u = await um.Users.FirstOrDefaultAsync(x => x.Email == email, ct);
            if (u is null)
            {
                u = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
                var res = await um.CreateAsync(u, pwd);
                if (!res.Succeeded) throw new InvalidOperationException(string.Join(';', res.Errors.Select(e => e.Description)));
                if (!string.IsNullOrWhiteSpace(role))
                    await um.AddToRoleAsync(u, role);
            }
            return u;
        }

        // Roles
        var rm = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Admin", "Trader" })
            if (!await rm.RoleExistsAsync(role)) await rm.CreateAsync(new IdentityRole(role));

        var admin = await EnsureUser(opt.Value.AdminEmail, opt.Value.AdminPassword, "Admin");
        var user  = await EnsureUser(opt.Value.UserEmail,  opt.Value.UserPassword,  "Trader");

        // Seed portfolio + cash + a couple of positions for demo user
        async Task EnsurePortfolio(string userId)
        {
            var p = await db.Portfolios.FindAsync([userId], ct);
            if (p is null)
            {
                p = new PortfolioEntity { UserId = userId, Cash = 100_000m };
                db.Portfolios.Add(p);
                db.Positions.AddRange(
                    new PositionEntity { UserId = userId, Symbol = "AAPL", Quantity = 10, AvgPrice = 180m },
                    new PositionEntity { UserId = userId, Symbol = "MSFT", Quantity = 5,  AvgPrice = 350m }
                );
                await db.SaveChangesAsync(ct);
            }
        }

        await EnsurePortfolio(user.Id);
        log.LogInformation("Demo seed complete. Admin={Admin} User={User}", opt.Value.AdminEmail, opt.Value.UserEmail);
    }

    public Task StopAsync(CancellationToken _) => Task.CompletedTask;
}
