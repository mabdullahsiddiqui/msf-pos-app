using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pos_app.Services;
using pos_app.Models;
using pos_app.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace pos_app.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClientDataController : ControllerBase
    {
        private readonly DataAccessService _dataAccessService;
        private readonly ClientDataService _clientDataService;
        private readonly AuthService _authService;
        private readonly MasterDbContext _masterDbContext;
        private readonly ILogger<ClientDataController> _logger;

        public ClientDataController(
            DataAccessService dataAccessService,
            ClientDataService clientDataService,
            AuthService authService,
            MasterDbContext masterDbContext,
            ILogger<ClientDataController> logger)
        {
            _dataAccessService = dataAccessService;
            _clientDataService = clientDataService;
            _authService = authService;
            _masterDbContext = masterDbContext;
            _logger = logger;
        }

        // GET: api/clientdata/dashboard
        // Refactored to use ClientDataService with EF Core
        [HttpGet("dashboard")]
        public async Task<ActionResult> GetDashboardData()
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest("No active database connection found. Please set up your database connection first.");
                }

                // Use ClientDataService which combines EF queries with raw SQL for performance
                var dashboardData = await _clientDataService.GetDashboardDataAsync(user);
                
                // Return flat structure to match frontend expectations
                return Ok(new
                {
                    TotalSales = dashboardData.TotalSales,
                    TotalPurchases = dashboardData.TotalPurchases,
                    CustomerCount = dashboardData.TotalCustomers,
                    TotalItems = dashboardData.TotalItems,
                    NetProfit = dashboardData.NetProfit,
                    MonthName = dashboardData.MonthName,
                    RecentSales = dashboardData.RecentSales
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard data");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/clientdata/customers-ef
        // NEW: Example EF-based CRUD endpoint
        [HttpGet("customers-ef")]
        public async Task<ActionResult> GetCustomersUsingEF()
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest("No active database connection found.");
                }

                var customers = await _clientDataService.GetCustomersAsync(user);
                return Ok(customers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customers using EF");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/clientdata/customers
        // NEW: Example EF-based CREATE endpoint
        [HttpPost("customers")]
        public async Task<ActionResult> CreateCustomer([FromBody] ClientCustomer customer)
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest("No active database connection found.");
                }

                var createdCustomer = await _clientDataService.CreateCustomerAsync(user, customer);
                return CreatedAtAction(nameof(GetCustomerById), new { accCode = createdCustomer.AccCode }, createdCustomer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/clientdata/customers/{accCode}
        // NEW: Example EF-based READ endpoint
        [HttpGet("customers/{accCode}")]
        public async Task<ActionResult> GetCustomerById(string accCode)
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest("No active database connection found.");
                }

                var customer = await _clientDataService.GetCustomerByIdAsync(user, accCode);
                
                if (customer == null)
                {
                    return NotFound($"Customer with code {accCode} not found");
                }

                return Ok(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer {AccCode}", accCode);
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/clientdata/customers/{accCode}
        // NEW: Example EF-based UPDATE endpoint
        [HttpPut("customers/{accCode}")]
        public async Task<ActionResult> UpdateCustomer(string accCode, [FromBody] ClientCustomer customer)
        {
            try
            {
                if (accCode != customer.AccCode)
                {
                    return BadRequest("Account code mismatch");
                }

                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest("No active database connection found.");
                }

                await _clientDataService.UpdateCustomerAsync(user, customer);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer {AccCode}", accCode);
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/clientdata/customers/{accCode}
        // NEW: Example EF-based DELETE endpoint
        [HttpDelete("customers/{accCode}")]
        public async Task<ActionResult> DeleteCustomer(string accCode)
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest("No active database connection found.");
                }

                await _clientDataService.DeleteCustomerAsync(user, accCode);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer {AccCode}", accCode);
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/clientdata/customers
        [HttpGet("customers")]
        public async Task<ActionResult> GetCustomers()
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest("No active database connection found. Please set up your database connection first.");
                }

                // Query all customers from the user's database
                var query = @"
                    SELECT 
                        acc_code as AccCode,
                        acc_name as AccName,
                        acc_type as AccType,
                        cell_no as CellNo,
                        address as Address,
                        cont_pers as ContPers,
                        gst_no as GstNo,
                        cur_bal as CurBal
                    FROM customer 
                    ORDER BY acc_code";

                var customers = await _dataAccessService.ExecuteQueryAsync(user, query);
                return Ok(customers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customers");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/clientdata/customers/simple
        [HttpGet("customers/simple")]
        public async Task<ActionResult> GetCustomersSimple()
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest("No active database connection found. Please set up your database connection first.");
                }

                // Query customers from the user's database using the correct table name
                var query = @"
                    SELECT 
                        acc_code as AccCode,
                        acc_name as AccName,
                        acc_type as AccType,
                        cell_no as CellNo,
                        address as Address,
                        cont_pers as ContPers
                    FROM customer 
                    ORDER BY acc_code";

                var customers = await _dataAccessService.ExecuteQueryAsync(user, query);
                return Ok(customers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customers simple");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/clientdata/debug/connection
        [HttpGet("debug/connection")]
        public async Task<ActionResult> DebugConnection()
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return Ok(new
                    {
                        ConnectionSuccessful = false,
                        Error = "No active database connection found",
                        UserId = userId
                    });
                }

                // Test the connection
                var isConnected = await _dataAccessService.TestConnectionAsync(user);
                
                // Get table information
                var tableQuery = @"
                    SELECT TABLE_NAME 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_TYPE = 'BASE TABLE' 
                    AND TABLE_NAME LIKE '%customer%'";
                
                var tables = await _dataAccessService.ExecuteQueryAsync(user, tableQuery);
                
                // Get customer count
                var countQuery = "SELECT COUNT(*) as CustomerCount FROM customer";
                var countResult = await _dataAccessService.ExecuteQueryAsync(user, countQuery);
                var customerCount = countResult.FirstOrDefault()?["CustomerCount"] ?? 0;

                return Ok(new
                {
                    ConnectionSuccessful = isConnected,
                    ConnectionString = user.GetConnectionString().Replace(user.DatabasePassword ?? "", "***"),
                    ServerName = user.ServerName,
                    DatabaseName = user.DatabaseName,
                    DatabaseType = user.DatabaseType.ToString(),
                    CustomerTables = tables.Select(t => t["TABLE_NAME"]).ToList(),
                    CustomerCount = customerCount,
                    UserId = userId,
                    ConnectionName = user.ConnectionName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting debug info");
                return Ok(new
                {
                    ConnectionSuccessful = false,
                    Error = ex.Message,
                    UserId = GetCurrentUserId()
                });
            }
        }

        // POST: api/clientdata/setup/test-db
        [HttpPost("setup/test-db")]
        public async Task<ActionResult> SetupTestDatabaseConnection()
        {
            try
            {
                var userId = GetCurrentUserId();

                // Get the user and update their connection settings
                var user = await _masterDbContext.Users
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                if (user != null)
                {
                    // Update user's connection to point to local FlourDb for testing
                    user.ServerName = "ABDULLAH\\SQLEXPRESS";
                    user.DatabaseName = "FlourDb";
                    user.DatabaseType = DatabaseType.SQLServer;
                    user.Username = ""; // Trusted connection
                    user.DatabasePassword = "";
                    user.Port = 1433;
                    user.ConnectionName = "Local Test Database (FlourDb)";
                    user.LastConnectedAt = null;
                    user.LastError = null;

                    _masterDbContext.Users.Update(user);
                    await _masterDbContext.SaveChangesAsync();

                    return Ok(new { 
                        message = "Test database connection setup successfully",
                        serverName = "ABDULLAH\\SQLEXPRESS",
                        databaseName = "FlourDb",
                        connectionType = "Local Test Database"
                    });
                }
                else
                {
                    return BadRequest("User not found or inactive");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting up test database connection");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/clientdata/items
        [HttpGet("items")]
        public async Task<ActionResult> GetItems()
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest("No active database connection found. Please set up your database connection first.");
                }

                // Query items from the user's database
                var query = @"
                    SELECT TOP 50
                        item_code as ItemCode,
                        item_name as ItemName,
                        item_group as ItemGroup,
                        group_name as GroupName,
                        pack_size as PackSize,
                        mr_unit as MrUnit,
                        net_wght as NetWght
                    FROM item 
                    ORDER BY item_code";

                var items = await _dataAccessService.ExecuteQueryAsync(user, query);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting items");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/clientdata/recent-sales
        [HttpGet("recent-sales")]
        public async Task<ActionResult> GetRecentSales([FromQuery] int count = 10)
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest("No active database connection found. Please set up your database connection first.");
                }

                // Query recent sales from the user's database
                var query = $@"
                    SELECT TOP {count}
                        inv_no as InvNo,
                        inv_date as InvDate,
                        inv_type as InvType,
                        ac_code as AcCode,
                        ac_name as AcName,
                        total_amount as TotalAmount,
                        net_amount as NetAmount,
                        discount as Discount
                    FROM sale_inv 
                    ORDER BY inv_date DESC";

                var sales = await _dataAccessService.ExecuteQueryAsync(user, query);
                return Ok(sales);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent sales");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/clientdata/reports/sales-summary
        [HttpGet("reports/sales-summary")]
        public async Task<ActionResult> GetSalesReportSummary([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest("No active database connection found. Please set up your database connection first.");
                }

                // Build date filter
                var dateFilter = "";
                if (fromDate.HasValue && toDate.HasValue)
                {
                    dateFilter = $"WHERE inv_date >= '{fromDate.Value:yyyy-MM-dd}' AND inv_date <= '{toDate.Value:yyyy-MM-dd}'";
                }

                // Query sales summary from the user's database
                var query = $@"
                    SELECT 
                        COUNT(*) as TotalInvoices,
                        SUM(total_amount) as TotalSales,
                        SUM(discount) as TotalDiscount,
                        SUM(net_amount) as NetSales,
                        AVG(total_amount) as AverageInvoiceValue
                    FROM sale_inv 
                    {dateFilter}";

                var report = await _dataAccessService.ExecuteQueryAsync(user, query);
                return Ok(report.FirstOrDefault());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sales report summary");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/clientdata/reports/monthly-sales/{year}
        [HttpGet("reports/monthly-sales/{year}")]
        public async Task<ActionResult> GetMonthlySalesReport(int year)
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest("No active database connection found. Please set up your database connection first.");
                }

                // Query monthly sales report from the user's database
                var query = $@"
                    SELECT 
                        MONTH(inv_date) as Month,
                        COUNT(*) as InvoiceCount,
                        SUM(total_amount) as TotalSales,
                        SUM(net_amount) as NetSales
                    FROM sale_inv 
                    WHERE YEAR(inv_date) = {year}
                    GROUP BY MONTH(inv_date)
                    ORDER BY MONTH(inv_date)";

                var report = await _dataAccessService.ExecuteQueryAsync(user, query);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating monthly sales report");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/clientdata/test-connection
        [HttpPost("test-connection")]
        public async Task<ActionResult> TestConnection()
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return Ok(new { IsConnected = false, Message = "No active database connection found" });
                }

                var isConnected = await _dataAccessService.TestConnectionAsync(user);
                return Ok(new { IsConnected = isConnected });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connection");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/clientdata/session/start-date
        [HttpGet("session/start-date")]
        public async Task<ActionResult<DateTime?>> GetSessionStartDate()
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest("No active database connection found.");
                }

                using var context = _dataAccessService.GetClientContext(user);
                
                // Get the first session record (assuming there's typically one active session)
                var session = await context.Sessions.OrderBy(s => s.StartDate).FirstOrDefaultAsync();
                
                if (session == null)
                {
                    _logger.LogWarning("No session found in database for user {UserId}", userId);
                    return Ok((DateTime?)null);
                }

                return Ok(session.StartDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting session start date");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/clientdata/item-groups
        [HttpGet("item-groups")]
        public async Task<ActionResult> GetItemGroups()
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest("No active database connection found. Please set up your database connection first.");
                }

                // Query item groups from the user's database
                var query = @"
                    SELECT 
                        RTRIM(group_code) as GroupCode,
                        RTRIM(group_name) as GroupName
                    FROM item_group
                    ORDER BY group_name";

                var itemGroups = await _dataAccessService.ExecuteQueryAsync(user, query);
                return Ok(itemGroups);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item groups");
                return StatusCode(500, "Internal server error");
            }
        }

        // Helper method to get current user ID
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                throw new UnauthorizedAccessException("User ID not found in token");
            
            return userId;
        }
    }
}