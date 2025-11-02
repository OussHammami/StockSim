using Microsoft.AspNetCore.Identity;
using StockSim.Infrastructure.Identity;

public sealed class IdentitySeedHostedService(
    IServiceScopeFactory scopes,
    ILogger<IdentitySeedHostedService> log) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        using var s = scopes.CreateScope();
        var roles = s.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var users = s.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        async Task EnsureRole(string name)
        {
            if (!await roles.RoleExistsAsync(name))
                _ = await roles.CreateAsync(new IdentityRole(name));
        }

        async Task EnsureUser(string email, string pwd, params string[] roleNames)
        {
            var u = await users.FindByEmailAsync(email);
            if (u is null)
            {
                u = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
                var r = await users.CreateAsync(u, pwd);
                if (!r.Succeeded)
                    throw new InvalidOperationException(string.Join(';', r.Errors.Select(e => e.Description)));
            }
            foreach (var role in roleNames)
                if (!await users.IsInRoleAsync(u, role))
                    _ = await users.AddToRoleAsync(u, role);
        }

        await EnsureRole("Admin");
        await EnsureRole("Trader");

        await EnsureUser("admin@stocksim.local", "Admin#123", "Admin");
        await EnsureUser("trader@stocksim.local", "Trader#123", "Trader");
        log.LogInformation("Seeded demo users Admin and Trader.");
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
