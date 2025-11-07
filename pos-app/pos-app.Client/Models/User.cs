namespace pos_app.Client.Models
{
    public enum DatabaseType
    {
        SQLite,
        SQLServer
    }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string CellNo { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string ServerName { get; set; } = string.Empty;
        public string DatabasePassword { get; set; } = string.Empty;
        public int Port { get; set; } = 1433;
        public string ConnectionName { get; set; } = string.Empty;
        
        // Additional fields from DatabaseConnection
        public DatabaseType DatabaseType { get; set; } = DatabaseType.SQLServer;
        public string? FilePath { get; set; } // For SQLite
        public int ConnectionTimeout { get; set; } = 30;
        public DateTime? LastConnectedAt { get; set; }
        public string? LastError { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
