using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperMegaUnboxDeluxe.Infrastructure.Persistence;

/// <summary>
/// Helper class for database initialization and seeding in development environments.
/// </summary>
public static class DatabaseInitializationHelper
{
    /// <summary>
    /// Applies pending migrations to the database in development environments only.
    /// </summary>
    /// <param name="app">The service provider from the host.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task InitializeDatabaseAsync(this IServiceProvider app)
    {
        using var scope = app.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SmudDbContext>>();

        try
        {
            var context = scope.ServiceProvider.GetRequiredService<SmudDbContext>();

            logger.LogInformation("Applying pending migrations to database...");

            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                await context.Database.MigrateAsync();
                logger.LogInformation("Database migrations applied successfully.");
            }
            else
            {
                logger.LogInformation("Database is up to date, no migrations to apply.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating the database.");
            throw;
        }
    }
}
