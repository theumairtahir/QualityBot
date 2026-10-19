using Microsoft.EntityFrameworkCore;

namespace Sis.Api.Persistence;

public static class PersistenceExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Sis")
            ?? throw new InvalidOperationException("Connection string 'ConnectionStrings:Sis' is not configured.");

        services.AddDbContext<SisDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        services.AddHealthChecks().AddDbContextCheck<SisDbContext>("database");

        return services;
    }

    /// <summary>Applies pending migrations when <c>Database:ApplyMigrationsOnStartup</c> is true.</summary>
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SisDbContext>();
        await db.Database.MigrateAsync();
    }
}
