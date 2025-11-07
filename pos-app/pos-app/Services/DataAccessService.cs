using Microsoft.Data.Sqlite;
using System.Data.SqlClient;
using System.Data;
using pos_app.Models;
using Microsoft.Extensions.Logging;
using pos_app.Data;
using Microsoft.EntityFrameworkCore;

namespace pos_app.Services
{
    public class DataAccessService
    {
        private readonly ILogger<DataAccessService> _logger;
        private readonly Dictionary<string, IDbConnection> _connectionPool = new();
        private readonly ClientDbContextFactory _contextFactory;

        public DataAccessService(
            ILogger<DataAccessService> logger,
            ClientDbContextFactory contextFactory)
        {
            _logger = logger;
            _contextFactory = contextFactory;
        }

        public async Task<IDbConnection> GetConnectionAsync(User user)
        {
            try
            {
                var connectionKey = $"{user.DatabaseType}_{user.ServerName}_{user.DatabaseName}";
                
                if (_connectionPool.TryGetValue(connectionKey, out var existingConnection) && existingConnection.State == ConnectionState.Open)
                {
                    return existingConnection;
                }

                IDbConnection connection = user.DatabaseType switch
                {
                    DatabaseType.SQLite => new SqliteConnection(user.GetConnectionString()),
                    DatabaseType.SQLServer => new SqlConnection(user.GetConnectionString()),
                    _ => throw new NotSupportedException($"Database type {user.DatabaseType} is not supported")
                };

                connection.Open();
                _connectionPool[connectionKey] = connection;
                
                return connection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create database connection for {DatabaseType}: {ServerName}", 
                    user.DatabaseType, user.ServerName);
                throw;
            }
        }

        public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(User user, string query, int? commandTimeout = null)
        {
            var results = new List<Dictionary<string, object>>();
            
            try
            {
                using var connection = await GetConnectionAsync(user);
                using var command = connection.CreateCommand();
                command.CommandText = query;
                if (commandTimeout.HasValue && command is SqlCommand sqlCommand)
                {
                    sqlCommand.CommandTimeout = commandTimeout.Value;
                }
                
                using var reader = command.ExecuteReader();
                
                while (reader.Read())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.GetValue(i);
                    }
                    results.Add(row);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute query: {Query}", query);
                throw;
            }
            
            return results;
        }

        public async Task<bool> TestConnectionAsync(User user)
        {
            try
            {
                using var connection = await GetConnectionAsync(user);
                await UpdateConnectionStatusAsync(user, true, null);
                return true;
            }
            catch (Exception ex)
            {
                await UpdateConnectionStatusAsync(user, false, ex.Message);
                _logger.LogError(ex, "Connection test failed for {DatabaseType} database: {ServerName}", 
                    user.DatabaseType, user.ServerName);
                return false;
            }
        }

        private Task UpdateConnectionStatusAsync(User user, bool isConnected, string? error)
        {
            try
            {
                user.LastConnectedAt = isConnected ? DateTime.UtcNow : user.LastConnectedAt;
                user.LastError = error;
                // Note: This would need to be updated in the database through the context
                // For now, we'll just update the object
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update connection status");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets a ClientDbContext instance for the specified user (read-only by default)
        /// </summary>
        public ClientDbContext GetClientContext(User user)
        {
            return _contextFactory.CreateContext(user);
        }

        /// <summary>
        /// Gets a ClientDbContext instance with change tracking enabled (for CRUD operations)
        /// </summary>
        public ClientDbContext GetClientContextWithTracking(User user)
        {
            return _contextFactory.CreateContextWithTracking(user);
        }

        /// <summary>
        /// Executes a raw SQL query using Entity Framework and returns strongly-typed results
        /// This uses FromSqlRaw for performance while maintaining EF benefits
        /// </summary>
        public async Task<List<T>> ExecuteRawSqlQueryAsync<T>(User user, FormattableString query) where T : class
        {
            try
            {
                using var context = GetClientContext(user);
                // Note: FromSqlInterpolated is safer than FromSqlRaw for parameterized queries
                return await context.Set<T>().FromSqlInterpolated(query).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute raw SQL query for user {UserId}", user.Id);
                throw;
            }
        }

        /// <summary>
        /// Executes a raw SQL query and returns dynamic results (for complex queries with no entity mapping)
        /// </summary>
        public async Task<List<Dictionary<string, object>>> ExecuteRawSqlDynamicAsync(User user, string sql, params object[] parameters)
        {
            try
            {
                using var context = GetClientContext(user);
                
                // Execute the query using raw SQL and get results
                using var command = context.Database.GetDbConnection().CreateCommand();
                command.CommandText = sql;
                
                if (parameters != null && parameters.Length > 0)
                {
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var param = command.CreateParameter();
                        param.ParameterName = $"@p{i}";
                        param.Value = parameters[i] ?? DBNull.Value;
                        command.Parameters.Add(param);
                    }
                }

                await context.Database.OpenConnectionAsync();
                
                var results = new List<Dictionary<string, object>>();
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.GetValue(i);
                    }
                    results.Add(row);
                }
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute raw dynamic SQL query for user {UserId}", user.Id);
                throw;
            }
        }

        public void Dispose()
        {
            foreach (var connection in _connectionPool.Values)
            {
                connection?.Dispose();
            }
            _connectionPool.Clear();
        }
    }
}
