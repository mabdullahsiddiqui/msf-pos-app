using System.ComponentModel.DataAnnotations;

namespace pos_app.Models
{
    public enum DatabaseType
    {
        SQLite,
        SQLServer
    }

    public class User
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string CompanyName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string ContactPerson { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string CellNo { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string DatabaseName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string ServerName { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string DatabasePassword { get; set; } = string.Empty;
        
        public int Port { get; set; } = 1433;
        
        [StringLength(100)]
        public string ConnectionName { get; set; } = string.Empty;
        
        // Additional fields from DatabaseConnection
        [Required]
        public DatabaseType DatabaseType { get; set; } = DatabaseType.SQLServer;
        
        public string? FilePath { get; set; } // For SQLite
        
        public int ConnectionTimeout { get; set; } = 30;
        
        public DateTime? LastConnectedAt { get; set; }
        
        public string? LastError { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastLoginAt { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public string GetConnectionString()
        {
            return DatabaseType switch
            {
                DatabaseType.SQLite => $"Data Source={FilePath}",
                DatabaseType.SQLServer => BuildSqlServerConnectionString(),
                _ => throw new NotSupportedException($"Database type {DatabaseType} is not supported")
            };
        }
        
        private string BuildSqlServerConnectionString()
        {
            var builder = new System.Data.SqlClient.SqlConnectionStringBuilder
            {
                DataSource = ServerName,
                InitialCatalog = DatabaseName,
                UserID = Username,
                Password = DatabasePassword,
                ConnectTimeout = ConnectionTimeout,
                TrustServerCertificate = true,  // Fix for SSL certificate trust issue
                Encrypt = false  // Optional: disable encryption if not needed
            };
            
            if (Port != 1433)
            {
                builder.DataSource = $"{ServerName},{Port}";
            }
            
            return builder.ConnectionString;
        }
    }
}
