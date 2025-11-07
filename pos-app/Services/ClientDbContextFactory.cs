using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using System.Data.SqlClient;
using pos_app.Data;
using pos_app.Models;

namespace pos_app.Services
{
    /// <summary>
    /// Factory service to create ClientDbContext instances for each client's database
    /// Supports multi-tenant architecture with dynamic connection strings
    /// </summary>
    public class ClientDbContextFactory
    {
        private readonly ILogger<ClientDbContextFactory> _logger;

        public ClientDbContextFactory(ILogger<ClientDbContextFactory> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Creates a new ClientDbContext instance for the specified user's database
        /// </summary>
        /// <param name="user">User object containing database connection information</param>
        /// <returns>ClientDbContext instance configured for the user's database</returns>
        public ClientDbContext CreateContext(User user)
        {
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<ClientDbContext>();

                switch (user.DatabaseType)
                {
                    case DatabaseType.SQLite:
                        optionsBuilder.UseSqlite(user.GetConnectionString());
                        _logger.LogDebug("Created SQLite ClientDbContext for user {UserId}, database {DatabaseName}", 
                            user.Id, user.DatabaseName);
                        break;

                    case DatabaseType.SQLServer:
                        optionsBuilder.UseSqlServer(user.GetConnectionString());
                        _logger.LogDebug("Created SQL Server ClientDbContext for user {UserId}, database {DatabaseName}", 
                            user.Id, user.DatabaseName);
                        break;

                    default:
                        throw new NotSupportedException($"Database type {user.DatabaseType} is not supported");
                }

                // Disable change tracking for better read performance (can be enabled per-operation if needed)
                optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

                return new ClientDbContext(optionsBuilder.Options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create ClientDbContext for user {UserId}, database type {DatabaseType}", 
                    user.Id, user.DatabaseType);
                throw;
            }
        }

        /// <summary>
        /// Creates a new ClientDbContext instance with change tracking enabled
        /// Use this for operations that need to track entities (Insert, Update, Delete)
        /// </summary>
        /// <param name="user">User object containing database connection information</param>
        /// <returns>ClientDbContext instance with change tracking enabled</returns>
        public ClientDbContext CreateContextWithTracking(User user)
        {
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<ClientDbContext>();

                switch (user.DatabaseType)
                {
                    case DatabaseType.SQLite:
                        optionsBuilder.UseSqlite(user.GetConnectionString());
                        break;

                    case DatabaseType.SQLServer:
                        optionsBuilder.UseSqlServer(user.GetConnectionString());
                        break;

                    default:
                        throw new NotSupportedException($"Database type {user.DatabaseType} is not supported");
                }

                // Enable change tracking for CRUD operations
                optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);

                _logger.LogDebug("Created ClientDbContext with tracking for user {UserId}, database {DatabaseName}", 
                    user.Id, user.DatabaseName);

                return new ClientDbContext(optionsBuilder.Options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create ClientDbContext with tracking for user {UserId}", user.Id);
                throw;
            }
        }

        /// <summary>
        /// Tests if a connection can be established to the client database
        /// </summary>
        /// <param name="user">User object containing database connection information</param>
        /// <returns>True if connection is successful, false otherwise</returns>
        public async Task<bool> TestConnectionAsync(User user)
        {
            try
            {
                using var context = CreateContext(user);
                await context.Database.CanConnectAsync();
                _logger.LogInformation("Successfully connected to client database for user {UserId}", user.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to client database for user {UserId}", user.Id);
                return false;
            }
        }
    }
}

