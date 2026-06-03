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

        /// <summary>
        /// Gets comparison data (Sales, Recovery, Expenses) for dashboard charts
        /// </summary>
        public async Task<List<DashboardComparisonData>> GetDashboardComparisonDataAsync(User user)
        {
            try
            {
                var query = @"
-- Sales Month Wise
WITH sales AS (
    SELECT 
        inv_date,
        MONTH(inv_date) AS sale_month,
        YEAR(inv_date) AS sales_year,
        gros_amt
    FROM sale_inv
),
salesMonthWise AS (
    SELECT 
        FORMAT(inv_date, 'MMM-yyyy') AS trans_month,
        sale_month,
        sales_year,
        SUM(gros_amt) AS sales
    FROM sales
    GROUP BY sale_month, sales_year, FORMAT(inv_date, 'MMM-yyyy')
),

-- Recovery
RecoveryTemp AS (
    SELECT 
        MONTH(vouch_date) AS rec_month,
        YEAR(vouch_date) AS rec_year,
        amount
    FROM crvoucher 
    WHERE ac_code IN (SELECT ac_code FROM sale_inv)

    UNION ALL

    SELECT 
        MONTH(vouch_date),
        YEAR(vouch_date),
        rec_amt
    FROM bank_receipt
    WHERE credit_ac IN (SELECT ac_code FROM sale_inv)

    UNION ALL

    SELECT 
        MONTH(vouch_date),
        YEAR(vouch_date),
        cr_amount
    FROM j_vouch
    WHERE cr_amount > 0 
      AND dr_amount = 0
      AND acc_code IN (SELECT ac_code FROM sale_inv)
),
recovery AS (
    SELECT 
        FORMAT(DATEFROMPARTS(rec_year, rec_month, 1), 'MMM-yyyy') AS trans_month,
        rec_month,
        rec_year,
        SUM(amount) AS rec_amt
    FROM RecoveryTemp
    GROUP BY rec_month, rec_year
),

-- Expenses
ExpTemp AS (
    SELECT 
        g.acc_code,
        MONTH(g.vouch_date) AS exp_month,
        YEAR(g.vouch_date) AS exp_year,
        g.dr_amount AS amount
    FROM g_journal g
    CROSS JOIN profit_loss p
    WHERE g.dr_amount > 0
      AND (
            g.acc_code BETWEEN p.ope_frm_Ac AND p.ope_to_ac
         OR g.acc_code BETWEEN p.man_frm_Ac AND p.man_to_ac
      )
),
expMonthWise AS (
    SELECT 
        FORMAT(DATEFROMPARTS(exp_year, exp_month, 1), 'MMM-yyyy') AS trans_month,
        exp_month,
        exp_year,
        SUM(amount) AS exp_amt
    FROM ExpTemp
    GROUP BY exp_month, exp_year
),

-- Combine All
Summary AS (
    SELECT 
        trans_month,
        sale_month AS nMonth,
        sales_year AS nYear,
        sales,
        0.0 AS rec_amt,
        0.0 AS exp_amt
    FROM salesMonthWise

    UNION ALL

    SELECT 
        trans_month,
        rec_month,
        rec_year,
        0.0,
        rec_amt,
        0.0
    FROM recovery

    UNION ALL

    SELECT 
        trans_month,
        exp_month,
        exp_year,
        0.0,
        0.0,
        exp_amt
    FROM expMonthWise
)

-- Final Result
SELECT 
    trans_month as TransMonth,
    SUM(sales) AS Sales,
    SUM(rec_amt) AS RecAmt,
    SUM(exp_amt) AS ExpAmt
FROM Summary
GROUP BY trans_month, nYear, nMonth
ORDER BY nYear, nMonth;";

                var results = await _dataAccessService.ExecuteRawSqlDynamicAsync(user, query);
                
                return results.Select(r => new DashboardComparisonData
                {
                    TransMonth = r["TransMonth"]?.ToString() ?? "",
                    Sales = Convert.ToDecimal(r["Sales"]),
                    RecAmt = Convert.ToDecimal(r["RecAmt"]),
                    ExpAmt = Convert.ToDecimal(r["ExpAmt"])
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating dashboard comparison data for user {UserId}", user.Id);
                throw;
            }
        }

        /// <summary>
        /// Gets production distribution data for dashboard charts
        /// </summary>
        public async Task<List<ProductionDistributionData>> GetProductionDistributionAsync(User user, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var dateFilter = "";
                if (fromDate.HasValue && toDate.HasValue)
                {
                    dateFilter = $"WHERE prod_date BETWEEN '{fromDate.Value:yyyy-MM-dd}' AND '{toDate.Value:yyyy-MM-dd}'";
                }

                var query = $@"
WITH temp AS (
    SELECT 
        LEFT(item_code, 2) AS prod_group,
        wght_prod
    FROM batch_production
    {dateFilter}
)

SELECT 
    a.prod_group,
    MAX(b.group_name) AS group_name,
    ROUND(SUM(a.wght_prod) * 100.0 / 
        (SELECT CASE WHEN SUM(wght_prod) = 0 THEN 1 ELSE SUM(wght_prod) END FROM temp), 2) AS per
FROM temp a
JOIN item_group b 
    ON a.prod_group = b.group_code
GROUP BY a.prod_group;";

                var results = await _dataAccessService.ExecuteRawSqlDynamicAsync(user, query);
                
                return results.Select(r => new ProductionDistributionData
                {
                    GroupName = r["group_name"]?.ToString() ?? "Unknown",
                    Percentage = Convert.ToDecimal(r["per"])
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating production distribution data for user {UserId}", user.Id);
                return new List<ProductionDistributionData>();
            }
        }

        /// <summary>
        /// Gets sales distribution data for dashboard charts
        /// </summary>
        public async Task<List<SalesDistributionData>> GetSalesDistributionAsync(User user, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var dateFilter = "";
                if (fromDate.HasValue && toDate.HasValue)
                {
                    dateFilter = $"WHERE s.inv_date BETWEEN '{fromDate.Value:yyyy-MM-dd}' AND '{toDate.Value:yyyy-MM-dd}'";
                }

                var query = $@"
WITH temp AS (
    SELECT 
        i.item_group,
        s.grand_tot
    FROM sub_sinv s
    JOIN item i ON s.item_code = i.item_code
    {dateFilter}
)

SELECT 
    a.item_group,
    MAX(b.group_name) AS group_name,
    ROUND(SUM(a.grand_tot) * 100.0 / 
        (SELECT CASE WHEN SUM(grand_tot) = 0 THEN 1 ELSE SUM(grand_tot) END FROM temp), 2) AS per
FROM temp a
JOIN item_group b 
    ON a.item_group = b.group_code
GROUP BY a.item_group;";

                var results = await _dataAccessService.ExecuteRawSqlDynamicAsync(user, query);
                
                return results.Select(r => new SalesDistributionData
                {
                    GroupName = r["group_name"]?.ToString() ?? "Unknown",
                    Percentage = Convert.ToDecimal(r["per"])
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sales distribution data for user {UserId}", user.Id);
                return new List<SalesDistributionData>();
            }
        }

        /// <summary>
        /// Gets the comprehensive CEO Report data (Performance Summary and Bank Balances)
        /// </summary>
        public async Task<CEOReportResponse> GetCEOReportAsync(User user)
        {
            var response = new CEOReportResponse();
            try
            {
                var today = DateTime.Today.ToString("yyyy-MM-dd");

                // 1. Performance Summary
                var perfQuery = $@"
DECLARE @tran_date DATE = '{today}'

;WITH SummaryTrans AS (
    SELECT 1 AS sr, 'Today' AS legend
    UNION ALL SELECT 2, 'Yesterday'
    UNION ALL SELECT 3, 'This Week'
    UNION ALL SELECT 4, 'Last Week'
    UNION ALL SELECT 5, 'This Month'
    UNION ALL SELECT 6, 'Last Month'
    UNION ALL SELECT 7, 'Year To Date'
),
SalesAgg AS (
    SELECT 1 AS t_id, SUM(gros_amt) AS sales FROM sale_inv WHERE inv_date = @tran_date
    UNION ALL
    SELECT 2, SUM(gros_amt) FROM sale_inv WHERE inv_date = DATEADD(DAY,-1,@tran_date)
    UNION ALL
    SELECT 3, SUM(gros_amt) FROM sale_inv WHERE DATEPART(WEEK,inv_date)=DATEPART(WEEK,@tran_date)
    UNION ALL
    SELECT 4, SUM(gros_amt) FROM sale_inv WHERE DATEPART(WEEK,inv_date)=DATEPART(WEEK,DATEADD(WEEK,-1,@tran_date))
    UNION ALL
    SELECT 5, SUM(gros_amt) FROM sale_inv WHERE MONTH(inv_date)=MONTH(@tran_date) AND YEAR(inv_date)=YEAR(@tran_date)
    UNION ALL
    SELECT 6, SUM(gros_amt) FROM sale_inv WHERE MONTH(inv_date)=MONTH(DATEADD(MONTH,-1,@tran_date)) AND YEAR(inv_date)=YEAR(DATEADD(MONTH,-1,@tran_date))
    UNION ALL
    SELECT 7, SUM(gros_amt) FROM sale_inv
),
SalesRetAgg AS (
    SELECT 1 AS t_id, SUM(gros_amt) AS sales_ret FROM saleret_inv WHERE inv_date = @tran_date
    UNION ALL
    SELECT 2, SUM(gros_amt) FROM saleret_inv WHERE inv_date = DATEADD(DAY,-1,@tran_date)
    UNION ALL
    SELECT 3, SUM(gros_amt) FROM saleret_inv WHERE DATEPART(WEEK,inv_date)=DATEPART(WEEK,@tran_date)
    UNION ALL
    SELECT 4, SUM(gros_amt) FROM saleret_inv WHERE DATEPART(WEEK,inv_date)=DATEPART(WEEK,DATEADD(WEEK,-1,@tran_date))
    UNION ALL
    SELECT 5, SUM(gros_amt) FROM saleret_inv WHERE MONTH(inv_date)=MONTH(@tran_date) AND YEAR(inv_date)=YEAR(@tran_date)
    UNION ALL
    SELECT 6, SUM(gros_amt) FROM saleret_inv WHERE MONTH(inv_date)=MONTH(DATEADD(MONTH,-1,@tran_date)) AND YEAR(inv_date)=YEAR(DATEADD(MONTH,-1,@tran_date))
    UNION ALL
    SELECT 7, SUM(gros_amt) FROM saleret_inv
),
RecoveryData AS (
    SELECT vouch_date, amount FROM crvoucher WHERE ac_code IN (SELECT ac_code FROM sale_inv)
    UNION ALL
    SELECT vouch_date, rec_amt FROM bank_receipt WHERE credit_ac IN (SELECT ac_code FROM sale_inv)
    UNION ALL
    SELECT vouch_date, cr_amount FROM j_vouch 
    WHERE cr_amount > 0 AND dr_amount = 0 AND acc_code IN (SELECT ac_code FROM sale_inv)
),
RecoveryAgg AS (
    SELECT 1 AS t_id, SUM(amount) AS recovery FROM RecoveryData WHERE vouch_date=@tran_date
    UNION ALL
    SELECT 2, SUM(amount) FROM RecoveryData WHERE vouch_date=DATEADD(DAY,-1,@tran_date)
    UNION ALL
    SELECT 3, SUM(amount) FROM RecoveryData WHERE DATEPART(WEEK,vouch_date)=DATEPART(WEEK,@tran_date)
    UNION ALL
    SELECT 4, SUM(amount) FROM RecoveryData WHERE DATEPART(WEEK,vouch_date)=DATEPART(WEEK,DATEADD(WEEK,-1,@tran_date))
    UNION ALL
    SELECT 5, SUM(amount) FROM RecoveryData WHERE MONTH(vouch_date)=MONTH(@tran_date) AND YEAR(vouch_date)=YEAR(@tran_date)
    UNION ALL
    SELECT 6, SUM(amount) FROM RecoveryData WHERE MONTH(vouch_date)=MONTH(DATEADD(MONTH,-1,@tran_date)) AND YEAR(vouch_date)=YEAR(DATEADD(MONTH,-1,@tran_date))
    UNION ALL
    SELECT 7, SUM(amount) FROM RecoveryData
),
PurchaseAgg AS (
    SELECT 1 AS t_id, SUM(gross_amt) AS purchase FROM pur_inv WHERE inv_date=@tran_date
    UNION ALL
    SELECT 2, SUM(gross_amt) FROM pur_inv WHERE inv_date=DATEADD(DAY,-1,@tran_date)
    UNION ALL
    SELECT 3, SUM(gross_amt) FROM pur_inv WHERE DATEPART(WEEK,inv_date)=DATEPART(WEEK,@tran_date)
    UNION ALL
    SELECT 4, SUM(gross_amt) FROM pur_inv WHERE DATEPART(WEEK,inv_date)=DATEPART(WEEK,DATEADD(WEEK,-1,@tran_date))
    UNION ALL
    SELECT 5, SUM(gross_amt) FROM pur_inv WHERE MONTH(inv_date)=MONTH(@tran_date) AND YEAR(inv_date)=YEAR(@tran_date)
    UNION ALL
    SELECT 6, SUM(gross_amt) FROM pur_inv WHERE MONTH(inv_date)=MONTH(DATEADD(MONTH,-1,@tran_date)) AND YEAR(inv_date)=YEAR(DATEADD(MONTH,-1,@tran_date))
    UNION ALL
    SELECT 7, SUM(gross_amt) FROM pur_inv
)

SELECT 
    a.sr,
    a.legend,
    ISNULL(b.sales,0) AS sales,
    ISNULL(c.sales_ret,0) AS sales_ret,
    ISNULL(b.sales,0) - ISNULL(c.sales_ret,0) AS net_sales,
    ISNULL(d.recovery,0) AS recovery,
    ISNULL(e.purchase,0) AS purchase
FROM SummaryTrans a
LEFT JOIN SalesAgg b ON a.sr=b.t_id
LEFT JOIN SalesRetAgg c ON a.sr=c.t_id
LEFT JOIN RecoveryAgg d ON a.sr=d.t_id
LEFT JOIN PurchaseAgg e ON a.sr=e.t_id
ORDER BY sr;";

                var perfResults = await _dataAccessService.ExecuteRawSqlDynamicAsync(user, perfQuery);
                response.PerformanceSummary = perfResults.Select(r => new CEOSummaryData
                {
                    Sr = Convert.ToInt32(r["sr"]),
                    Legend = r["legend"]?.ToString() ?? "",
                    Sales = Convert.ToDecimal(r["sales"]),
                    SalesRet = Convert.ToDecimal(r["sales_ret"]),
                    NetSales = Convert.ToDecimal(r["net_sales"]),
                    Recovery = Convert.ToDecimal(r["recovery"]),
                    Purchase = Convert.ToDecimal(r["purchase"])
                }).ToList();

                // 2. Bank Summary (Current Month)
                var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).ToString("yyyy-MM-dd");
                var monthEnd = DateTime.Today.ToString("yyyy-MM-dd");

                var bankQuery = $@"
DECLARE @fromdate DATE = '{monthStart}', @uptodate DATE = '{monthEnd}';
DECLARE @fromac nvarchar(12) = (select bank_from from acc_Desc);
DECLARE @uptoac nvarchar(12) = (select bank_to from acc_desc);

;WITH acList AS (
    SELECT acc_code, acc_name
    FROM customer
    WHERE acc_code BETWEEN @fromac AND @uptoac
),
prevBal AS (
    SELECT 
        acc_code,
        ISNULL(SUM(dr_amount) - SUM(cr_amount), 0) AS prev_balance,
        CASE 
            WHEN ISNULL(SUM(dr_amount) - SUM(cr_amount), 0) > 0 THEN 'Dr.'
            ELSE 'Cr.'
        END AS prev_type
    FROM g_journal
    WHERE acc_code BETWEEN @fromac AND @uptoac
      AND vouch_date < @fromdate
    GROUP BY acc_code
),
currentBal AS (
    SELECT 
        acc_code,
        SUM(dr_amount) AS debit,
        SUM(cr_amount) AS credit
    FROM g_journal
    WHERE vouch_date BETWEEN @fromdate AND @uptodate
      AND acc_code BETWEEN @fromac AND @uptoac
    GROUP BY acc_code
)

SELECT * FROM (
    SELECT 
        a.acc_code, a.acc_name,
        ISNULL(b.prev_balance,0) AS prev_balance,
        CASE WHEN ISNULL(b.prev_balance,0) = 0 THEN '' ELSE b.prev_type END AS prev_type,
        ISNULL(c.debit,0) AS debit, ISNULL(c.credit,0) AS credit,
        (ISNULL(b.prev_balance,0) + ISNULL(c.debit,0)) - ISNULL(c.credit,0) AS cur_bal,
        CASE WHEN (ISNULL(b.prev_balance,0) + ISNULL(c.debit,0) - ISNULL(c.credit,0)) > 0 THEN 'Dr.' ELSE 'Cr.' END AS cur_type
    FROM acList a
    LEFT JOIN prevBal b ON a.acc_code = b.acc_code
    LEFT JOIN currentBal c ON a.acc_code = c.acc_code
) FinalData
WHERE NOT (prev_balance = 0 AND debit = 0 AND credit = 0 AND cur_bal = 0)
ORDER BY acc_code;";

                var bankResults = await _dataAccessService.ExecuteRawSqlDynamicAsync(user, bankQuery);
                response.BankSummary = bankResults.Select(r => new CEOBankSummaryData
                {
                    AccCode = r["acc_code"]?.ToString() ?? "",
                    AccName = r["acc_name"]?.ToString() ?? "",
                    PrevBalance = Math.Abs(Convert.ToDecimal(r["prev_balance"])),
                    PrevType = r["prev_type"]?.ToString() ?? "",
                    Debit = Convert.ToDecimal(r["debit"]),
                    Credit = Convert.ToDecimal(r["credit"]),
                    CurBal = Math.Abs(Convert.ToDecimal(r["cur_bal"])),
                    CurType = r["cur_type"]?.ToString() ?? ""
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating CEO report for user {UserId}", user.Id);
            }
            return response;
        }

        #endregion
    }
}

