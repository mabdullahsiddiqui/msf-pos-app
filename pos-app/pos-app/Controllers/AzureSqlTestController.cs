using Microsoft.AspNetCore.Mvc;
using pos_app.Models;
using pos_app.Services;

namespace pos_app.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AzureSqlTestController : ControllerBase
    {
        private readonly DataAccessService _dataAccessService;

        public AzureSqlTestController(DataAccessService dataAccessService)
        {
            _dataAccessService = dataAccessService;
        }

        [HttpGet("test-connection")]
        public async Task<IActionResult> TestAzureSqlConnection()
        {
            try
            {
                // Test Azure SQL Database connection using the provided credentials
                var testUser = new User
                {
                    Username = "test",
                    PasswordHash = "test",
                    CompanyName = "Test",
                    Email = "test@test.com",
                    ContactPerson = "Test",
                    CellNo = "1234567890",
                    DatabaseName = "mss-db",
                    ServerName = "mss-server.database.windows.net",
                    DatabasePassword = "*mas@MSS@mth*", // Azure SQL Admin Password
                    Port = 1433, // Standard SQL Server port
                    ConnectionName = "Azure SQL Test Connection",
                    DatabaseType = DatabaseType.SQLServer,
                    ConnectionTimeout = 30,
                    IsActive = true
                };

                var isConnected = await _dataAccessService.TestConnectionAsync(testUser);
                
                if (isConnected)
                {
                    return Ok(new { 
                        success = true, 
                        message = "Azure SQL Database connection successful!",
                        serverName = testUser.ServerName,
                        databaseName = testUser.DatabaseName
                    });
                }
                else
                {
                    return StatusCode(500, new { 
                        success = false, 
                        message = "Azure SQL Database connection failed" 
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = $"Azure SQL Database connection error: {ex.Message}", 
                    details = ex.ToString() 
                });
            }
        }
    }
}
