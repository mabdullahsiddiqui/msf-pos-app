using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using pos_app.Services;
using pos_app.Models;
using Microsoft.Extensions.Logging;

namespace pos_app.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DataController : ControllerBase
    {
        private readonly DataAccessService _dataAccessService;
        private readonly AuthService _authService;
        private readonly ILogger<DataController> _logger;

        public DataController(
            DataAccessService dataAccessService, 
            AuthService authService,
            ILogger<DataController> logger)
        {
            _dataAccessService = dataAccessService;
            _authService = authService;
            _logger = logger;
        }

        [HttpGet("sales")]
        public async Task<ActionResult<List<Dictionary<string, object>>>> GetSales()
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest("User not found or inactive");
                }

                var query = @"
                    SELECT TOP 50 
                        SaleId,
                        SaleDate,
                        CustomerName,
                        Items,
                        Quantity,
                        UnitPrice,
                        TotalAmount,
                        PaymentMethod,
                        Status
                    FROM Sales 
                    ORDER BY SaleDate DESC";

                var sales = await _dataAccessService.ExecuteQueryAsync(user, query);
                return Ok(sales);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching sales data");
                return StatusCode(500, "Error fetching sales data");
            }
        }

        [HttpGet("inventory")]
        public async Task<ActionResult<List<Dictionary<string, object>>>> GetInventory()
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest("User not found or inactive");
                }

                var query = @"
                    SELECT 
                        ItemId,
                        ItemName,
                        Description,
                        Quantity,
                        UnitPrice,
                        Category,
                        Status
                    FROM Inventory 
                    WHERE Status = 'Active'
                    ORDER BY ItemName";

                var inventory = await _dataAccessService.ExecuteQueryAsync(user, query);
                return Ok(inventory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching inventory data");
                return StatusCode(500, "Error fetching inventory data");
            }
        }

        [HttpGet("reports/summary")]
        public async Task<ActionResult<Dictionary<string, object>>> GetSummaryReport()
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest("User not found or inactive");
                }

                var salesQuery = @"
                    SELECT 
                        COUNT(*) as TotalSales,
                        SUM(TotalAmount) as TotalRevenue
                    FROM Sales 
                    WHERE SaleDate >= DATEADD(day, -30, GETDATE())";

                var inventoryQuery = @"
                    SELECT COUNT(*) as TotalItems
                    FROM Inventory 
                    WHERE Status = 'Active'";

                var sales = await _dataAccessService.ExecuteQueryAsync(user, salesQuery);
                var inventory = await _dataAccessService.ExecuteQueryAsync(user, inventoryQuery);

                var summary = new Dictionary<string, object>
                {
                    ["TotalSales"] = sales.FirstOrDefault()?["TotalSales"] ?? 0,
                    ["TotalRevenue"] = sales.FirstOrDefault()?["TotalRevenue"] ?? 0,
                    ["TotalItems"] = inventory.FirstOrDefault()?["TotalItems"] ?? 0
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching summary report");
                return StatusCode(500, "Error fetching summary report");
            }
        }



        [HttpGet("query")]
        public async Task<ActionResult<List<Dictionary<string, object>>>> ExecuteQuery([FromQuery] string sql)
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest("User not found or inactive");
                }

                // Basic SQL injection protection - only allow SELECT statements
                if (!sql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest("Only SELECT queries are allowed");
                }

                var results = await _dataAccessService.ExecuteQueryAsync(user, sql);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing custom query");
                return StatusCode(500, "Error executing query");
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            throw new UnauthorizedAccessException("Invalid user ID");
        }
    }
}
