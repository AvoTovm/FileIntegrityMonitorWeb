using FileIntegrityMonitor.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FileIntegrityMonitor.Services
{
    public class DatabaseInitializationService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public DatabaseInitializationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Create a new scope to get scoped services
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                try
                {
                    // Get the database path
                    var connection = dbContext.Database.GetDbConnection();
                    var databasePath = connection.ConnectionString.Replace("Data Source=", "").Trim();

                    Console.WriteLine($"Database file path: {databasePath}");
                    Console.WriteLine($"Database file exists: {File.Exists(databasePath)}");

                    // Ensure database is created
                    bool databaseCreated = await dbContext.Database.EnsureCreatedAsync(cancellationToken);

                    if (databaseCreated)
                    {
                        Console.WriteLine("Database was newly created");
                    }
                    else
                    {
                        Console.WriteLine("Database already existed");
                    }

                    // Check tables were created
                    bool hasUsersTable = await dbContext.Users.AnyAsync(cancellationToken);
                    bool hasRolesTable = await dbContext.Roles.AnyAsync(cancellationToken);

                    Console.WriteLine($"Users table has records: {hasUsersTable}");
                    Console.WriteLine($"Roles table has records: {hasRolesTable}");

                    // Try a query to verify tables are accessible
                    var userCount = await dbContext.Users.CountAsync(cancellationToken);
                    Console.WriteLine($"Total users in database: {userCount}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Database initialization error: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}