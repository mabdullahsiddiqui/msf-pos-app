using Microsoft.EntityFrameworkCore;
using pos_app.Data;
using pos_app.Models;

namespace pos_app.Services
{
    /// <summary>
    /// Service layer for client database operations
    /// Provides CRUD operations for entities and complex reporting via FromSqlRaw
    /// </summary>
    public class ClientDataService
    {
        private readonly DataAccessService _dataAccessService;
        private readonly ILogger<ClientDataService> _logger;

        public ClientDataService(
            DataAccessService dataAccessService,
            ILogger<ClientDataService> logger)
        {
            _dataAccessService = dataAccessService;
            _logger = logger;
        }

        #region Customer CRUD Operations

        /// <summary>
        /// Gets all customers for the specified user's database
        /// </summary>
        public async Task<List<ClientCustomer>> GetCustomersAsync(User user)
        {
            try
            {
                using var context = _dataAccessService.GetClientContext(user);
                return await context.Customers.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching customers for user {UserId}", user.Id);
                throw;
            }
        }

        /// <summary>
        /// Gets a customer by account code
        /// </summary>
        public async Task<ClientCustomer?> GetCustomerByIdAsync(User user, string accCode)
        {
            try
            {
                using var context = _dataAccessService.GetClientContext(user);
                return await context.Customers.FindAsync(accCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching customer {AccCode} for user {UserId}", accCode, user.Id);
                throw;
            }
        }

        /// <summary>
        /// Creates a new customer
        /// </summary>
        public async Task<ClientCustomer> CreateCustomerAsync(User user, ClientCustomer customer)
        {
            try
            {
                using var context = _dataAccessService.GetClientContextWithTracking(user);
                context.Customers.Add(customer);
                await context.SaveChangesAsync();
                _logger.LogInformation("Created customer {AccCode} for user {UserId}", customer.AccCode, user.Id);
                return customer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer for user {UserId}", user.Id);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing customer
        /// </summary>
        public async Task UpdateCustomerAsync(User user, ClientCustomer customer)
        {
            try
            {
                using var context = _dataAccessService.GetClientContextWithTracking(user);
                context.Customers.Update(customer);
                await context.SaveChangesAsync();
                _logger.LogInformation("Updated customer {AccCode} for user {UserId}", customer.AccCode, user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer {AccCode} for user {UserId}", customer.AccCode, user.Id);
                throw;
            }
        }

        /// <summary>
        /// Deletes a customer
        /// </summary>
        public async Task DeleteCustomerAsync(User user, string accCode)
        {
            try
            {
                using var context = _dataAccessService.GetClientContextWithTracking(user);
                var customer = await context.Customers.FindAsync(accCode);
                if (customer != null)
                {
                    context.Customers.Remove(customer);
                    await context.SaveChangesAsync();
                    _logger.LogInformation("Deleted customer {AccCode} for user {UserId}", accCode, user.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer {AccCode} for user {UserId}", accCode, user.Id);
                throw;
            }
        }

        #endregion

        #region Item CRUD Operations

        /// <summary>
        /// Gets all items for the specified user's database
        /// </summary>
        public async Task<List<ClientItem>> GetItemsAsync(User user)
        {
            try
            {
                using var context = _dataAccessService.GetClientContext(user);
                return await context.Items.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching items for user {UserId}", user.Id);
                throw;
            }
        }

        /// <summary>
        /// Gets an item by item code
        /// </summary>
        public async Task<ClientItem?> GetItemByIdAsync(User user, string itemCode)
        {
            try
            {
                using var context = _dataAccessService.GetClientContext(user);
                return await context.Items.FindAsync(itemCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching item {ItemCode} for user {UserId}", itemCode, user.Id);
                throw;
            }
        }

        #endregion

        #region Sale Invoice Operations

        /// <summary>
        /// Gets sale invoices with optional date filtering
        /// </summary>
        public async Task<List<ClientSaleInvoice>> GetSaleInvoicesAsync(User user, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                using var context = _dataAccessService.GetClientContext(user);
                var query = context.SaleInvoices.AsQueryable();

                if (fromDate.HasValue)
                {
                    query = query.Where(s => s.InvDate >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(s => s.InvDate <= toDate.Value);
                }

                return await query.OrderByDescending(s => s.InvDate).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching sale invoices for user {UserId}", user.Id);
                throw;
            }
        }

        /// <summary>
        /// Gets sale invoice with details
        /// </summary>
        public async Task<ClientSaleInvoice?> GetSaleInvoiceWithDetailsAsync(User user, string invNo, string invType)
        {
            try
            {
                using var context = _dataAccessService.GetClientContext(user);
                return await context.SaleInvoices
                    .FirstOrDefaultAsync(s => s.InvNo == invNo && s.InvType == invType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching sale invoice {InvNo} for user {UserId}", invNo, user.Id);
                throw;
            }
        }

        #endregion

        #region Complex Report Operations using FromSqlRaw

        /// <summary>
        /// Gets Trial Balance using raw SQL for performance
        /// Uses the View_gjournal view which is critical for ledger reports
        /// </summary>
        public async Task<List<Dictionary<string, object>>> GetTrialBalanceAsync(User user, DateTime asOfDate)
        {
            try
            {
                var dateString = asOfDate.ToString("yyyy-MM-dd");
                
                var query = $@"
                    SELECT 
                        a.acc_code as AccCode,
                        a.acc_name as AccName,
                        a.acc_type as AccType,
                        ISNULL(b.debit, 0) as Debit,
                        ISNULL(b.credit, 0) as Credit
                    FROM customer a 
                    LEFT JOIN (
                        SELECT 
                            acc_code,
                            CASE WHEN SUM(dr_amount) - SUM(cr_amount) > 0 
                                 THEN SUM(dr_amount) - SUM(cr_amount) 
                                 ELSE 0 END as debit,
                            CASE WHEN SUM(dr_amount) - SUM(cr_amount) < 0 
                                 THEN ABS(SUM(dr_amount) - SUM(cr_amount)) 
                                 ELSE 0 END as credit
                        FROM view_gjournal 
                        WHERE vouch_date <= '{dateString}'
                        GROUP BY acc_code
                    ) b ON a.acc_code = b.acc_code
                    WHERE (b.debit > 0 OR b.credit > 0)
                    ORDER BY a.acc_code";

                return await _dataAccessService.ExecuteRawSqlDynamicAsync(user, query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating trial balance for user {UserId}", user.Id);
                throw;
            }
        }

        /// <summary>
        /// Gets Cash Book report using raw SQL
        /// </summary>
        public async Task<List<Dictionary<string, object>>> GetCashBookAsync(User user, DateTime fromDate, DateTime toDate)
        {
            try
            {
                var fromDateStr = fromDate.ToString("yyyy-MM-dd");
                var toDateStr = toDate.ToString("yyyy-MM-dd");
                
                var query = $@"
                    SELECT 
                        vouch_no as VouchNo,
                        vouch_date as VouchDate,
                        acc_code as AccCode,
                        descript as Description,
                        dr_amount as DrAmount,
                        cr_amount as CrAmount
                    FROM view_gjournal
                    WHERE acc_code = (SELECT cash_ac FROM acc_desc)
                    AND vouch_date BETWEEN '{fromDateStr}' AND '{toDateStr}'
                    ORDER BY vouch_date, vouch_no";

                return await _dataAccessService.ExecuteRawSqlDynamicAsync(user, query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating cash book for user {UserId}", user.Id);
                throw;
            }
        }

        /// <summary>
        /// Gets sales summary report
        /// </summary>
        public async Task<Dictionary<string, object>?> GetSalesSummaryAsync(User user, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var dateFilter = "";
                if (fromDate.HasValue && toDate.HasValue)
                {
                    var fromDateStr = fromDate.Value.ToString("yyyy-MM-dd");
                    var toDateStr = toDate.Value.ToString("yyyy-MM-dd");
                    dateFilter = $"WHERE inv_date >= '{fromDateStr}' AND inv_date <= '{toDateStr}'";
                }

                var query = $@"
                    SELECT 
                        COUNT(*) as TotalInvoices,
                        SUM(gros_amt) as TotalSales,
                        SUM(disc_amt) as TotalDiscount,
                        SUM(net_amt) as NetSales,
                        AVG(gros_amt) as AverageInvoiceValue
                    FROM sale_inv 
                    {dateFilter}";

                var results = await _dataAccessService.ExecuteRawSqlDynamicAsync(user, query);
                return results.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sales summary for user {UserId}", user.Id);
                throw;
            }
        }

        #endregion

        #region Dashboard Operations

        /// <summary>
        /// Gets dashboard data including sales, purchases, and item counts
        /// </summary>
        public async Task<ClientDashboardData> GetDashboardDataAsync(User user)
        {
            try
            {
                using var context = _dataAccessService.GetClientContext(user);

                var dashboardData = new ClientDashboardData
                {
                    TotalCustomers = await context.Customers.CountAsync(),
                    TotalItems = await context.Items.CountAsync(),
                    RecentSales = await context.SaleInvoices
                        .OrderByDescending(s => s.InvDate)
                        .Take(10)
                        .ToListAsync()
                };

                // Get current month date range (database-agnostic approach)
                var currentMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var currentMonthEnd = currentMonthStart.AddMonths(1).AddDays(-1);
                var startDateStr = currentMonthStart.ToString("yyyy-MM-dd");
                var endDateStr = currentMonthEnd.ToString("yyyy-MM-dd");

                // Get sales and purchases totals using database-agnostic SQL
                var salesQuery = user.DatabaseType == DatabaseType.SQLServer
                    ? $"SELECT ISNULL(SUM(net_amt), 0) as Total FROM sale_inv WHERE inv_date >= '{startDateStr}' AND inv_date <= '{endDateStr}'"
                    : $"SELECT IFNULL(SUM(net_amt), 0) as Total FROM sale_inv WHERE inv_date >= '{startDateStr}' AND inv_date <= '{endDateStr}'";

                var purchasesQuery = user.DatabaseType == DatabaseType.SQLServer
                    ? $"SELECT ISNULL(SUM(net_amt), 0) as Total FROM pur_inv WHERE inv_date >= '{startDateStr}' AND inv_date <= '{endDateStr}'"
                    : $"SELECT IFNULL(SUM(net_amt), 0) as Total FROM pur_inv WHERE inv_date >= '{startDateStr}' AND inv_date <= '{endDateStr}'";

                var salesResult = await _dataAccessService.ExecuteRawSqlDynamicAsync(user, salesQuery);
                var purchasesResult = await _dataAccessService.ExecuteRawSqlDynamicAsync(user, purchasesQuery);

                if (salesResult.Any())
                {
                    dashboardData.TotalSales = Convert.ToDecimal(salesResult.First()["Total"]);
                }

                if (purchasesResult.Any())
                {
                    dashboardData.TotalPurchases = Convert.ToDecimal(purchasesResult.First()["Total"]);
                }

                dashboardData.NetProfit = dashboardData.TotalSales - dashboardData.TotalPurchases;
                dashboardData.MonthName = DateTime.Now.ToString("MMMM yyyy");

                return dashboardData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating dashboard data for user {UserId}", user.Id);
                throw;
            }
        }

        #endregion
    }
}

