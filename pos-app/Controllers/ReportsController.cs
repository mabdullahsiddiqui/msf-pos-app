using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using pos_app.Models;
using pos_app.Services;
using pos_app.Data;
using System.Security.Claims;
using System.Diagnostics;

namespace pos_app.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly DataAccessService _dataAccessService;
        private readonly ClientDataService _clientDataService;
        private readonly AuthService _authService;
        private readonly MasterDbContext _masterDbContext;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(
            DataAccessService dataAccessService,
            ClientDataService clientDataService,
            AuthService authService,
            MasterDbContext masterDbContext,
            ILogger<ReportsController> logger)
        {
            _dataAccessService = dataAccessService;
            _clientDataService = clientDataService;
            _authService = authService;
            _masterDbContext = masterDbContext;
            _logger = logger;
        }

        /* 
         * ARCHITECTURE NOTE - Reports Implementation Strategy
         * 
         * This controller uses a hybrid approach for reports:
         * 
         * 1. COMPLEX REPORTS (Trial Balance, Monthly Account Balance, etc.):
         *    - Use raw SQL directly via DataAccessService.ExecuteQueryAsync()
         *    - These reports have complex hierarchical aggregations, temp tables, and SQL-specific logic
         *    - Maintaining them in SQL provides better performance and clarity
         *    - Examples: GetTrialBalanceCSharp, GetMonthlyAccountBalance, GetThreeTrialBalance
         * 
         * 2. SIMPLE REPORTS:
         *    - Can use ClientDataService which wraps FromSqlRaw() for cleaner architecture
         *    - Provides better code organization and reusability
         *    - Example: Sales Summary, simple data aggregations
         * 
         * 3. FUTURE REPORTS:
         *    - For new simple reports, prefer using ClientDataService methods
         *    - For complex reports with SQL-specific features, continue using DataAccessService
         * 
         * This follows the hybrid EF Core approach:
         * - EF Core for CRUD operations (type-safe, IntelliSense)
         * - Raw SQL for complex reporting (performance, SQL features)
         */
        
        /* 
         * COMMENTED OUT - Simple Trial Balance (kept for reference)
         * This is the old simple implementation without hierarchical aggregation.
         * Now using GetTrialBalanceCSharp as the primary endpoint.
         * Uncomment if needed for backward compatibility.
         */
        /*
        // Traditional Trial Balance Report
        [HttpGet("trial-balance")]
        public async Task<ActionResult<TrialBalanceResponse>> GetTrialBalance([FromQuery] DateTime? asOfDate = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest(new TrialBalanceResponse
                    {
                        Success = false,
                        Message = "No active database connection found. Please set up your database connection first."
                    });
                }

                // Use provided date or default to current date
                var reportDate = asOfDate ?? DateTime.Now;
                var dateString = reportDate.ToString("yyyy/MM/dd");

                // Traditional Trial Balance SQL query
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
                    ) as b ON a.acc_code = b.acc_code
                    ORDER BY a.acc_code";

                var results = await _dataAccessService.ExecuteQueryAsync(user, query);
                
                var trialBalanceItems = new List<TrialBalanceItem>();
                decimal totalDebit = 0;
                decimal totalCredit = 0;

                foreach (var row in results)
                {
                    var item = new TrialBalanceItem
                    {
                        AccCode = row.ContainsKey("AccCode") ? row["AccCode"]?.ToString() ?? "" : "",
                        AccName = row.ContainsKey("AccName") ? row["AccName"]?.ToString() ?? "" : "",
                        AccType = row.ContainsKey("AccType") ? row["AccType"]?.ToString() ?? "" : "",
                        Debit = row.ContainsKey("Debit") && decimal.TryParse(row["Debit"]?.ToString(), out var debit) ? debit : 0,
                        Credit = row.ContainsKey("Credit") && decimal.TryParse(row["Credit"]?.ToString(), out var credit) ? credit : 0
                    };

                    trialBalanceItems.Add(item);
                    totalDebit += item.Debit;
                    totalCredit += item.Credit;
                }

                return Ok(new TrialBalanceResponse
                {
                    Success = true,
                    Message = "Trial Balance retrieved successfully",
                    Data = trialBalanceItems,
                    AsOfDate = reportDate,
                    TotalDebit = totalDebit,
                    TotalCredit = totalCredit
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trial balance");
                return StatusCode(500, new TrialBalanceResponse
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }
        */

        // Hierarchical Trial Balance Report - C# Logic Implementation
        [HttpGet("trial-balance-csharp")]
        public async Task<ActionResult<TrialBalanceResponse>> GetTrialBalanceCSharp([FromQuery] DateTime? asOfDate = null)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest(new TrialBalanceResponse
                    {
                        Success = false,
                        Message = "No active database connection found. Please set up your database connection first."
                    });
                }

                // Use provided date or default to current date
                var reportDate = asOfDate ?? DateTime.Now;
                var dateString = reportDate.ToString("yyyy/MM/dd");

                // Step 1: We'll get all accounts in the next query

                // Step 2: Get all account balances (both Group and Detail) with numeric account codes
                var allAccountsWithBalancesQuery = $@"
                    SELECT 
                        a.acc_code as AccCode,
                        a.acc_name as AccName,
                        a.acc_type as AccType,
                        CAST(REPLACE(a.acc_code, '-', '') AS BIGINT) as AccCodeNumeric,
                        CASE WHEN SUM(ISNULL(dr_amount, 0)) - SUM(ISNULL(cr_amount, 0)) > 0 
                             THEN SUM(ISNULL(dr_amount, 0)) - SUM(ISNULL(cr_amount, 0)) 
                             ELSE 0 END as Debit,
                        CASE WHEN SUM(ISNULL(dr_amount, 0)) - SUM(ISNULL(cr_amount, 0)) < 0 
                             THEN ABS(SUM(ISNULL(dr_amount, 0)) - SUM(ISNULL(cr_amount, 0))) 
                             ELSE 0 END as Credit
                    FROM customer a 
                    LEFT JOIN view_gjournal b ON a.acc_code = b.acc_code 
                        AND b.vouch_date <= '{dateString}'
                    GROUP BY a.acc_code, a.acc_name, a.acc_type
                    ORDER BY a.acc_code";

                var allAccountsWithBalances = await _dataAccessService.ExecuteQueryAsync(user, allAccountsWithBalancesQuery);

                // Step 3: Calculate hierarchical sums using LINQ-based cascading approach
                var accountData = new List<(string AccCode, string AccName, string AccType, long AccCodeNumeric, decimal Debit, decimal Credit)>();

                // Initialize accountData directly using LINQ
                accountData = allAccountsWithBalances
                    .Select(row => (
                        AccCode: row["AccCode"]?.ToString() ?? "",
                        AccName: row["AccName"]?.ToString() ?? "",
                        AccType: row["AccType"]?.ToString() ?? "",
                        AccCodeNumeric: long.TryParse(row["AccCodeNumeric"]?.ToString(), out var n) ? n : 0,
                        Debit: decimal.TryParse(row["Debit"]?.ToString(), out var d) ? d : 0,
                        Credit: decimal.TryParse(row["Credit"]?.ToString(), out var c) ? c : 0
                    ))
                    .Where(a => !string.IsNullOrEmpty(a.AccCode))
                    .ToList();

                // Loop 1 - Process 3rd Tyre: Sum 4th tyre (Detail) accounts into 3rd tyre groups
                var thirdTyreAccounts = accountData.Where(a => 
                    a.AccCodeNumeric % 10000 == 0 && // Account code is a multiple of 10,000
                    a.AccCodeNumeric % 1000000 != 0 && // But NOT a multiple of 1,000,000 (so not a 2nd tyre group)
                    a.AccCodeNumeric % 100000000 != 0 // And NOT a multiple of 100,000,000 (so not a 1st tyre group)
                ).ToList();

                for (int i = 0; i < thirdTyreAccounts.Count; i++)
                {
                    var account = thirdTyreAccounts[i];
                    var matchingAccounts = accountData.Where(d => 
                        d.AccCodeNumeric >= account.AccCodeNumeric && 
                        d.AccCodeNumeric < account.AccCodeNumeric + 10000 &&
                        d.AccCodeNumeric % 10000 != 0).ToList(); // 4th tyre (Detail)

                    var sumDebit = matchingAccounts.Sum(d => d.Debit);
                    var sumCredit = matchingAccounts.Sum(d => d.Credit);

                    // Update the account in the main list
                    var accountIndex = accountData.FindIndex(a => a.AccCode == account.AccCode);
                    if (accountIndex >= 0)
                    {
                        accountData[accountIndex] = (account.AccCode, account.AccName, account.AccType, account.AccCodeNumeric, sumDebit, sumCredit);
                    }
                }

                // Loop 2 - Process 2nd Tyre: Sum 3rd tyre groups into 2nd tyre groups
                var secondTyreAccounts = accountData.Where(a => 
                    a.AccCodeNumeric % 1000000 == 0 && 
                    a.AccCodeNumeric % 100000000 != 0).ToList();

                for (int i = 0; i < secondTyreAccounts.Count; i++)
                {
                    var account = secondTyreAccounts[i];
                    var matchingAccounts = accountData.Where(t => 
                        t.AccCodeNumeric >= account.AccCodeNumeric && 
                        t.AccCodeNumeric < account.AccCodeNumeric + 1000000 &&
                        t.AccCodeNumeric % 10000 == 0 && 
                        t.AccCodeNumeric % 1000000 != 0).ToList(); // 3rd tyre

                    var sumDebit = matchingAccounts.Sum(t => t.Debit);
                    var sumCredit = matchingAccounts.Sum(t => t.Credit);

                    // Update the account in the main list
                    var accountIndex = accountData.FindIndex(a => a.AccCode == account.AccCode);
                    if (accountIndex >= 0)
                    {
                        accountData[accountIndex] = (account.AccCode, account.AccName, account.AccType, account.AccCodeNumeric, sumDebit, sumCredit);
                    }
                }

                // Loop 3 - Process 1st Tyre: Sum 2nd tyre groups into 1st tyre groups
                var firstTyreAccounts = accountData.Where(a => a.AccCodeNumeric % 100000000 == 0).ToList();

                for (int i = 0; i < firstTyreAccounts.Count; i++)
                {
                    var account = firstTyreAccounts[i];
                    var matchingAccounts = accountData.Where(s => 
                        s.AccCodeNumeric >= account.AccCodeNumeric && 
                        s.AccCodeNumeric < account.AccCodeNumeric + 100000000 &&
                        s.AccCodeNumeric % 1000000 == 0 && 
                        s.AccCodeNumeric % 100000000 != 0).ToList(); // 2nd tyre

                    var sumDebit = matchingAccounts.Sum(s => s.Debit);
                    var sumCredit = matchingAccounts.Sum(s => s.Credit);

                    // Update the account in the main list
                    var accountIndex = accountData.FindIndex(a => a.AccCode == account.AccCode);
                    if (accountIndex >= 0)
                    {
                        accountData[accountIndex] = (account.AccCode, account.AccName, account.AccType, account.AccCodeNumeric, sumDebit, sumCredit);
                    }
                }

                // Step 4: Create final trial balance items using LINQ
                var trialBalanceItems = accountData
                    .Where(a => a.Debit > 0 || a.Credit > 0)
                    .OrderBy(a => a.AccCodeNumeric)
                    .Select(a => new TrialBalanceItem
                    {
                        AccCode = a.AccCode,
                        AccName = a.AccName,
                        AccType = a.AccType,
                        Debit = a.Debit,
                        Credit = a.Credit
                    }).ToList();

                var totalDebit = accountData.Sum(a => a.Debit);
                var totalCredit = accountData.Sum(a => a.Credit);


                stopwatch.Stop();
                return Ok(new TrialBalanceResponse
                {
                    Success = true,
                    Message = "Hierarchical Trial Balance (C# Logic) retrieved successfully",
                    Data = trialBalanceItems.OrderBy(x => x.AccCode).ToList(),
                    AsOfDate = reportDate,
                    TotalDebit = totalDebit,
                    TotalCredit = totalCredit,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error getting hierarchical trial balance (C# Logic)");
                return StatusCode(500, new TrialBalanceResponse
                {
                    Success = false,
                    Message = "Internal server error",
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                });
            }
        }

        // Hierarchical Trial Balance Report - SQL Implementation (with LINQ aggregation)
        // NOTE: This endpoint uses the same LINQ-based logic as C# Logic for comparison purposes
        // It's kept available for developers but not exposed in the UI by default
        [HttpGet("trial-balance-sql")]
        public async Task<ActionResult<TrialBalanceResponse>> GetTrialBalanceSql([FromQuery] DateTime? asOfDate = null)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest(new TrialBalanceResponse
                    {
                        Success = false,
                        Message = "No active database connection found. Please set up your database connection first."
                    });
                }

                // Use provided date or default to current date
                var reportDate = asOfDate ?? DateTime.Now;
                var dateString = reportDate.ToString("yyyy/MM/dd");

                // Step 2: Get all account balances (both Group and Detail) with numeric account codes
                var allAccountsWithBalancesQuery = $@"
                    SELECT 
                        a.acc_code as AccCode,
                        a.acc_name as AccName,
                        a.acc_type as AccType,
                        CAST(REPLACE(a.acc_code, '-', '') AS BIGINT) as AccCodeNumeric,
                        CASE WHEN SUM(ISNULL(dr_amount, 0)) - SUM(ISNULL(cr_amount, 0)) > 0 
                             THEN SUM(ISNULL(dr_amount, 0)) - SUM(ISNULL(cr_amount, 0)) 
                             ELSE 0 END as Debit,
                        CASE WHEN SUM(ISNULL(dr_amount, 0)) - SUM(ISNULL(cr_amount, 0)) < 0 
                             THEN ABS(SUM(ISNULL(dr_amount, 0)) - SUM(ISNULL(cr_amount, 0))) 
                             ELSE 0 END as Credit
                    FROM customer a 
                    LEFT JOIN view_gjournal b ON a.acc_code = b.acc_code 
                        AND b.vouch_date <= '{dateString}'
                    GROUP BY a.acc_code, a.acc_name, a.acc_type
                    ORDER BY a.acc_code";

                var allAccountsWithBalances = await _dataAccessService.ExecuteQueryAsync(user, allAccountsWithBalancesQuery);

                // Step 3: Calculate hierarchical sums using LINQ-based cascading approach
                var accountData = new List<(string AccCode, string AccName, string AccType, long AccCodeNumeric, decimal Debit, decimal Credit)>();

                // Initialize accountData directly using LINQ
                accountData = allAccountsWithBalances
                    .Select(row => (
                        AccCode: row["AccCode"]?.ToString() ?? "",
                        AccName: row["AccName"]?.ToString() ?? "",
                        AccType: row["AccType"]?.ToString() ?? "",
                        AccCodeNumeric: long.TryParse(row["AccCodeNumeric"]?.ToString(), out var n) ? n : 0,
                        Debit: decimal.TryParse(row["Debit"]?.ToString(), out var d) ? d : 0,
                        Credit: decimal.TryParse(row["Credit"]?.ToString(), out var c) ? c : 0
                    ))
                    .Where(a => !string.IsNullOrEmpty(a.AccCode))
                    .ToList();

                // Loop 1 - Process 3rd Tyre: Sum 4th tyre (Detail) accounts into 3rd tyre groups
                var thirdTyreAccounts = accountData.Where(a => 
                    a.AccCodeNumeric % 10000 == 0 && // Account code is a multiple of 10,000
                    a.AccCodeNumeric % 1000000 != 0 && // But NOT a multiple of 1,000,000 (so not a 2nd tyre group)
                    a.AccCodeNumeric % 100000000 != 0 // And NOT a multiple of 100,000,000 (so not a 1st tyre group)
                ).ToList();

                for (int i = 0; i < thirdTyreAccounts.Count; i++)
                {
                    var account = thirdTyreAccounts[i];
                    var matchingAccounts = accountData.Where(d => 
                        d.AccCodeNumeric >= account.AccCodeNumeric && 
                        d.AccCodeNumeric < account.AccCodeNumeric + 10000 &&
                        d.AccCodeNumeric % 10000 != 0).ToList(); // 4th tyre (Detail)

                    var sumDebit = matchingAccounts.Sum(d => d.Debit);
                    var sumCredit = matchingAccounts.Sum(d => d.Credit);

                    // Update the account in the main list
                    var accountIndex = accountData.FindIndex(a => a.AccCode == account.AccCode);
                    if (accountIndex >= 0)
                    {
                        accountData[accountIndex] = (account.AccCode, account.AccName, account.AccType, account.AccCodeNumeric, sumDebit, sumCredit);
                    }
                }

                // Loop 2 - Process 2nd Tyre: Sum 3rd tyre groups into 2nd tyre groups
                var secondTyreAccounts = accountData.Where(a => 
                    a.AccCodeNumeric % 1000000 == 0 && 
                    a.AccCodeNumeric % 100000000 != 0).ToList();

                for (int i = 0; i < secondTyreAccounts.Count; i++)
                {
                    var account = secondTyreAccounts[i];
                    var matchingAccounts = accountData.Where(t => 
                        t.AccCodeNumeric >= account.AccCodeNumeric && 
                        t.AccCodeNumeric < account.AccCodeNumeric + 1000000 &&
                        t.AccCodeNumeric % 10000 == 0 && 
                        t.AccCodeNumeric % 1000000 != 0).ToList(); // 3rd tyre

                    var sumDebit = matchingAccounts.Sum(t => t.Debit);
                    var sumCredit = matchingAccounts.Sum(t => t.Credit);

                    // Update the account in the main list
                    var accountIndex = accountData.FindIndex(a => a.AccCode == account.AccCode);
                    if (accountIndex >= 0)
                    {
                        accountData[accountIndex] = (account.AccCode, account.AccName, account.AccType, account.AccCodeNumeric, sumDebit, sumCredit);
                    }
                }

                // Loop 3 - Process 1st Tyre: Sum 2nd tyre groups into 1st tyre groups
                var firstTyreAccounts = accountData.Where(a => a.AccCodeNumeric % 100000000 == 0).ToList();

                for (int i = 0; i < firstTyreAccounts.Count; i++)
                {
                    var account = firstTyreAccounts[i];
                    var matchingAccounts = accountData.Where(s => 
                        s.AccCodeNumeric >= account.AccCodeNumeric && 
                        s.AccCodeNumeric < account.AccCodeNumeric + 100000000 &&
                        s.AccCodeNumeric % 1000000 == 0 && 
                        s.AccCodeNumeric % 100000000 != 0).ToList(); // 2nd tyre

                    var sumDebit = matchingAccounts.Sum(s => s.Debit);
                    var sumCredit = matchingAccounts.Sum(s => s.Credit);

                    // Update the account in the main list
                    var accountIndex = accountData.FindIndex(a => a.AccCode == account.AccCode);
                    if (accountIndex >= 0)
                    {
                        accountData[accountIndex] = (account.AccCode, account.AccName, account.AccType, account.AccCodeNumeric, sumDebit, sumCredit);
                    }
                }

                // Step 4: Create final trial balance items using LINQ
                var trialBalanceItems = accountData
                    .OrderBy(a => a.AccCodeNumeric)
                    .Select(a => new TrialBalanceItem
                    {
                        AccCode = a.AccCode,
                        AccName = a.AccName,
                        AccType = a.AccType,
                        Debit = a.Debit,
                        Credit = a.Credit
                    }).ToList();

                var totalDebit = accountData.Sum(a => a.Debit);
                var totalCredit = accountData.Sum(a => a.Credit);

                stopwatch.Stop();
                return Ok(new TrialBalanceResponse
                {
                    Success = true,
                    Message = "Hierarchical Trial Balance (SQL Logic) retrieved successfully",
                    Data = trialBalanceItems,
                    AsOfDate = reportDate,
                    TotalDebit = totalDebit,
                    TotalCredit = totalCredit,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error getting hierarchical trial balance (SQL Logic)");
                return StatusCode(500, new TrialBalanceResponse
                {
                    Success = false,
                    Message = "Internal server error",
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                });
            }
        }


        // Monthly Account Balance Report
        [HttpGet("monthly-account-balance")]
        public async Task<ActionResult<MonthlyAccountBalanceResponse>> GetMonthlyAccountBalance(
            [FromQuery] DateTime? fromDate = null, 
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? fromAccount = null,
            [FromQuery] string? uptoAccount = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest(new MonthlyAccountBalanceResponse
                    {
                        Success = false,
                        Message = "No active database connection found. Please set up your database connection first."
                    });
                }

                // Default account range if not provided
                var fromAcc = fromAccount ?? "1-01-00-0000";
                var uptoAcc = uptoAccount ?? "9-99-99-9999";

                // Convert account codes to numeric for range filtering
                var fromAccountNumeric = long.Parse(fromAcc.Replace("-", ""));
                var uptoAccountNumeric = long.Parse(uptoAcc.Replace("-", ""));

                // Calculate dates from voucher data if not provided
                DateTime from;
                DateTime to;
                
                if (fromDate.HasValue && toDate.HasValue)
                {
                    // Use provided dates
                    from = fromDate.Value;
                    to = toDate.Value;
                }
                else
                {
                    // Query for min/max voucher dates for the account range (only type 'D')
                    var dateQuery = $@"
                        SELECT 
                            MIN(v.vouch_date) as MinDate,
                            MAX(v.vouch_date) as MaxDate
                        FROM view_gjournal v
                        INNER JOIN customer a ON v.acc_code = a.acc_code
                        WHERE CAST(REPLACE(v.acc_code, '-', '') AS BIGINT) >= {fromAccountNumeric}
                          AND CAST(REPLACE(v.acc_code, '-', '') AS BIGINT) <= {uptoAccountNumeric}
                          AND a.acc_type = 'D'";

                    var dateResults = await _dataAccessService.ExecuteQueryAsync(user, dateQuery);
                    
                    if (dateResults != null && dateResults.Any())
                    {
                        var firstRow = dateResults.First();
                        var minDateStr = firstRow.ContainsKey("MinDate") ? firstRow["MinDate"]?.ToString() : null;
                        var maxDateStr = firstRow.ContainsKey("MaxDate") ? firstRow["MaxDate"]?.ToString() : null;
                        
                        if (!string.IsNullOrEmpty(minDateStr) && DateTime.TryParse(minDateStr, out var minDate) &&
                            !string.IsNullOrEmpty(maxDateStr) && DateTime.TryParse(maxDateStr, out var maxDate))
                        {
                            from = minDate;
                            to = maxDate;
                        }
                        else
                        {
                            // No vouchers found, default to current month
                            from = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                            to = DateTime.Now;
                        }
                    }
                    else
                    {
                        // No vouchers found, default to current month
                        from = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                        to = DateTime.Now;
                    }
                }
                
                var fromDateString = from.ToString("yyyy/MM/dd");
                var toDateString = to.ToString("yyyy/MM/dd");

                // Generate month columns dynamically
                var monthColumns = new List<string>();
                var currentMonth = new DateTime(from.Year, from.Month, 1);
                var endMonth = new DateTime(to.Year, to.Month, 1);
                
                while (currentMonth <= endMonth)
                {
                    monthColumns.Add(currentMonth.ToString("MMM yyyy"));
                    currentMonth = currentMonth.AddMonths(1);
                }

                // Monthly Account Balance SQL query (only type 'D')
                var query = $@"
                    SELECT 
                        a.acc_code as AccCode,
                        a.acc_name as AccName,
                        FORMAT(v.vouch_date, 'MMM yyyy') as MonthYear,
                        SUM(ISNULL(v.dr_amount, 0)) - SUM(ISNULL(v.cr_amount, 0)) as Balance
                    FROM customer a 
                    LEFT JOIN view_gjournal v ON a.acc_code = v.acc_code 
                        AND v.vouch_date >= '{fromDateString}' 
                        AND v.vouch_date <= '{toDateString}'
                    WHERE CAST(REPLACE(a.acc_code, '-', '') AS BIGINT) >= {fromAccountNumeric}
                      AND CAST(REPLACE(a.acc_code, '-', '') AS BIGINT) <= {uptoAccountNumeric}
                      AND a.acc_type = 'D'
                    GROUP BY a.acc_code, a.acc_name, FORMAT(v.vouch_date, 'MMM yyyy')
                    ORDER BY a.acc_code";

                var results = await _dataAccessService.ExecuteQueryAsync(user, query);
                
                // Process results into structured data
                var accountBalances = new Dictionary<string, MonthlyAccountBalanceItem>();
                
                foreach (var row in results)
                {
                    var accCode = row.ContainsKey("AccCode") ? row["AccCode"]?.ToString() ?? "" : "";
                    var accName = row.ContainsKey("AccName") ? row["AccName"]?.ToString() ?? "" : "";
                    var monthYear = row.ContainsKey("MonthYear") ? row["MonthYear"]?.ToString() ?? "" : "";
                    var balance = row.ContainsKey("Balance") && decimal.TryParse(row["Balance"]?.ToString(), out var bal) ? bal : 0;

                    if (string.IsNullOrEmpty(accCode)) continue;

                    if (!accountBalances.ContainsKey(accCode))
                    {
                        accountBalances[accCode] = new MonthlyAccountBalanceItem
                        {
                            AccCode = accCode,
                            AccName = accName,
                            MonthlyBalances = new Dictionary<string, decimal>(),
                            Total = 0
                        };
                    }

                    if (!string.IsNullOrEmpty(monthYear))
                    {
                        accountBalances[accCode].MonthlyBalances[monthYear] = balance;
                        accountBalances[accCode].Total += balance;
                    }
                }

                // Ensure all accounts have entries for all months (with 0 values)
                var monthlyAccountBalanceItems = new List<MonthlyAccountBalanceItem>();
                foreach (var account in accountBalances.Values)
                {
                    foreach (var month in monthColumns)
                    {
                        if (!account.MonthlyBalances.ContainsKey(month))
                        {
                            account.MonthlyBalances[month] = 0;
                        }
                    }
                    monthlyAccountBalanceItems.Add(account);
                }

                return Ok(new MonthlyAccountBalanceResponse
                {
                    Success = true,
                    Message = "Monthly Account Balance retrieved successfully",
                    Data = monthlyAccountBalanceItems.OrderBy(x => x.AccCode).ToList(),
                    MonthColumns = monthColumns,
                    FromDate = from,
                    ToDate = to,
                    FromAccount = fromAcc,
                    UptoAccount = uptoAcc
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting monthly account balance");
                return StatusCode(500, new MonthlyAccountBalanceResponse
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        // 3 Trial Balance Report
        [HttpGet("three-trial-balance")]
        public async Task<ActionResult<ThreeTrialBalanceResponse>> GetThreeTrialBalance(
            [FromQuery] DateTime? fromDate = null, 
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? fromAccount = null,
            [FromQuery] string? uptoAccount = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest(new ThreeTrialBalanceResponse
                    {
                        Success = false,
                        Message = "No active database connection found. Please set up your database connection first."
                    });
                }

                // Use provided dates or default to current date
                var from = fromDate ?? DateTime.Now;
                var to = toDate ?? DateTime.Now;
                var fromDateString = from.ToString("yyyy/MM/dd");
                var toDateString = to.ToString("yyyy/MM/dd");

                // Default account range if not provided
                var fromAcc = fromAccount ?? "1-01-01-0000";
                var uptoAcc = uptoAccount ?? "1-01-01-9999";

                // Convert account codes to numeric for range filtering
                var fromAccountNumeric = long.Parse(fromAcc.Replace("-", ""));
                var uptoAccountNumeric = long.Parse(uptoAcc.Replace("-", ""));

                // 3 Trial Balance SQL query
                // Start from customer table to include all accounts, even those with no transactions before fromDate
                var query = $@"
                    SELECT c.acc_code as AccCode, c.acc_name as AccName, 
                           ISNULL(a.prev_bal, 0) as PrevBal, 
                           ISNULL(a.bal_type, 'Cr.') as BalType,
                           ISNULL(b.cur_debit,0) as CurDebit, 
                           ISNULL(b.cur_credit,0) as CurCredit,
                           ABS((ISNULL(b.cur_debit,0)-ISNULL(b.cur_credit,0))+ISNULL(a.prev_bal,0)) as CurBal,
                           CASE WHEN (ISNULL(b.cur_debit,0)-ISNULL(b.cur_credit,0))+ISNULL(a.prev_bal,0) >0 THEN 'Dr.' ELSE 'Cr.' END as CurBalType
                    FROM customer c
                    LEFT JOIN (
                        SELECT acc_code, SUM(dr_amount)-SUM(cr_amount) as prev_bal,
                               CASE WHEN SUM(dr_amount)-SUM(cr_amount) >0 THEN 'Dr.' ELSE 'Cr.' END as bal_type
                        FROM view_gjournal WHERE vouch_Date < '{fromDateString}' 
                        GROUP BY acc_code
                    ) as a ON c.acc_code = a.acc_code
                    LEFT JOIN (
                        SELECT acc_code, SUM(dr_amount) as cur_debit, SUM(cr_amount) as cur_credit 
                        FROM view_gjournal WHERE vouch_Date BETWEEN '{fromDateString}' AND '{toDateString}' 
                        GROUP BY acc_code
                    ) as b ON c.acc_code = b.acc_code
                    WHERE CAST(REPLACE(c.acc_code, '-', '') AS BIGINT) >= {fromAccountNumeric}
                      AND CAST(REPLACE(c.acc_code, '-', '') AS BIGINT) <= {uptoAccountNumeric}
                    ORDER BY c.acc_code";

                var results = await _dataAccessService.ExecuteQueryAsync(user, query);
                
                var threeTrialBalanceItems = new List<ThreeTrialBalanceItem>();
                decimal totalPrevBal = 0;
                decimal totalCurDebit = 0;
                decimal totalCurCredit = 0;
                decimal totalCurBal = 0;

                foreach (var row in results)
                {
                    var item = new ThreeTrialBalanceItem
                    {
                        AccCode = row.ContainsKey("AccCode") ? row["AccCode"]?.ToString() ?? "" : "",
                        AccName = row.ContainsKey("AccName") ? row["AccName"]?.ToString() ?? "" : "",
                        PrevBal = row.ContainsKey("PrevBal") && decimal.TryParse(row["PrevBal"]?.ToString(), out var prevBal) ? Math.Abs(prevBal) : 0,
                        BalType = row.ContainsKey("BalType") ? row["BalType"]?.ToString() ?? "" : "",
                        CurDebit = row.ContainsKey("CurDebit") && decimal.TryParse(row["CurDebit"]?.ToString(), out var curDebit) ? curDebit : 0,
                        CurCredit = row.ContainsKey("CurCredit") && decimal.TryParse(row["CurCredit"]?.ToString(), out var curCredit) ? curCredit : 0,
                        CurBal = row.ContainsKey("CurBal") && decimal.TryParse(row["CurBal"]?.ToString(), out var curBal) ? curBal : 0,
                        CurBalType = row.ContainsKey("CurBalType") ? row["CurBalType"]?.ToString() ?? "" : ""
                    };

                    threeTrialBalanceItems.Add(item);
                    totalPrevBal += item.PrevBal;
                    totalCurDebit += item.CurDebit;
                    totalCurCredit += item.CurCredit;
                    totalCurBal += item.CurBal;
                }

                return Ok(new ThreeTrialBalanceResponse
                {
                    Success = true,
                    Message = "3 Trial Balance retrieved successfully",
                    Data = threeTrialBalanceItems,
                    FromDate = from,
                    ToDate = to,
                    FromAccount = fromAcc,
                    UptoAccount = uptoAcc,
                    TotalPrevBal = totalPrevBal,
                    TotalCurDebit = totalCurDebit,
                    TotalCurCredit = totalCurCredit,
                    TotalCurBal = totalCurBal
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting 3 trial balance");
                return StatusCode(500, new ThreeTrialBalanceResponse
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        // Account Ledger Report
        [HttpGet("ledger")]
        public async Task<ActionResult<LedgerReportResponse>> GetAccountLedger(
            [FromQuery] string accountId,
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate)
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest(new LedgerReportResponse
                    {
                        Success = false,
                        Message = "No active database connection found. Please set up your database connection first."
                    });
                }

                if (string.IsNullOrEmpty(accountId))
                {
                    return BadRequest(new LedgerReportResponse
                    {
                        Success = false,
                        Message = "Account ID is required."
                    });
                }

                var fromDateString = fromDate.ToString("yyyy/MM/dd");
                var toDateString = toDate.ToString("yyyy/MM/dd");

                // Get account name
                var accountQuery = $"SELECT acc_name FROM customer WHERE acc_code = '{accountId}'";
                var accountResult = await _dataAccessService.ExecuteQueryAsync(user, accountQuery);
                var accountName = accountResult.FirstOrDefault()?["acc_name"]?.ToString() ?? "";

                // Ledger query using client's exact SQL pattern
                var ledgerQuery = $@"
                    declare @stDate as date,@enDate as date,@accountId as varchar(12)
                    set @stDate = '{fromDateString}'
                    set @enDate = '{toDateString}'
                    set @accountId = '{accountId}'

                    ---- Previous Balance
                    select *  into #ledger from 
                    (select '000000' as vouch_no,'' as vouch_date,'' as acc_code,'Previous Balance' descript,
                    case when sum(dr_amount)-sum(cr_amount) >0 then abs(sum(dr_amount)-sum(cr_amount)) else 0 end as debit,
                    case when sum(dr_amount)-sum(cr_amount) <0 then abs(sum(dr_amount)-sum(cr_amount)) else 0 end as credit,
                    0 as balance
                     from view_gjournal where acc_code = @accountId and vouch_Date <@stDate 
                    ------ current Record 
                    union all
                    select  vouch_no,vouch_date,acc_code, descript,dr_amount,cr_amount,0 as balance
                     from view_gjournal where acc_code = @accountId and vouch_Date between @stDate and @enDate 
                     ) as a order by vouch_Date

                    SELECT 
                        a.vouch_no,
                        a.vouch_date,
                        a.acc_code,
                        a.descript,
                        a.debit,
                        a.credit,
                        
                        -- Running Balance
                        (
                            SELECT SUM(x.debit - x.credit)
                            FROM #ledger x
                            WHERE x.vouch_date <= a.vouch_date
                              AND x.vouch_no <= a.vouch_no
                        ) AS balance,
                        
                        -- Balance Type
                        IIF(
                            (
                                SELECT SUM(x.debit - x.credit)
                                FROM #ledger x
                                WHERE x.vouch_date <= a.vouch_date
                                  AND x.vouch_no <= a.vouch_no
                            ) >= 0,
                            'Dr', 'Cr'
                        ) AS bal_type

                    FROM #ledger a
                    ORDER BY a.vouch_date, a.vouch_no;

                    DROP TABLE #ledger";

                var results = await _dataAccessService.ExecuteQueryAsync(user, ledgerQuery);
                
                var ledgerItems = new List<LedgerItem>();
                decimal totalDebit = 0;
                decimal totalCredit = 0;

                foreach (var row in results)
                {
                    var item = new LedgerItem
                    {
                        VouchNo = row.ContainsKey("vouch_no") ? row["vouch_no"]?.ToString() ?? "" : "",
                        VouchDate = row.ContainsKey("vouch_date") && DateTime.TryParse(row["vouch_date"]?.ToString(), out var date) ? date : DateTime.MinValue,
                        AccCode = row.ContainsKey("acc_code") ? row["acc_code"]?.ToString() ?? "" : "",
                        Descript = row.ContainsKey("descript") ? row["descript"]?.ToString() ?? "" : "",
                        Debit = row.ContainsKey("debit") && decimal.TryParse(row["debit"]?.ToString(), out var debit) ? debit : 0,
                        Credit = row.ContainsKey("credit") && decimal.TryParse(row["credit"]?.ToString(), out var credit) ? credit : 0,
                        Balance = row.ContainsKey("balance") && decimal.TryParse(row["balance"]?.ToString(), out var balance) ? Math.Abs(balance) : 0,
                        BalType = row.ContainsKey("bal_type") ? row["bal_type"]?.ToString() ?? "" : ""
                    };

                    ledgerItems.Add(item);
                    totalDebit += item.Debit;
                    totalCredit += item.Credit;
                }

                return Ok(new LedgerReportResponse
                {
                    Success = true,
                    Message = "Account Ledger retrieved successfully",
                    Data = ledgerItems,
                    FromDate = fromDate,
                    ToDate = toDate,
                    AccountCode = accountId,
                    AccountName = accountName,
                    TotalDebit = totalDebit,
                    TotalCredit = totalCredit
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account ledger");
                return StatusCode(500, new LedgerReportResponse
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        // Account Position Report
        [HttpGet("account-position")]
        public async Task<ActionResult<AccountPositionResponse>> GetAccountPosition(
            [FromQuery] string accountId,
            [FromQuery] DateTime uptoDate)
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest(new AccountPositionResponse
                    {
                        Success = false,
                        Message = "No active database connection found. Please set up your database connection first."
                    });
                }

                if (string.IsNullOrEmpty(accountId))
                {
                    return BadRequest(new AccountPositionResponse
                    {
                        Success = false,
                        Message = "Account ID is required."
                    });
                }

                var uptoDateString = uptoDate.ToString("yyyy/MM/dd");

                // Get account name
                var accountQuery = $"SELECT acc_name FROM customer WHERE acc_code = '{accountId}'";
                var accountResult = await _dataAccessService.ExecuteQueryAsync(user, accountQuery);
                var accountName = accountResult.FirstOrDefault()?["acc_name"]?.ToString() ?? "";

                // Calculate totals up to the specified date
                var totalsQuery = $@"
                    SELECT 
                        SUM(ISNULL(dr_amount, 0)) as TotalDebit,
                        SUM(ISNULL(cr_amount, 0)) as TotalCredit
                    FROM view_gjournal 
                    WHERE acc_code = '{accountId}' 
                    AND vouch_date <= '{uptoDateString}'";

                var totalsResult = await _dataAccessService.ExecuteQueryAsync(user, totalsQuery);
                var firstRow = totalsResult.FirstOrDefault();

                decimal totalDebit = 0;
                decimal totalCredit = 0;

                if (firstRow != null)
                {
                    totalDebit = firstRow.ContainsKey("TotalDebit") && decimal.TryParse(firstRow["TotalDebit"]?.ToString(), out var debit) ? debit : 0;
                    totalCredit = firstRow.ContainsKey("TotalCredit") && decimal.TryParse(firstRow["TotalCredit"]?.ToString(), out var credit) ? credit : 0;
                }

                // Calculate balance
                decimal balance = totalDebit - totalCredit;
                string balanceType = balance >= 0 ? "Dr" : "Cr";
                decimal balanceAmount = Math.Abs(balance);

                return Ok(new AccountPositionResponse
                {
                    Success = true,
                    Message = "Account position retrieved successfully",
                    AccountCode = accountId,
                    AccountName = accountName,
                    TotalDebit = totalDebit,
                    TotalCredit = totalCredit,
                    Balance = balanceAmount,
                    BalanceType = balanceType,
                    UptoDate = uptoDate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account position");
                return StatusCode(500, new AccountPositionResponse
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        // Sales Summary Report - EXAMPLE using ClientDataService
        // This demonstrates the hybrid EF approach for simpler reports
        [HttpGet("sales-summary")]
        public async Task<ActionResult> GetSalesSummary([FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest(new { Success = false, Message = "No active database connection found." });
                }

                // Use ClientDataService for simpler reports
                // The service wraps FromSqlRaw() for better architecture
                var summary = await _clientDataService.GetSalesSummaryAsync(user, fromDate, toDate);
                
                return Ok(new
                {
                    Success = true,
                    Message = "Sales summary retrieved successfully",
                    Data = summary,
                    FromDate = fromDate,
                    ToDate = toDate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales summary");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        // Cash Book Report
        [HttpGet("cash-book")]
        public async Task<ActionResult<CashBookResponse>> GetCashBook([FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest(new CashBookResponse
                    {
                        Success = false,
                        Message = "User not found or inactive"
                    });
                }

                // Use provided dates or default to current date
                var reportFromDate = fromDate ?? DateTime.Now;
                var reportToDate = toDate ?? DateTime.Now;

                // Main cash book query - matches client desktop query exactly
                // Uses view_cashBook directly for better performance
                // If view_cashBook doesn't exist, falls back to optimized inline logic
                var cashBookQuery = $@"
                    declare @stDate as date,@enDate as date
                    set @stDate = '{reportFromDate:yyyy/MM/dd}'
                    set @enDate = '{reportToDate:yyyy/MM/dd}'

                    select a.*,b.acc_name as ac_name from (
                    select 0 as sr_no,'' trans_Date,'' as acc_code,'' as acc_name, 'Previous Balance' as descript,'' as vouch_no,
                    case when sum(dr_amount)-sum(Cr_amount) >0 then abs(sum(dr_amount)-sum(Cr_amount)) else 0 end as Receipts,
                    case when sum(dr_amount)-sum(Cr_amount) <0 then abs(sum(dr_amount)-sum(Cr_amount)) else 0 end as Payment
                    from view_gjournal  where acc_code =(select cash_ac from acc_desc) and vouch_Date <@stDate
                    union all
                    select 1 as sr_no,vouch_Date as trans_Date,acc_code,'' as acc_name, descript, vouch_no,
                     dr_amount as Receipts,0 as payment
                    from view_cashBook  where  vouch_Date between @stDate and @enDate
                    and dr_amount >0
                    union all 
                    select 2 as sr_no,vouch_Date as trans_Date,acc_code,'' as acc_name, descript, vouch_no,
                    0 as Receipts,CR_AMOUNT as payment
                    from view_cashBook  where  vouch_Date between @stDate and @enDate and cr_amount >0
                    ) as a left join customer b on a.acc_code=b.acc_code
                    order by trans_Date,sr_no";

                List<Dictionary<string, object>> results;
                try
                {
                    // Try using view_cashBook directly (matches client query)
                    results = await _dataAccessService.ExecuteQueryAsync(user, cashBookQuery, commandTimeout: 120);
                }
                catch (System.Data.SqlClient.SqlException ex) when (ex.Message.Contains("Invalid object name") && ex.Message.Contains("view_cashBook"))
                {
                    // Fall back to optimized inline logic if view_cashBook doesn't exist
                    _logger.LogInformation("view_cashBook not found, using inline logic");
                    var fallbackQuery = $@"
                        declare @stDate as date, @enDate as date, @cashAc varchar(12)
                        set @stDate = '{reportFromDate:yyyy/MM/dd}'
                        set @enDate = '{reportToDate:yyyy/MM/dd}'
                        set @cashAc = (select cash_ac from acc_desc)

                        ;WITH CashVouchersInRange AS (
                            SELECT DISTINCT vouch_no 
                            FROM view_gjournal 
                            WHERE acc_code = @cashAc
                            AND vouch_Date between @stDate and @enDate
                        )
                        select a.*, b.acc_name as ac_name from (
                            select 0 as sr_no, '' trans_Date, '' as acc_code, '' as acc_name, 'Previous Balance' as descript, '' as vouch_no,
                            case when sum(dr_amount)-sum(Cr_amount) >0 then abs(sum(dr_amount)-sum(Cr_amount)) else 0 end as Receipts,
                            case when sum(dr_amount)-sum(Cr_amount) <0 then abs(sum(dr_amount)-sum(Cr_amount)) else 0 end as Payment
                            from view_gjournal where acc_code = @cashAc and vouch_Date < @stDate
                            union all
                            select 1 as sr_no, g.vouch_Date as trans_Date, g.acc_code, '' as acc_name, g.descript, g.vouch_no,
                            g.dr_amount as Receipts, 0 as payment
                            from view_gjournal g
                            INNER JOIN CashVouchersInRange cv ON g.vouch_no = cv.vouch_no
                            where g.vouch_Date between @stDate and @enDate
                            and g.dr_amount > 0 
                            and g.acc_code <> @cashAc
                            and LEFT(g.vouch_no, 2) <> 'OB'
                            union all 
                            select 2 as sr_no, g.vouch_Date as trans_Date, g.acc_code, '' as acc_name, g.descript, g.vouch_no,
                            0 as Receipts, g.cr_amount as payment
                            from view_gjournal g
                            INNER JOIN CashVouchersInRange cv ON g.vouch_no = cv.vouch_no
                            where g.vouch_Date between @stDate and @enDate 
                            and g.cr_amount > 0
                            and g.acc_code <> @cashAc
                            and LEFT(g.vouch_no, 2) <> 'OB'
                        ) as a left join customer b on a.acc_code=b.acc_code
                        order by trans_Date, sr_no";
                    results = await _dataAccessService.ExecuteQueryAsync(user, fallbackQuery, commandTimeout: 120);
                }
                
                var cashBookItems = new List<CashBookItem>();
                decimal totalReceipts = 0;
                decimal totalPayments = 0;

                foreach (var row in results)
                {
                    var item = new CashBookItem
                    {
                        SrNo = row.ContainsKey("sr_no") && int.TryParse(row["sr_no"]?.ToString(), out var srNo) ? srNo : 0,
                        TransDate = row.ContainsKey("trans_Date") && !string.IsNullOrEmpty(row["trans_Date"]?.ToString()) && 
                                   DateTime.TryParse(row["trans_Date"]?.ToString(), out var transDate) ? transDate : null,
                        AccCode = row.ContainsKey("acc_code") ? row["acc_code"]?.ToString() ?? "" : "",
                        AccName = row.ContainsKey("ac_name") ? row["ac_name"]?.ToString() ?? "" : "",
                        Description = row.ContainsKey("descript") ? row["descript"]?.ToString() ?? "" : "",
                        VoucherNo = row.ContainsKey("vouch_no") ? row["vouch_no"]?.ToString() ?? "" : "",
                        Receipts = row.ContainsKey("Receipts") && decimal.TryParse(row["Receipts"]?.ToString(), out var receipts) ? receipts : 0,
                        Payments = row.ContainsKey("Payment") && decimal.TryParse(row["Payment"]?.ToString(), out var payments) ? payments : 0
                    };

                    cashBookItems.Add(item);
                    totalReceipts += item.Receipts;
                    totalPayments += item.Payments;
                }

                stopwatch.Stop();

                return Ok(new CashBookResponse
                {
                    Success = true,
                    Message = "Cash Book retrieved successfully",
                    Data = cashBookItems,
                    FromDate = reportFromDate,
                    ToDate = reportToDate,
                    TotalReceipts = totalReceipts,
                    TotalPayments = totalPayments,
                    CashBalance = totalReceipts - totalPayments,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error getting cash book");
                return StatusCode(500, new CashBookResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}",
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                });
            }
        }

        // Journal Book Report
        [HttpGet("journal-book")]
        public async Task<ActionResult<JournalBookResponse>> GetJournalBook([FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest(new JournalBookResponse
                    {
                        Success = false,
                        Message = "No active database connection found. Please set up your database connection first."
                    });
                }

                // Use provided dates or default to current date
                var reportFromDate = fromDate ?? DateTime.Now;
                var reportToDate = toDate ?? DateTime.Now;
                var fromDateString = reportFromDate.ToString("yyyy/MM/dd");
                var toDateString = reportToDate.ToString("yyyy/MM/dd");

                // Journal Book SQL query - get all entries with account names
                // Only include Journal Voucher entries (voucher numbers starting with 'JV')
                // Exclude Opening Balance records (voucher numbers starting with 'OB')
                var journalBookQuery = $@"
                    SELECT 
                        v.vouch_date as VouchDate,
                        v.vouch_no as VouchNo,
                        v.acc_code as AccCode,
                        ISNULL(c.acc_name, '') as AccName,
                        ISNULL(v.descript, '') as Description,
                        ISNULL(v.dr_amount, 0) as Receipts,
                        ISNULL(v.cr_amount, 0) as Payments,
                        ROW_NUMBER() OVER (PARTITION BY v.vouch_no ORDER BY v.dr_amount DESC, v.cr_amount DESC) as RowNum
                    FROM view_gjournal v
                    LEFT JOIN customer c ON v.acc_code = c.acc_code
                    WHERE v.vouch_date >= '{fromDateString}' 
                      AND v.vouch_date <= '{toDateString}'
                      AND LEFT(v.vouch_no, 2) = 'JV'
                    ORDER BY v.vouch_date, v.vouch_no, RowNum";

                var results = await _dataAccessService.ExecuteQueryAsync(user, journalBookQuery, commandTimeout: 120);
                
                // Group entries by voucher number and process
                var voucherGroups = results
                    .GroupBy(row => 
                        row.ContainsKey("VouchNo") ? row["VouchNo"]?.ToString() ?? "" : ""
                    )
                    .Where(g => !string.IsNullOrEmpty(g.Key))
                    .Select(g => new
                    {
                        VoucherNo = g.Key,
                        VoucherDate = g.First().ContainsKey("VouchDate") && DateTime.TryParse(g.First()["VouchDate"]?.ToString(), out var date) ? date : DateTime.MinValue,
                        Entries = g.OrderBy(e => 
                            e.ContainsKey("RowNum") && int.TryParse(e["RowNum"]?.ToString(), out var rowNum) ? rowNum : 0
                        ).ToList()
                    })
                    .OrderBy(g => g.VoucherDate)
                    .ThenBy(g => g.VoucherNo)
                    .ToList();

                var journalBookItems = new List<JournalBookItem>();
                decimal totalReceipts = 0;
                decimal totalPayments = 0;

                foreach (var voucherGroup in voucherGroups)
                {
                    var voucherReceipts = 0m;
                    var voucherPayments = 0m;
                    bool isFirstEntry = true;

                    foreach (var row in voucherGroup.Entries)
                    {
                        var receipts = row.ContainsKey("Receipts") && decimal.TryParse(row["Receipts"]?.ToString(), out var r) ? r : 0;
                        var payments = row.ContainsKey("Payments") && decimal.TryParse(row["Payments"]?.ToString(), out var p) ? p : 0;
                        var accCode = row.ContainsKey("AccCode") ? row["AccCode"]?.ToString() ?? "" : "";
                        var accName = row.ContainsKey("AccName") ? row["AccName"]?.ToString() ?? "" : "";
                        var description = row.ContainsKey("Description") ? row["Description"]?.ToString() ?? "" : "";

                        voucherReceipts += receipts;
                        voucherPayments += payments;

                        // Build particulars: account name + description
                        var particulars = "";
                        if (!string.IsNullOrEmpty(accName))
                        {
                            particulars = accName.Trim();
                        }
                        if (!string.IsNullOrEmpty(description))
                        {
                            if (!string.IsNullOrEmpty(particulars))
                            {
                                particulars += ", " + description.Trim();
                            }
                            else
                            {
                                particulars = description.Trim();
                            }
                        }

                        var item = new JournalBookItem
                        {
                            Date = isFirstEntry ? voucherGroup.VoucherDate : null,
                            Particulars = particulars,
                            VoucherNo = voucherGroup.VoucherNo,
                            Receipts = receipts,
                            Payments = payments,
                            SrNo = 0,
                            AccCode = accCode,
                            AccName = accName,
                            Description = description,
                            IsTotalRow = false
                        };

                        journalBookItems.Add(item);
                        isFirstEntry = false;
                    }

                    // Add total row for this voucher
                    journalBookItems.Add(new JournalBookItem
                    {
                        Date = null,
                        Particulars = "Total:",
                        VoucherNo = voucherGroup.VoucherNo,
                        Receipts = voucherReceipts,
                        Payments = voucherPayments,
                        SrNo = 0,
                        AccCode = "",
                        AccName = "",
                        Description = "",
                        IsTotalRow = true
                    });

                    totalReceipts += voucherReceipts;
                    totalPayments += voucherPayments;
                }

                stopwatch.Stop();

                return Ok(new JournalBookResponse
                {
                    Success = true,
                    Message = "Journal Book retrieved successfully",
                    Data = journalBookItems,
                    FromDate = reportFromDate,
                    ToDate = reportToDate,
                    TotalReceipts = totalReceipts,
                    TotalPayments = totalPayments
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error getting journal book");
                return StatusCode(500, new JournalBookResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        // Transaction Journal Report
        [HttpGet("transaction-journal")]
        public async Task<ActionResult<TransactionJournalResponse>> GetTransactionJournal(
            [FromQuery] DateTime fromDate, 
            [FromQuery] DateTime toDate,
            [FromQuery] string? documentTypes = null)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest(new TransactionJournalResponse
                    {
                        Success = false,
                        Message = "No active database connection found. Please set up your database connection first."
                    });
                }

                // Validate document types
                if (string.IsNullOrWhiteSpace(documentTypes))
                {
                    return BadRequest(new TransactionJournalResponse
                    {
                        Success = false,
                        Message = "Please select at least one document type."
                    });
                }

                // Parse document types (comma-separated string like "SV,ES,SR,JV")
                var selectedTypes = documentTypes.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim().ToUpper())
                    .Where(t => !string.IsNullOrEmpty(t))
                    .ToList();

                if (selectedTypes.Count == 0)
                {
                    return BadRequest(new TransactionJournalResponse
                    {
                        Success = false,
                        Message = "Please select at least one document type."
                    });
                }

                var fromDateString = fromDate.ToString("yyyy/MM/dd");
                var toDateString = toDate.ToString("yyyy/MM/dd");

                // Build voucher type filter - map to voucher prefixes
                // SV = Sales Journal, ES = Purchase Journal, SR = Sales Return Journal,
                // JV = Journal Voucher, CP = Cash Payment Journal, CR = Cash Receipts Journal,
                // BP = Bank Payment Journal, BR = Bank Receipts Journal, IB = Inter Bank Journal
                var voucherPrefixes = selectedTypes.Select(t => $"'{t}'").ToList();
                var voucherFilter = string.Join(",", voucherPrefixes);

                // Transaction Journal SQL query - get all entries with account names
                // Filter by selected voucher types
                // Join with sale_inv to get delivery and vehicle information for sales vouchers
                var transactionJournalQuery = $@"
                    SELECT 
                        v.vouch_date as VouchDate,
                        v.vouch_no as VouchNo,
                        v.acc_code as AccCode,
                        ISNULL(c.acc_name, '') as AccName,
                        ISNULL(v.descript, '') as Description,
                        ISNULL(v.dr_amount, 0) as Debit,
                        ISNULL(v.cr_amount, 0) as Credit,
                        ISNULL(si.deliver_to, '') as DeliverTo,
                        ISNULL(si.vehicle_no, '') as VehicleNo,
                        ROW_NUMBER() OVER (PARTITION BY v.vouch_no ORDER BY v.dr_amount DESC, v.cr_amount DESC) as RowNum
                    FROM view_gjournal v
                    LEFT JOIN customer c ON v.acc_code = c.acc_code
                    LEFT JOIN sale_inv si ON LEFT(v.vouch_no, 2) = 'SV' 
                        AND (
                            RIGHT(v.vouch_no, LEN(v.vouch_no) - 2) = si.inv_no
                            OR LTRIM(RIGHT(v.vouch_no, LEN(v.vouch_no) - 2), '0') = LTRIM(si.inv_no, '0')
                        )
                    WHERE v.vouch_date >= '{fromDateString}' 
                      AND v.vouch_date <= '{toDateString}'
                      AND LEFT(v.vouch_no, 2) IN ({voucherFilter})
                    ORDER BY v.vouch_date, v.vouch_no, RowNum";

                var results = await _dataAccessService.ExecuteQueryAsync(user, transactionJournalQuery, commandTimeout: 120);
                
                // Document type mapping: voucher prefix to display name
                var documentTypeMapping = new Dictionary<string, string>
                {
                    { "SV", "Sales" },
                    { "ES", "Purchase" },
                    { "SR", "Sales Return" },
                    { "JV", "Journal Voucher" },
                    { "CP", "Cash Payment" },
                    { "CR", "Cash Receipts" },
                    { "BP", "Bank Payment" },
                    { "BR", "Bank Receipts" },
                    { "IB", "Inter Bank" }
                };

                // Group entries by voucher number first
                var voucherGroups = results
                    .GroupBy(row => 
                        row.ContainsKey("VouchNo") ? row["VouchNo"]?.ToString() ?? "" : ""
                    )
                    .Where(g => !string.IsNullOrEmpty(g.Key))
                    .Select(g => new
                    {
                        VoucherNo = g.Key,
                        VoucherPrefix = g.Key.Length >= 2 ? g.Key.Substring(0, 2).ToUpper() : "",
                        VoucherDate = g.First().ContainsKey("VouchDate") && DateTime.TryParse(g.First()["VouchDate"]?.ToString(), out var date) ? date : DateTime.MinValue,
                        Entries = g.OrderBy(e => 
                            e.ContainsKey("RowNum") && int.TryParse(e["RowNum"]?.ToString(), out var rowNum) ? rowNum : 0
                        ).ToList()
                    })
                    .ToList();

                // Group vouchers by document type (voucher prefix)
                var documentTypeGroups = voucherGroups
                    .GroupBy(vg => vg.VoucherPrefix)
                    .Where(g => selectedTypes.Contains(g.Key))
                    .ToDictionary(g => g.Key, g => g.OrderBy(v => v.VoucherDate).ThenBy(v => v.VoucherNo).ToList());

                var transactionJournalItems = new List<TransactionJournalItem>();
                decimal totalDebit = 0;
                decimal totalCredit = 0;

                // Process each document type in the order they were selected
                foreach (var documentType in selectedTypes)
                {
                    if (!documentTypeGroups.ContainsKey(documentType))
                        continue;

                    // Add header row for this document type
                    var documentTypeName = documentTypeMapping.ContainsKey(documentType) 
                        ? documentTypeMapping[documentType] 
                        : documentType;
                    
                    transactionJournalItems.Add(new TransactionJournalItem
                    {
                        Date = null,
                        Particulars = "",
                        VrNo = "",
                        Debit = 0,
                        Credit = 0,
                        AccCode = "",
                        AccName = "",
                        Description = "",
                        DeliverTo = "",
                        VehicleNo = "",
                        IsTotalRow = false,
                        IsHeaderRow = true,
                        DocumentTypeName = documentTypeName
                    });

                    // Process all vouchers for this document type
                    var vouchersForType = documentTypeGroups[documentType];
                    foreach (var voucherGroup in vouchersForType)
                    {
                        var voucherDebit = 0m;
                        var voucherCredit = 0m;
                        bool isFirstEntry = true;
                        
                        // Separate debit and credit entries
                        var debitEntries = new List<Dictionary<string, object>>();
                        var creditEntries = new List<Dictionary<string, object>>();
                        
                        // Extract DeliverTo and VehicleNo from any row in the voucher (should be same for all rows)
                        string voucherDeliverTo = "";
                        string voucherVehicleNo = "";

                        foreach (var row in voucherGroup.Entries)
                        {
                            var debit = row.ContainsKey("Debit") && decimal.TryParse(row["Debit"]?.ToString(), out var d) ? d : 0;
                            var credit = row.ContainsKey("Credit") && decimal.TryParse(row["Credit"]?.ToString(), out var c) ? c : 0;
                            
                            voucherDebit += debit;
                            voucherCredit += credit;
                            
                            // Extract DeliverTo and VehicleNo from first row that has them
                            if (string.IsNullOrEmpty(voucherDeliverTo) && row.ContainsKey("DeliverTo"))
                            {
                                var deliverToValue = row["DeliverTo"]?.ToString()?.Trim();
                                if (!string.IsNullOrWhiteSpace(deliverToValue))
                                {
                                    voucherDeliverTo = deliverToValue;
                                }
                            }
                            if (string.IsNullOrEmpty(voucherVehicleNo) && row.ContainsKey("VehicleNo"))
                            {
                                var vehicleNoValue = row["VehicleNo"]?.ToString()?.Trim();
                                if (!string.IsNullOrWhiteSpace(vehicleNoValue))
                                {
                                    voucherVehicleNo = vehicleNoValue;
                                }
                            }
                            
                            if (debit > 0)
                            {
                                debitEntries.Add(row);
                            }
                            else if (credit > 0)
                            {
                                creditEntries.Add(row);
                            }
                        }

                        // Add consolidated debit entry if there are any debits
                        if (debitEntries.Count > 0)
                        {
                            // Get AccName from first debit entry (typically "NET SALE" or similar)
                            var firstDebitRow = debitEntries.First();
                            var debitAccName = firstDebitRow.ContainsKey("AccName") ? firstDebitRow["AccName"]?.ToString() ?? "" : "";
                            
                            var consolidatedDebitItem = new TransactionJournalItem
                            {
                                Date = voucherGroup.VoucherDate,
                                Particulars = "Sales",
                                VrNo = voucherGroup.VoucherNo,
                                Debit = voucherDebit,
                                Credit = 0,
                                AccCode = "",
                                AccName = debitAccName, // Preserve AccName from debit entries
                                Description = "",
                                DeliverTo = "",
                                VehicleNo = "",
                                IsTotalRow = false,
                                IsHeaderRow = false,
                                DocumentTypeName = ""
                            };
                            transactionJournalItems.Add(consolidatedDebitItem);
                            isFirstEntry = false;
                        }

                        // Add credit entries as individual rows
                        foreach (var row in creditEntries)
                        {
                            var credit = row.ContainsKey("Credit") && decimal.TryParse(row["Credit"]?.ToString(), out var c) ? c : 0;
                            var accCode = row.ContainsKey("AccCode") ? row["AccCode"]?.ToString() ?? "" : "";
                            var accName = row.ContainsKey("AccName") ? row["AccName"]?.ToString() ?? "" : "";
                            var description = row.ContainsKey("Description") ? row["Description"]?.ToString() ?? "" : "";

                            var item = new TransactionJournalItem
                            {
                                Date = isFirstEntry ? voucherGroup.VoucherDate : null,
                                Particulars = "", // Empty - frontend will display AccName and Description separately
                                VrNo = voucherGroup.VoucherNo,
                                Debit = 0,
                                Credit = credit,
                                AccCode = accCode,
                                AccName = accName, // Preserve AccName for bold display
                                Description = description, // Preserve Description for second line display
                                DeliverTo = voucherDeliverTo, // Use voucher-level delivery information
                                VehicleNo = voucherVehicleNo, // Use voucher-level vehicle information
                                IsTotalRow = false,
                                IsHeaderRow = false,
                                DocumentTypeName = ""
                            };

                            transactionJournalItems.Add(item);
                            isFirstEntry = false;
                        }

                        // Add total row for this voucher - "Total:" goes in Vr.No column
                        transactionJournalItems.Add(new TransactionJournalItem
                        {
                            Date = null,
                            Particulars = "", // Empty for total rows
                            VrNo = "Total:", // "Total:" goes in Vr.No column (column 3)
                            Debit = voucherDebit,
                            Credit = voucherCredit,
                            AccCode = "",
                            AccName = "",
                            Description = "",
                            DeliverTo = "",
                            VehicleNo = "",
                            IsTotalRow = true,
                            IsHeaderRow = false,
                            DocumentTypeName = ""
                        });

                        totalDebit += voucherDebit;
                        totalCredit += voucherCredit;
                    }
                }

                stopwatch.Stop();

                return Ok(new TransactionJournalResponse
                {
                    Success = true,
                    Message = "Transaction Journal retrieved successfully",
                    Data = transactionJournalItems,
                    FromDate = fromDate,
                    ToDate = toDate,
                    TotalDebit = totalDebit,
                    TotalCredit = totalCredit,
                    SelectedDocumentTypes = selectedTypes
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error getting transaction journal");
                return StatusCode(500, new TransactionJournalResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        // Purchase Journal Report
        [HttpGet("purchase-journal")]
        public async Task<ActionResult<PurchaseJournalResponse>> GetPurchaseJournal(
            [FromQuery] DateTime fromDate, 
            [FromQuery] DateTime toDate,
            [FromQuery] string invoiceType = "All")
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest(new PurchaseJournalResponse
                    {
                        Success = false,
                        Message = "No active database connection found. Please set up your database connection first."
                    });
                }

                // Map invoice types to voucher prefixes
                // Based on getinvType function: Crop -> PI, E.Sack -> ES, Others -> OP
                var voucherPrefixes = new List<string>();
                switch (invoiceType?.Trim())
                {
                    case "Crop":
                        voucherPrefixes.Add("PI");
                        break;
                    case "E.Sack":
                        voucherPrefixes.Add("ES");
                        break;
                    case "Others":
                        voucherPrefixes.Add("OP");
                        break;
                    case "All":
                    default:
                        voucherPrefixes.AddRange(new[] { "ES", "PI", "OP" });
                        break;
                }

                if (voucherPrefixes.Count == 0)
                {
                    return BadRequest(new PurchaseJournalResponse
                    {
                        Success = false,
                        Message = "Invalid invoice type specified."
                    });
                }

                var fromDateString = fromDate.ToString("yyyy/MM/dd");
                var toDateString = toDate.ToString("yyyy/MM/dd");
                var voucherFilter = string.Join(",", voucherPrefixes.Select(v => $"'{v}'"));

                // Purchase Journal SQL query - get all entries with account names
                var purchaseJournalQuery = $@"
                    SELECT 
                        v.vouch_date as VouchDate,
                        v.vouch_no as VouchNo,
                        v.acc_code as AccCode,
                        ISNULL(c.acc_name, '') as AccName,
                        ISNULL(v.descript, '') as Description,
                        ISNULL(v.dr_amount, 0) as Debit,
                        ISNULL(v.cr_amount, 0) as Credit,
                        ROW_NUMBER() OVER (PARTITION BY v.vouch_no ORDER BY v.dr_amount DESC, v.cr_amount DESC) as RowNum
                    FROM view_gjournal v
                    LEFT JOIN customer c ON v.acc_code = c.acc_code
                    WHERE v.vouch_date >= '{fromDateString}' 
                      AND v.vouch_date <= '{toDateString}'
                      AND LEFT(v.vouch_no, 2) IN ({voucherFilter})
                    ORDER BY v.vouch_date, v.vouch_no, RowNum";

                var results = await _dataAccessService.ExecuteQueryAsync(user, purchaseJournalQuery, commandTimeout: 120);
                
                // Group entries by voucher number
                var voucherGroups = results
                    .GroupBy(row => 
                        row.ContainsKey("VouchNo") ? row["VouchNo"]?.ToString() ?? "" : ""
                    )
                    .Where(g => !string.IsNullOrEmpty(g.Key))
                    .Select(g => new
                    {
                        VoucherNo = g.Key,
                        VoucherDate = g.First().ContainsKey("VouchDate") && DateTime.TryParse(g.First()["VouchDate"]?.ToString(), out var date) ? date : DateTime.MinValue,
                        Entries = g.OrderBy(e => 
                            e.ContainsKey("RowNum") && int.TryParse(e["RowNum"]?.ToString(), out var rowNum) ? rowNum : 0
                        ).ToList()
                    })
                    .OrderBy(v => v.VoucherDate)
                    .ThenBy(v => v.VoucherNo)
                    .ToList();

                var purchaseJournalItems = new List<PurchaseJournalItem>();
                decimal totalDebit = 0;
                decimal totalCredit = 0;

                // Process each voucher
                foreach (var voucherGroup in voucherGroups)
                {
                    var voucherDebit = 0m;
                    var voucherCredit = 0m;
                    
                    // Process all entries for this voucher
                    foreach (var row in voucherGroup.Entries)
                    {
                        var debit = row.ContainsKey("Debit") && decimal.TryParse(row["Debit"]?.ToString(), out var d) ? d : 0;
                        var credit = row.ContainsKey("Credit") && decimal.TryParse(row["Credit"]?.ToString(), out var c) ? c : 0;
                        
                        voucherDebit += debit;
                        voucherCredit += credit;
                        
                        // Add entry to report
                        purchaseJournalItems.Add(new PurchaseJournalItem
                        {
                            Date = voucherGroup.VoucherDate,
                            VoucherNo = voucherGroup.VoucherNo,
                            Description = row.ContainsKey("Description") ? row["Description"]?.ToString() ?? "" : "",
                            Debit = debit,
                            Credit = credit,
                            AccCode = row.ContainsKey("AccCode") ? row["AccCode"]?.ToString() ?? "" : "",
                            AccName = row.ContainsKey("AccName") ? row["AccName"]?.ToString() ?? "" : "",
                            IsVoucherTotal = false,
                            IsGrandTotal = false
                        });
                    }

                    // Add voucher total row
                    purchaseJournalItems.Add(new PurchaseJournalItem
                    {
                        Date = null,
                        VoucherNo = voucherGroup.VoucherNo, // Keep voucher number for reference
                        Description = "Total",
                        Debit = voucherDebit,
                        Credit = voucherCredit,
                        AccCode = "",
                        AccName = "",
                        IsVoucherTotal = true,
                        IsGrandTotal = false
                    });

                    totalDebit += voucherDebit;
                    totalCredit += voucherCredit;
                }

                // Add grand total row
                if (purchaseJournalItems.Count > 0)
                {
                    purchaseJournalItems.Add(new PurchaseJournalItem
                    {
                        Date = null,
                        VoucherNo = "",
                        Description = "Grand Total",
                        Debit = totalDebit,
                        Credit = totalCredit,
                        AccCode = "",
                        AccName = "",
                        IsVoucherTotal = false,
                        IsGrandTotal = true
                    });
                }

                stopwatch.Stop();

                return Ok(new PurchaseJournalResponse
                {
                    Success = true,
                    Message = "Purchase Journal retrieved successfully",
                    Data = purchaseJournalItems,
                    FromDate = fromDate,
                    ToDate = toDate,
                    TotalDebit = totalDebit,
                    TotalCredit = totalCredit,
                    InvoiceType = invoiceType
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error getting purchase journal");
                return StatusCode(500, new PurchaseJournalResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        // Sales Journal Report
        [HttpGet("sales-journal")]
        public async Task<ActionResult<SalesJournalResponse>> GetSalesJournal(
            [FromQuery] DateTime fromDate, 
            [FromQuery] DateTime toDate,
            [FromQuery] string invoiceType = "All")
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest(new SalesJournalResponse
                    {
                        Success = false,
                        Message = "No active database connection found. Please set up your database connection first."
                    });
                }

                // Map invoice types to voucher prefixes
                // Based on getinvType function: Credit -> SV, Cash -> CS, Others -> OS
                var voucherPrefixes = new List<string>();
                switch (invoiceType?.Trim())
                {
                    case "Credit":
                        voucherPrefixes.Add("SV");
                        break;
                    case "Cash":
                        voucherPrefixes.Add("CS");
                        break;
                    case "Others":
                        voucherPrefixes.Add("OS");
                        break;
                    case "All":
                    default:
                        voucherPrefixes.AddRange(new[] { "SV", "CS", "OS" });
                        break;
                }

                if (voucherPrefixes.Count == 0)
                {
                    return BadRequest(new SalesJournalResponse
                    {
                        Success = false,
                        Message = "Invalid invoice type specified."
                    });
                }

                var fromDateString = fromDate.ToString("yyyy/MM/dd");
                var toDateString = toDate.ToString("yyyy/MM/dd");
                var voucherFilter = string.Join(",", voucherPrefixes.Select(v => $"'{v}'"));

                // Sales Journal SQL query - get all entries with account names
                var salesJournalQuery = $@"
                    SELECT 
                        v.vouch_date as VouchDate,
                        v.vouch_no as VouchNo,
                        v.acc_code as AccCode,
                        ISNULL(c.acc_name, '') as AccName,
                        ISNULL(v.descript, '') as Description,
                        ISNULL(v.dr_amount, 0) as Debit,
                        ISNULL(v.cr_amount, 0) as Credit,
                        ROW_NUMBER() OVER (PARTITION BY v.vouch_no ORDER BY v.dr_amount DESC, v.cr_amount DESC) as RowNum
                    FROM view_gjournal v
                    LEFT JOIN customer c ON v.acc_code = c.acc_code
                    WHERE v.vouch_date >= '{fromDateString}' 
                      AND v.vouch_date <= '{toDateString}'
                      AND LEFT(v.vouch_no, 2) IN ({voucherFilter})
                    ORDER BY v.vouch_date, v.vouch_no, RowNum";

                var results = await _dataAccessService.ExecuteQueryAsync(user, salesJournalQuery, commandTimeout: 120);
                
                // Group entries by voucher number
                var voucherGroups = results
                    .GroupBy(row => 
                        row.ContainsKey("VouchNo") ? row["VouchNo"]?.ToString() ?? "" : ""
                    )
                    .Where(g => !string.IsNullOrEmpty(g.Key))
                    .Select(g => new
                    {
                        VoucherNo = g.Key,
                        VoucherDate = g.First().ContainsKey("VouchDate") && DateTime.TryParse(g.First()["VouchDate"]?.ToString(), out var date) ? date : DateTime.MinValue,
                        Entries = g.OrderBy(e => 
                            e.ContainsKey("RowNum") && int.TryParse(e["RowNum"]?.ToString(), out var rowNum) ? rowNum : 0
                        ).ToList()
                    })
                    .OrderBy(v => v.VoucherDate)
                    .ThenBy(v => v.VoucherNo)
                    .ToList();

                var salesJournalItems = new List<SalesJournalItem>();
                decimal totalDebit = 0;
                decimal totalCredit = 0;

                // Process each voucher
                foreach (var voucherGroup in voucherGroups)
                {
                    var voucherDebit = 0m;
                    var voucherCredit = 0m;
                    
                    // Process all entries for this voucher
                    foreach (var row in voucherGroup.Entries)
                    {
                        var debit = row.ContainsKey("Debit") && decimal.TryParse(row["Debit"]?.ToString(), out var d) ? d : 0;
                        var credit = row.ContainsKey("Credit") && decimal.TryParse(row["Credit"]?.ToString(), out var c) ? c : 0;
                        
                        voucherDebit += debit;
                        voucherCredit += credit;
                        
                        // Add entry to report
                        salesJournalItems.Add(new SalesJournalItem
                        {
                            Date = voucherGroup.VoucherDate,
                            VoucherNo = voucherGroup.VoucherNo,
                            Description = row.ContainsKey("Description") ? row["Description"]?.ToString() ?? "" : "",
                            Debit = debit,
                            Credit = credit,
                            AccCode = row.ContainsKey("AccCode") ? row["AccCode"]?.ToString() ?? "" : "",
                            AccName = row.ContainsKey("AccName") ? row["AccName"]?.ToString() ?? "" : "",
                            IsVoucherTotal = false,
                            IsGrandTotal = false
                        });
                    }

                    // Add voucher total row
                    salesJournalItems.Add(new SalesJournalItem
                    {
                        Date = null,
                        VoucherNo = voucherGroup.VoucherNo, // Keep voucher number for reference
                        Description = "Total",
                        Debit = voucherDebit,
                        Credit = voucherCredit,
                        AccCode = "",
                        AccName = "",
                        IsVoucherTotal = true,
                        IsGrandTotal = false
                    });

                    totalDebit += voucherDebit;
                    totalCredit += voucherCredit;
                }

                // Add grand total row
                if (salesJournalItems.Count > 0)
                {
                    salesJournalItems.Add(new SalesJournalItem
                    {
                        Date = null,
                        VoucherNo = "",
                        Description = "Grand Total",
                        Debit = totalDebit,
                        Credit = totalCredit,
                        AccCode = "",
                        AccName = "",
                        IsVoucherTotal = false,
                        IsGrandTotal = true
                    });
                }

                stopwatch.Stop();

                return Ok(new SalesJournalResponse
                {
                    Success = true,
                    Message = "Sales Journal retrieved successfully",
                    Data = salesJournalItems,
                    FromDate = fromDate,
                    ToDate = toDate,
                    TotalDebit = totalDebit,
                    TotalCredit = totalCredit,
                    InvoiceType = invoiceType
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error getting sales journal");
                return StatusCode(500, new SalesJournalResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        // Purchase Register Report
        [HttpGet("purchase-register")]
        public async Task<ActionResult<PurchaseRegisterResponse>> GetPurchaseRegister(
            [FromQuery] DateTime fromDate, 
            [FromQuery] DateTime toDate,
            [FromQuery] string invoiceType = "All")
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest(new PurchaseRegisterResponse
                    {
                        Success = false,
                        Message = "No active database connection found. Please set up your database connection first."
                    });
                }

                var fromDateString = fromDate.ToString("yyyy/MM/dd");
                var toDateString = toDate.ToString("yyyy/MM/dd");

                // Build invoice type filter
                string invoiceTypeFilter = "";
                if (invoiceType?.Trim() != "All")
                {
                    invoiceTypeFilter = $"AND pi.inv_type = '{invoiceType.Trim()}'";
                }

                // Purchase Register SQL query - join pur_inv with sub_pinv to get detailed line items
                var purchaseRegisterQuery = $@"
                    SELECT 
                        pi.inv_type as InvoiceType,
                        pi.inv_no as InvoiceNo,
                        pi.inv_date as Date,
                        RTRIM(pi.ac_name) as Supplier,
                        RTRIM(pi.vehicle_no) as VehicleNo,
                        RTRIM(sp.item_name) as Item,
                        CASE 
                            WHEN sp.as_per = 'Mounds' OR sp.as_per = 'Kgs' THEN 
                                CASE 
                                    WHEN sp.packing = CAST(sp.packing AS INT) THEN CAST(CAST(sp.packing AS INT) AS VARCHAR) + ' Kgs'
                                    ELSE CAST(sp.packing AS VARCHAR) + ' Kgs'
                                END
                            ELSE 
                                CASE 
                                    WHEN sp.packing = CAST(sp.packing AS INT) THEN CAST(CAST(sp.packing AS INT) AS VARCHAR) + ' Unit'
                                    ELSE CAST(sp.packing AS VARCHAR) + ' Unit'
                                END
                        END as Packing,
                        (sp.qty_jute + sp.qty_pp_100 + sp.qty_pp_50) as Qty,
                        sp.total_wght as Weight,
                        sp.rate as Rate,
                        RTRIM(sp.as_per) as AsPer,
                        sp.grand_tot as Amount,
                        pi.inv_no as InvoiceNoSort
                    FROM pur_inv pi
                    INNER JOIN sub_pinv sp ON pi.inv_no = sp.inv_no AND pi.inv_type = sp.inv_type
                    WHERE pi.inv_date >= '{fromDateString}' 
                      AND pi.inv_date <= '{toDateString}'
                      {invoiceTypeFilter}
                    ORDER BY pi.inv_date, pi.inv_no, sp.sr_no";

                var results = await _dataAccessService.ExecuteQueryAsync(user, purchaseRegisterQuery, commandTimeout: 120);
                
                var purchaseRegisterItems = new List<PurchaseRegisterItem>();
                decimal totalWeight = 0;
                decimal totalAmount = 0;
                string currentInvoiceNo = "";
                decimal invoiceWeight = 0;
                decimal invoiceAmount = 0;

                // Process each row
                foreach (var row in results)
                {
                    var invoiceNo = row.ContainsKey("InvoiceNo") ? row["InvoiceNo"]?.ToString()?.Trim() ?? "" : "";
                    
                    // If this is a new invoice and we have previous invoice data, add subtotal
                    if (!string.IsNullOrEmpty(currentInvoiceNo) && currentInvoiceNo != invoiceNo)
                    {
                        // Add subtotal row for previous invoice
                        purchaseRegisterItems.Add(new PurchaseRegisterItem
                        {
                            InvoiceType = "",
                            InvoiceNo = "",
                            Date = null,
                            Supplier = "",
                            VehicleNo = "",
                            Item = "",
                            Packing = "",
                            Qty = 0,
                            Weight = invoiceWeight,
                            Rate = 0,
                            AsPer = "",
                            Amount = invoiceAmount,
                            IsSubTotal = true
                        });
                        
                        invoiceWeight = 0;
                        invoiceAmount = 0;
                    }

                    // Parse row data
                    var rowInvoiceType = row.ContainsKey("InvoiceType") ? row["InvoiceType"]?.ToString()?.Trim() ?? "" : "";
                    var date = row.ContainsKey("Date") && DateTime.TryParse(row["Date"]?.ToString(), out var d) ? d : (DateTime?)null;
                    var supplier = row.ContainsKey("Supplier") ? row["Supplier"]?.ToString()?.Trim() ?? "" : "";
                    var vehicleNo = row.ContainsKey("VehicleNo") ? row["VehicleNo"]?.ToString()?.Trim() ?? "" : "";
                    var item = row.ContainsKey("Item") ? row["Item"]?.ToString()?.Trim() ?? "" : "";
                    var packingRaw = row.ContainsKey("Packing") ? row["Packing"]?.ToString()?.Trim() ?? "" : "";
                    // Format packing to remove .00 if it's a whole number
                    var packing = packingRaw;
                    if (!string.IsNullOrEmpty(packingRaw))
                    {
                        // Check if packing ends with " Kgs" or " Unit" and extract the number
                        if (packingRaw.EndsWith(" Kgs") || packingRaw.EndsWith(" Unit"))
                        {
                            var suffix = packingRaw.EndsWith(" Kgs") ? " Kgs" : " Unit";
                            var numberPart = packingRaw.Replace(suffix, "").Trim();
                            if (decimal.TryParse(numberPart, out var packingValue))
                            {
                                if (packingValue % 1 == 0)
                                {
                                    packing = $"{(int)packingValue}{suffix}";
                                }
                                else
                                {
                                    packing = packingRaw; // Keep original if has decimals
                                }
                            }
                        }
                    }
                    var qty = row.ContainsKey("Qty") && decimal.TryParse(row["Qty"]?.ToString(), out var q) ? q : 0;
                    var weight = row.ContainsKey("Weight") && decimal.TryParse(row["Weight"]?.ToString(), out var w) ? w : 0;
                    var rate = row.ContainsKey("Rate") && decimal.TryParse(row["Rate"]?.ToString(), out var r) ? r : 0;
                    var asPer = row.ContainsKey("AsPer") ? row["AsPer"]?.ToString()?.Trim() ?? "" : "";
                    var amount = row.ContainsKey("Amount") && decimal.TryParse(row["Amount"]?.ToString(), out var a) ? a : 0;

                    // Add item row
                    purchaseRegisterItems.Add(new PurchaseRegisterItem
                    {
                        InvoiceType = rowInvoiceType,
                        InvoiceNo = invoiceNo,
                        Date = date,
                        Supplier = supplier,
                        VehicleNo = vehicleNo,
                        Item = item,
                        Packing = packing,
                        Qty = qty,
                        Weight = weight,
                        Rate = rate,
                        AsPer = asPer,
                        Amount = amount,
                        IsSubTotal = false
                    });

                    invoiceWeight += weight;
                    invoiceAmount += amount;
                    totalWeight += weight;
                    totalAmount += amount;
                    currentInvoiceNo = invoiceNo;
                }

                // Add subtotal for last invoice if any
                if (!string.IsNullOrEmpty(currentInvoiceNo) && invoiceWeight > 0)
                {
                    purchaseRegisterItems.Add(new PurchaseRegisterItem
                    {
                        InvoiceType = "",
                        InvoiceNo = "",
                        Date = null,
                        Supplier = "",
                        VehicleNo = "",
                        Item = "",
                        Packing = "",
                        Qty = 0,
                        Weight = invoiceWeight,
                        Rate = 0,
                        AsPer = "",
                        Amount = invoiceAmount,
                        IsSubTotal = true
                    });
                }

                stopwatch.Stop();

                return Ok(new PurchaseRegisterResponse
                {
                    Success = true,
                    Message = "Purchase Register retrieved successfully",
                    Data = purchaseRegisterItems,
                    FromDate = fromDate,
                    ToDate = toDate,
                    InvoiceType = invoiceType ?? "All",
                    TotalWeight = totalWeight,
                    TotalAmount = totalAmount
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error getting purchase register");
                return StatusCode(500, new PurchaseRegisterResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        // Sales Register Report
        [HttpGet("sales-register")]
        public async Task<ActionResult<SalesRegisterResponse>> GetSalesRegister(
            [FromQuery] DateTime fromDate, 
            [FromQuery] DateTime toDate,
            [FromQuery] string invoiceType = "All")
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest(new SalesRegisterResponse
                    {
                        Success = false,
                        Message = "No active database connection found. Please set up your database connection first."
                    });
                }

                var fromDateString = fromDate.ToString("yyyy/MM/dd");
                var toDateString = toDate.ToString("yyyy/MM/dd");

                // Build invoice type filter
                string invoiceTypeFilter = "";
                if (invoiceType?.Trim() != "All")
                {
                    invoiceTypeFilter = $"AND si.inv_type = '{invoiceType.Trim().Replace("'", "''")}'";
                }

                // Sales Register SQL query - join sale_inv with sub_sinv to get detailed line items
                var salesRegisterQuery = $@"
                    SELECT 
                        si.inv_type as InvoiceType,
                        si.inv_no as InvoiceNo,
                        si.inv_date as Date,
                        RTRIM(si.ac_name) as Customer,
                        RTRIM(si.vehicle_no) as VehicleNo,
                        RTRIM(ss.item_name) as Item,
                        CASE 
                            WHEN ss.as_per = 'Mounds' OR ss.as_per = 'Kgs' THEN 
                                CASE 
                                    WHEN ss.packing = CAST(ss.packing AS INT) THEN CAST(CAST(ss.packing AS INT) AS VARCHAR) + ' Kgs'
                                    ELSE CAST(ss.packing AS VARCHAR) + ' Kgs'
                                END
                            ELSE 
                                CASE 
                                    WHEN ss.packing = CAST(ss.packing AS INT) THEN CAST(CAST(ss.packing AS INT) AS VARCHAR) + ' Unit'
                                    ELSE CAST(ss.packing AS VARCHAR) + ' Unit'
                                END
                        END as Packing,
                        ss.qty as Qty,
                        ss.total_wght as Weight,
                        ss.rate as Rate,
                        ss.grand_tot as Amount,
                        si.fare as Fare,
                        si.net_amt as NetAmt,
                        si.inv_no as InvoiceNoSort
                    FROM sale_inv si
                    INNER JOIN sub_sinv ss ON si.inv_no = ss.inv_no AND si.inv_type = ss.inv_type
                    WHERE si.inv_date >= '{fromDateString}' 
                      AND si.inv_date <= '{toDateString}'
                      {invoiceTypeFilter}
                    ORDER BY si.inv_date, si.inv_no, ss.item_code";

                var results = await _dataAccessService.ExecuteQueryAsync(user, salesRegisterQuery, commandTimeout: 120);
                
                var salesRegisterItems = new List<SalesRegisterItem>();
                decimal totalWeight = 0;
                decimal totalAmount = 0;
                decimal totalFare = 0;
                decimal totalNetAmt = 0;
                string currentInvoiceNo = "";
                decimal invoiceWeight = 0;
                decimal invoiceAmount = 0;
                decimal invoiceFare = 0;
                decimal invoiceNetAmt = 0;

                // Process each row
                foreach (var row in results)
                {
                    var invoiceNo = row.ContainsKey("InvoiceNo") ? row["InvoiceNo"]?.ToString()?.Trim() ?? "" : "";
                    
                    // If this is a new invoice and we have previous invoice data, add subtotal
                    if (!string.IsNullOrEmpty(currentInvoiceNo) && currentInvoiceNo != invoiceNo)
                    {
                        // Add subtotal row for previous invoice
                        salesRegisterItems.Add(new SalesRegisterItem
                        {
                            InvoiceType = "",
                            InvoiceNo = "",
                            Date = null,
                            Customer = "",
                            VehicleNo = "",
                            Item = "Total",
                            Packing = "",
                            Qty = 0,
                            Weight = invoiceWeight,
                            Rate = 0,
                            Amount = invoiceAmount,
                            Fare = invoiceFare,
                            NetAmt = invoiceNetAmt,
                            IsSubTotal = true
                        });
                        
                        invoiceWeight = 0;
                        invoiceAmount = 0;
                        invoiceFare = 0;
                        invoiceNetAmt = 0;
                    }

                    // Parse row data
                    var rowInvoiceType = row.ContainsKey("InvoiceType") ? row["InvoiceType"]?.ToString()?.Trim() ?? "" : "";
                    var date = row.ContainsKey("Date") && DateTime.TryParse(row["Date"]?.ToString(), out var d) ? d : (DateTime?)null;
                    var customer = row.ContainsKey("Customer") ? row["Customer"]?.ToString()?.Trim() ?? "" : "";
                    var vehicleNo = row.ContainsKey("VehicleNo") ? row["VehicleNo"]?.ToString()?.Trim() ?? "" : "";
                    var item = row.ContainsKey("Item") ? row["Item"]?.ToString()?.Trim() ?? "" : "";
                    var packingRaw = row.ContainsKey("Packing") ? row["Packing"]?.ToString()?.Trim() ?? "" : "";
                    // Format packing to remove .00 if it's a whole number
                    var packing = packingRaw;
                    if (!string.IsNullOrEmpty(packingRaw))
                    {
                        // Check if packing ends with " Kgs" or " Unit" and extract the number
                        if (packingRaw.EndsWith(" Kgs") || packingRaw.EndsWith(" Unit"))
                        {
                            var suffix = packingRaw.EndsWith(" Kgs") ? " Kgs" : " Unit";
                            var numberPart = packingRaw.Replace(suffix, "").Trim();
                            if (decimal.TryParse(numberPart, out var packingValue))
                            {
                                if (packingValue % 1 == 0)
                                {
                                    packing = $"{(int)packingValue}{suffix}";
                                }
                                else
                                {
                                    packing = packingRaw; // Keep original if has decimals
                                }
                            }
                        }
                    }
                    var qty = row.ContainsKey("Qty") && decimal.TryParse(row["Qty"]?.ToString(), out var q) ? q : 0;
                    var weight = row.ContainsKey("Weight") && decimal.TryParse(row["Weight"]?.ToString(), out var w) ? w : 0;
                    var rate = row.ContainsKey("Rate") && decimal.TryParse(row["Rate"]?.ToString(), out var r) ? r : 0;
                    var amount = row.ContainsKey("Amount") && decimal.TryParse(row["Amount"]?.ToString(), out var a) ? a : 0;
                    var fare = row.ContainsKey("Fare") && decimal.TryParse(row["Fare"]?.ToString(), out var f) ? f : 0;
                    var netAmt = row.ContainsKey("NetAmt") && decimal.TryParse(row["NetAmt"]?.ToString(), out var n) ? n : 0;

                    // For fare and netAmt, capture them once per invoice (on first item)
                    if (currentInvoiceNo != invoiceNo)
                    {
                        invoiceFare = fare;
                        invoiceNetAmt = netAmt;
                        totalFare += fare;
                        totalNetAmt += netAmt;
                    }

                    // Add item row (fare and netAmt are 0 for item rows, shown only in total row)
                    salesRegisterItems.Add(new SalesRegisterItem
                    {
                        InvoiceType = rowInvoiceType,
                        InvoiceNo = invoiceNo,
                        Date = date,
                        Customer = customer,
                        VehicleNo = vehicleNo,
                        Item = item,
                        Packing = packing,
                        Qty = qty,
                        Weight = weight,
                        Rate = rate,
                        Amount = amount,
                        Fare = 0, // Fare only shown in total row
                        NetAmt = 0, // NetAmt only shown in total row
                        IsSubTotal = false
                    });

                    invoiceWeight += weight;
                    invoiceAmount += amount;
                    totalWeight += weight;
                    totalAmount += amount;
                    currentInvoiceNo = invoiceNo;
                }

                // Add subtotal for last invoice if any
                if (!string.IsNullOrEmpty(currentInvoiceNo) && invoiceWeight > 0)
                {
                    salesRegisterItems.Add(new SalesRegisterItem
                    {
                        InvoiceType = "",
                        InvoiceNo = "",
                        Date = null,
                        Customer = "",
                        VehicleNo = "",
                        Item = "Total",
                        Packing = "",
                        Qty = 0,
                        Weight = invoiceWeight,
                        Rate = 0,
                        Amount = invoiceAmount,
                        Fare = invoiceFare,
                        NetAmt = invoiceNetAmt,
                        IsSubTotal = true
                    });
                }

                stopwatch.Stop();

                return Ok(new SalesRegisterResponse
                {
                    Success = true,
                    Message = "Sales Register retrieved successfully",
                    Data = salesRegisterItems,
                    FromDate = fromDate,
                    ToDate = toDate,
                    InvoiceType = invoiceType ?? "All",
                    TotalWeight = totalWeight,
                    TotalAmount = totalAmount,
                    TotalFare = totalFare,
                    TotalNetAmt = totalNetAmt
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error getting sales register");
                return StatusCode(500, new SalesRegisterResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        // Item Purchase Ledger Report
        [HttpGet("item-purchase-ledger")]
        public async Task<ActionResult<ItemPurchaseLedgerResponse>> GetItemPurchaseLedger(
            [FromQuery] DateTime fromDate, 
            [FromQuery] DateTime toDate,
            [FromQuery] string itemCode,
            [FromQuery] string itemName,
            [FromQuery] string? variety = null,
            [FromQuery] decimal? packSize = null,
            [FromQuery] string? status = null)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest(new ItemPurchaseLedgerResponse
                    {
                        Success = false,
                        Message = "No active database connection found. Please set up your database connection first."
                    });
                }

                if (string.IsNullOrWhiteSpace(itemCode) || string.IsNullOrWhiteSpace(itemName))
                {
                    return BadRequest(new ItemPurchaseLedgerResponse
                    {
                        Success = false,
                        Message = "Item Code and Item Name are required."
                    });
                }

                var fromDateString = fromDate.ToString("yyyy/MM/dd");
                var toDateString = toDate.ToString("yyyy/MM/dd");

                // Build item filter conditions
                var itemFilters = new List<string>
                {
                    $"sp.item_code = '{itemCode.Trim().Replace("'", "''")}'",
                    $"RTRIM(sp.item_name) = '{itemName.Trim().Replace("'", "''")}'"
                };

                if (!string.IsNullOrWhiteSpace(variety))
                {
                    itemFilters.Add($"RTRIM(sp.variety) = '{variety.Trim().Replace("'", "''")}'");
                }

                if (packSize.HasValue)
                {
                    itemFilters.Add($"sp.packing = {packSize.Value}");
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    itemFilters.Add($"RTRIM(sp.status) = '{status.Trim().Replace("'", "''")}'");
                }

                var itemFilterClause = string.Join(" AND ", itemFilters);

                // Item Purchase Ledger SQL query - join pur_inv with sub_pinv filtered by item
                var itemPurchaseLedgerQuery = $@"
                    SELECT 
                        pi.inv_type as InvoiceType,
                        pi.inv_no as InvoiceNo,
                        pi.inv_date as Date,
                        RTRIM(pi.ac_name) as Supplier,
                        RTRIM(pi.vehicle_no) as VehicleNo,
                        (sp.qty_jute + sp.qty_pp_100 + sp.qty_pp_50) as Qty,
                        sp.total_wght as Weight,
                        sp.rate as Rate,
                        RTRIM(sp.as_per) as AsPer,
                        sp.grand_tot as Amount,
                        RTRIM(sp.item_code) as ItemCode,
                        RTRIM(sp.item_name) as ItemName,
                        RTRIM(sp.variety) as Variety,
                        sp.packing as PackSize,
                        RTRIM(sp.status) as Status
                    FROM pur_inv pi
                    INNER JOIN sub_pinv sp ON pi.inv_no = sp.inv_no AND pi.inv_type = sp.inv_type
                    WHERE pi.inv_date >= '{fromDateString}' 
                      AND pi.inv_date <= '{toDateString}'
                      AND {itemFilterClause}
                    ORDER BY pi.inv_date, pi.inv_no, sp.sr_no";

                var results = await _dataAccessService.ExecuteQueryAsync(user, itemPurchaseLedgerQuery, commandTimeout: 120);
                
                var itemPurchaseLedgerItems = new List<ItemPurchaseLedgerItem>();
                decimal totalQty = 0;
                decimal totalWeight = 0;
                decimal totalAmount = 0;
                string actualItemCode = "";
                string actualItemName = "";
                string actualVariety = "";
                decimal actualPackSize = 0;
                string actualStatus = "";

                // Process each row
                foreach (var row in results)
                {
                    // Capture item details from first row
                    if (string.IsNullOrEmpty(actualItemCode) && row.ContainsKey("ItemCode"))
                    {
                        actualItemCode = row["ItemCode"]?.ToString()?.Trim() ?? itemCode;
                        actualItemName = row.ContainsKey("ItemName") ? row["ItemName"]?.ToString()?.Trim() ?? itemName : itemName;
                        actualVariety = row.ContainsKey("Variety") ? row["Variety"]?.ToString()?.Trim() ?? (variety ?? "") : (variety ?? "");
                        actualPackSize = row.ContainsKey("PackSize") && decimal.TryParse(row["PackSize"]?.ToString(), out var ps) ? ps : (packSize ?? 0);
                        actualStatus = row.ContainsKey("Status") ? row["Status"]?.ToString()?.Trim() ?? (status ?? "") : (status ?? "");
                    }

                    // Parse row data
                    var rowInvoiceType = row.ContainsKey("InvoiceType") ? row["InvoiceType"]?.ToString()?.Trim() ?? "" : "";
                    var invoiceNo = row.ContainsKey("InvoiceNo") ? row["InvoiceNo"]?.ToString()?.Trim() ?? "" : "";
                    var date = row.ContainsKey("Date") && DateTime.TryParse(row["Date"]?.ToString(), out var d) ? d : (DateTime?)null;
                    var supplier = row.ContainsKey("Supplier") ? row["Supplier"]?.ToString()?.Trim() ?? "" : "";
                    var vehicleNo = row.ContainsKey("VehicleNo") ? row["VehicleNo"]?.ToString()?.Trim() ?? "" : "";
                    var qty = row.ContainsKey("Qty") && decimal.TryParse(row["Qty"]?.ToString(), out var q) ? q : 0;
                    var weight = row.ContainsKey("Weight") && decimal.TryParse(row["Weight"]?.ToString(), out var w) ? w : 0;
                    var rate = row.ContainsKey("Rate") && decimal.TryParse(row["Rate"]?.ToString(), out var r) ? r : 0;
                    var asPer = row.ContainsKey("AsPer") ? row["AsPer"]?.ToString()?.Trim() ?? "" : "";
                    var amount = row.ContainsKey("Amount") && decimal.TryParse(row["Amount"]?.ToString(), out var a) ? a : 0;

                    // Add item row
                    itemPurchaseLedgerItems.Add(new ItemPurchaseLedgerItem
                    {
                        InvoiceType = rowInvoiceType,
                        InvoiceNo = invoiceNo,
                        Date = date,
                        Supplier = supplier,
                        VehicleNo = vehicleNo,
                        Qty = qty,
                        Weight = weight,
                        Rate = rate,
                        AsPer = asPer,
                        Amount = amount
                    });

                    totalQty += qty;
                    totalWeight += weight;
                    totalAmount += amount;
                }

                // Use actual values from database if available, otherwise use provided values
                if (string.IsNullOrEmpty(actualItemCode))
                {
                    actualItemCode = itemCode;
                    actualItemName = itemName;
                    actualVariety = variety ?? "";
                    actualPackSize = packSize ?? 0;
                    actualStatus = status ?? "";
                }

                stopwatch.Stop();

                return Ok(new ItemPurchaseLedgerResponse
                {
                    Success = true,
                    Message = "Item Purchase Ledger retrieved successfully",
                    Data = itemPurchaseLedgerItems,
                    FromDate = fromDate,
                    ToDate = toDate,
                    ItemCode = actualItemCode,
                    ItemName = actualItemName,
                    Variety = actualVariety,
                    PackSize = actualPackSize,
                    Status = actualStatus,
                    TotalQty = totalQty,
                    TotalWeight = totalWeight,
                    TotalAmount = totalAmount
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error getting item purchase ledger");
                return StatusCode(500, new ItemPurchaseLedgerResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        // Item Sales Ledger Report
        [HttpGet("item-sales-ledger")]
        public async Task<ActionResult<ItemSalesLedgerResponse>> GetItemSalesLedger(
            [FromQuery] DateTime fromDate, 
            [FromQuery] DateTime toDate,
            [FromQuery] string itemCode,
            [FromQuery] string itemName,
            [FromQuery] string? variety = null,
            [FromQuery] decimal? packSize = null,
            [FromQuery] string? status = null)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest(new ItemSalesLedgerResponse
                    {
                        Success = false,
                        Message = "No active database connection found. Please set up your database connection first."
                    });
                }

                if (string.IsNullOrWhiteSpace(itemCode) || string.IsNullOrWhiteSpace(itemName))
                {
                    return BadRequest(new ItemSalesLedgerResponse
                    {
                        Success = false,
                        Message = "Item Code and Item Name are required."
                    });
                }

                var fromDateString = fromDate.ToString("yyyy/MM/dd");
                var toDateString = toDate.ToString("yyyy/MM/dd");

                // Build item filter conditions
                var itemFilters = new List<string>
                {
                    $"ss.item_code = '{itemCode.Trim().Replace("'", "''")}'",
                    $"RTRIM(ss.item_name) = '{itemName.Trim().Replace("'", "''")}'"
                };

                if (!string.IsNullOrWhiteSpace(variety))
                {
                    itemFilters.Add($"RTRIM(ss.variety) = '{variety.Trim().Replace("'", "''")}'");
                }

                if (packSize.HasValue)
                {
                    itemFilters.Add($"ss.packing = {packSize.Value}");
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    itemFilters.Add($"RTRIM(ss.status) = '{status.Trim().Replace("'", "''")}'");
                }

                var itemFilterClause = string.Join(" AND ", itemFilters);

                // Item Sales Ledger SQL query - join sale_inv with sub_sinv filtered by item
                var itemSalesLedgerQuery = $@"
                    SELECT 
                        si.inv_no as InvoiceNo,
                        si.inv_date as Date,
                        ss.qty as Qty,
                        ss.total_wght as TotalWeight,
                        ss.rate as Rate,
                        RTRIM(ss.as_per) as AsPer,
                        ss.gros_amt as GrossAmount,
                        ss.disc_amt as Discount,
                        ss.grand_tot as NetAmount,
                        RTRIM(ss.item_code) as ItemCode,
                        RTRIM(ss.item_name) as ItemName,
                        RTRIM(ss.variety) as Variety,
                        ss.packing as PackSize,
                        RTRIM(ss.status) as Status
                    FROM sale_inv si
                    INNER JOIN sub_sinv ss ON si.inv_no = ss.inv_no AND si.inv_type = ss.inv_type
                    WHERE si.inv_date >= '{fromDateString}' 
                      AND si.inv_date <= '{toDateString}'
                      AND {itemFilterClause}
                    ORDER BY si.inv_date, si.inv_no, ss.item_code";

                var results = await _dataAccessService.ExecuteQueryAsync(user, itemSalesLedgerQuery, commandTimeout: 120);
                
                var itemSalesLedgerItems = new List<ItemSalesLedgerItem>();
                decimal totalQty = 0;
                decimal totalWeight = 0;
                decimal totalGrossAmount = 0;
                decimal totalDiscount = 0;
                decimal totalNetAmount = 0;
                string actualItemCode = "";
                string actualItemName = "";
                string actualVariety = "";
                decimal actualPackSize = 0;
                string actualStatus = "";

                string currentInvoiceNo = "";
                decimal invoiceQty = 0;
                decimal invoiceWeight = 0;
                decimal invoiceNetAmount = 0;

                // Process each row
                foreach (var row in results)
                {
                    // Capture item details from first row
                    if (string.IsNullOrEmpty(actualItemCode) && row.ContainsKey("ItemCode"))
                    {
                        actualItemCode = row["ItemCode"]?.ToString()?.Trim() ?? itemCode;
                        actualItemName = row.ContainsKey("ItemName") ? row["ItemName"]?.ToString()?.Trim() ?? itemName : itemName;
                        actualVariety = row.ContainsKey("Variety") ? row["Variety"]?.ToString()?.Trim() ?? (variety ?? "") : (variety ?? "");
                        actualPackSize = row.ContainsKey("PackSize") && decimal.TryParse(row["PackSize"]?.ToString(), out var ps) ? ps : (packSize ?? 0);
                        actualStatus = row.ContainsKey("Status") ? row["Status"]?.ToString()?.Trim() ?? (status ?? "") : (status ?? "");
                    }

                    // Parse row data
                    var invoiceNo = row.ContainsKey("InvoiceNo") ? row["InvoiceNo"]?.ToString()?.Trim() ?? "" : "";
                    var date = row.ContainsKey("Date") && DateTime.TryParse(row["Date"]?.ToString(), out var d) ? d : (DateTime?)null;
                    var qty = row.ContainsKey("Qty") && decimal.TryParse(row["Qty"]?.ToString(), out var q) ? q : 0;
                    var weight = row.ContainsKey("TotalWeight") && decimal.TryParse(row["TotalWeight"]?.ToString(), out var w) ? w : 0;
                    var rate = row.ContainsKey("Rate") && decimal.TryParse(row["Rate"]?.ToString(), out var r) ? r : 0;
                    var asPer = row.ContainsKey("AsPer") ? row["AsPer"]?.ToString()?.Trim() ?? "" : "";
                    var grossAmount = row.ContainsKey("GrossAmount") && decimal.TryParse(row["GrossAmount"]?.ToString(), out var ga) ? ga : 0;
                    var discount = row.ContainsKey("Discount") && decimal.TryParse(row["Discount"]?.ToString(), out var disc) ? disc : 0;
                    var netAmount = row.ContainsKey("NetAmount") && decimal.TryParse(row["NetAmount"]?.ToString(), out var na) ? na : 0;

                    // Check if we need to add subtotal for previous invoice
                    if (!string.IsNullOrEmpty(currentInvoiceNo) && currentInvoiceNo != invoiceNo)
                    {
                        // Add subtotal row for previous invoice
                        itemSalesLedgerItems.Add(new ItemSalesLedgerItem
                        {
                            InvoiceNo = "",
                            Date = null,
                            Qty = invoiceQty,
                            TotalWeight = invoiceWeight,
                            Rate = 0,
                            AsPer = "",
                            GrossAmount = 0,
                            Discount = 0,
                            NetAmount = invoiceNetAmount,
                            IsSubTotal = true
                        });

                        // Reset invoice totals
                        invoiceQty = 0;
                        invoiceWeight = 0;
                        invoiceNetAmount = 0;
                    }

                    // Add item row
                    itemSalesLedgerItems.Add(new ItemSalesLedgerItem
                    {
                        InvoiceNo = invoiceNo,
                        Date = date,
                        Qty = qty,
                        TotalWeight = weight,
                        Rate = rate,
                        AsPer = asPer,
                        GrossAmount = grossAmount,
                        Discount = discount,
                        NetAmount = netAmount,
                        IsSubTotal = false
                    });

                    // Accumulate invoice totals
                    invoiceQty += qty;
                    invoiceWeight += weight;
                    invoiceNetAmount += netAmount;

                    // Accumulate grand totals
                    totalQty += qty;
                    totalWeight += weight;
                    totalGrossAmount += grossAmount;
                    totalDiscount += discount;
                    totalNetAmount += netAmount;

                    currentInvoiceNo = invoiceNo;
                }

                // Add final subtotal if there are items
                if (!string.IsNullOrEmpty(currentInvoiceNo) && invoiceQty > 0)
                {
                    itemSalesLedgerItems.Add(new ItemSalesLedgerItem
                    {
                        InvoiceNo = "",
                        Date = null,
                        Qty = invoiceQty,
                        TotalWeight = invoiceWeight,
                        Rate = 0,
                        AsPer = "",
                        GrossAmount = 0,
                        Discount = 0,
                        NetAmount = invoiceNetAmount,
                        IsSubTotal = true
                    });
                }

                // Use actual values from database if available, otherwise use provided values
                if (string.IsNullOrEmpty(actualItemCode))
                {
                    actualItemCode = itemCode;
                    actualItemName = itemName;
                    actualVariety = variety ?? "";
                    actualPackSize = packSize ?? 0;
                    actualStatus = status ?? "";
                }

                // Calculate average rates
                decimal averageRatePerUnit = totalQty > 0 ? totalNetAmount / totalQty : 0;
                decimal averageRatePerKgs = totalWeight > 0 ? totalNetAmount / totalWeight : 0;
                decimal averageRatePerMounds = totalWeight > 0 ? totalNetAmount / (totalWeight / 40) : 0;

                stopwatch.Stop();

                return Ok(new ItemSalesLedgerResponse
                {
                    Success = true,
                    Message = "Item Sales Ledger retrieved successfully",
                    Data = itemSalesLedgerItems,
                    FromDate = fromDate,
                    ToDate = toDate,
                    ItemCode = actualItemCode,
                    ItemName = actualItemName,
                    Variety = actualVariety,
                    PackSize = actualPackSize,
                    Status = actualStatus,
                    TotalQty = totalQty,
                    TotalWeight = totalWeight,
                    TotalGrossAmount = totalGrossAmount,
                    TotalDiscount = totalDiscount,
                    TotalNetAmount = totalNetAmount,
                    AverageRatePerUnit = averageRatePerUnit,
                    AverageRatePerKgs = averageRatePerKgs,
                    AverageRatePerMounds = averageRatePerMounds
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error getting item sales ledger");
                return StatusCode(500, new ItemSalesLedgerResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        // Item Purchase Register Report
        [HttpGet("item-purchase-register")]
        public async Task<ActionResult<ItemPurchaseRegisterResponse>> GetItemPurchaseRegister(
            [FromQuery] DateTime fromDate, 
            [FromQuery] DateTime toDate)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest(new ItemPurchaseRegisterResponse
                    {
                        Success = false,
                        Message = "No active database connection found. Please set up your database connection first."
                    });
                }

                var fromDateString = fromDate.ToString("yyyy/MM/dd");
                var toDateString = toDate.ToString("yyyy/MM/dd");

                // Item Purchase Register SQL query - join pur_inv with sub_pinv, ordered by item
                var itemPurchaseRegisterQuery = $@"
                    SELECT 
                        pi.inv_no as InvoiceNo,
                        pi.inv_date as Date,
                        (sp.qty_jute + sp.qty_pp_100 + sp.qty_pp_50) as Qty,
                        sp.total_wght as TotalWeight,
                        sp.rate as Rate,
                        RTRIM(sp.as_per) as AsPer,
                        sp.grand_tot as NetAmt,
                        RTRIM(sp.item_name) as ItemName,
                        RTRIM(sp.variety) as Variety,
                        sp.packing as Packing,
                        RTRIM(sp.status) as Status
                    FROM pur_inv pi
                    INNER JOIN sub_pinv sp ON pi.inv_no = sp.inv_no AND pi.inv_type = sp.inv_type
                    WHERE pi.inv_date >= '{fromDateString}' 
                      AND pi.inv_date <= '{toDateString}'
                    ORDER BY sp.item_name, sp.variety, sp.packing, sp.status, pi.inv_date, pi.inv_no, sp.sr_no";

                var results = await _dataAccessService.ExecuteQueryAsync(user, itemPurchaseRegisterQuery, commandTimeout: 120);
                
                var itemPurchaseRegisterItems = new List<ItemPurchaseRegisterItem>();
                decimal grandTotalQty = 0;
                decimal grandTotalWeight = 0;
                decimal grandTotalAmount = 0;

                // Group results by item (ItemName + Variety + Packing + Status)
                var itemGroups = results
                    .GroupBy(row => new
                    {
                        ItemName = row.ContainsKey("ItemName") ? row["ItemName"]?.ToString()?.Trim() ?? "" : "",
                        Variety = row.ContainsKey("Variety") ? row["Variety"]?.ToString()?.Trim() ?? "" : "",
                        Packing = row.ContainsKey("Packing") && decimal.TryParse(row["Packing"]?.ToString(), out var p) ? p : 0,
                        Status = row.ContainsKey("Status") ? row["Status"]?.ToString()?.Trim() ?? "" : ""
                    })
                    .OrderBy(g => g.Key.ItemName)
                    .ThenBy(g => g.Key.Variety)
                    .ThenBy(g => g.Key.Packing)
                    .ThenBy(g => g.Key.Status)
                    .ToList();

                foreach (var itemGroup in itemGroups)
                {
                    var itemName = itemGroup.Key.ItemName;
                    var variety = itemGroup.Key.Variety;
                    var packing = itemGroup.Key.Packing;
                    var status = itemGroup.Key.Status;

                    // Get AsPer from first transaction in the group for header display
                    var firstRow = itemGroup.FirstOrDefault();
                    var asPerForHeader = firstRow != null && firstRow.ContainsKey("AsPer") 
                        ? firstRow["AsPer"]?.ToString()?.Trim() ?? "Unit" 
                        : "Unit";

                    // Add item header row
                    itemPurchaseRegisterItems.Add(new ItemPurchaseRegisterItem
                    {
                        ItemName = itemName,
                        Variety = variety,
                        Packing = packing,
                        Status = status,
                        AsPer = asPerForHeader,
                        IsItemHeader = true,
                        IsSubTotal = false
                    });

                    // Process transactions for this item
                    decimal itemTotalQty = 0;
                    decimal itemTotalWeight = 0;
                    decimal itemTotalAmount = 0;

                    foreach (var row in itemGroup.OrderBy(r => 
                        r.ContainsKey("Date") && DateTime.TryParse(r["Date"]?.ToString(), out var d) ? d : DateTime.MinValue)
                        .ThenBy(r => r.ContainsKey("InvoiceNo") ? r["InvoiceNo"]?.ToString() ?? "" : ""))
                    {
                        var invoiceNo = row.ContainsKey("InvoiceNo") ? row["InvoiceNo"]?.ToString()?.Trim() ?? "" : "";
                        var date = row.ContainsKey("Date") && DateTime.TryParse(row["Date"]?.ToString(), out var d) ? d : (DateTime?)null;
                        var qty = row.ContainsKey("Qty") && decimal.TryParse(row["Qty"]?.ToString(), out var q) ? q : 0;
                        var totalWeight = row.ContainsKey("TotalWeight") && decimal.TryParse(row["TotalWeight"]?.ToString(), out var w) ? w : 0;
                        var rate = row.ContainsKey("Rate") && decimal.TryParse(row["Rate"]?.ToString(), out var r) ? r : 0;
                        var asPer = row.ContainsKey("AsPer") ? row["AsPer"]?.ToString()?.Trim() ?? "" : "";
                        var netAmt = row.ContainsKey("NetAmt") && decimal.TryParse(row["NetAmt"]?.ToString(), out var a) ? a : 0;

                        // Add transaction row
                        itemPurchaseRegisterItems.Add(new ItemPurchaseRegisterItem
                        {
                            InvoiceNo = invoiceNo,
                            Date = date,
                            Qty = qty,
                            TotalWeight = totalWeight,
                            Rate = rate,
                            AsPer = asPer,
                            NetAmt = netAmt,
                            ItemName = itemName,
                            Variety = variety,
                            Packing = packing,
                            Status = status,
                            IsItemHeader = false,
                            IsSubTotal = false
                        });

                        itemTotalQty += qty;
                        itemTotalWeight += totalWeight;
                        itemTotalAmount += netAmt;
                    }

                    // Add subtotal row for this item
                    itemPurchaseRegisterItems.Add(new ItemPurchaseRegisterItem
                    {
                        ItemName = itemName,
                        Variety = variety,
                        Packing = packing,
                        Status = status,
                        Qty = itemTotalQty,
                        TotalWeight = itemTotalWeight,
                        NetAmt = itemTotalAmount,
                        IsItemHeader = false,
                        IsSubTotal = true
                    });

                    grandTotalQty += itemTotalQty;
                    grandTotalWeight += itemTotalWeight;
                    grandTotalAmount += itemTotalAmount;
                }

                stopwatch.Stop();

                return Ok(new ItemPurchaseRegisterResponse
                {
                    Success = true,
                    Message = "Item Purchase Register retrieved successfully",
                    Data = itemPurchaseRegisterItems,
                    FromDate = fromDate,
                    ToDate = toDate,
                    GrandTotalQty = grandTotalQty,
                    GrandTotalWeight = grandTotalWeight,
                    GrandTotalAmount = grandTotalAmount
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error getting item purchase register");
                return StatusCode(500, new ItemPurchaseRegisterResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        // Supplier Purchase Ledger Report
        [HttpGet("supplier-purchase-ledger")]
        public async Task<ActionResult<SupplierPurchaseLedgerResponse>> GetSupplierPurchaseLedger(
            [FromQuery] DateTime fromDate, 
            [FromQuery] DateTime toDate,
            [FromQuery] string? supplierAccount = null,
            [FromQuery] string reportType = "Summary",
            [FromQuery] bool allSuppliers = true)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest(new SupplierPurchaseLedgerResponse
                    {
                        Success = false,
                        Message = "No active database connection found. Please set up your database connection first."
                    });
                }

                var fromDateString = fromDate.ToString("yyyy/MM/dd");
                var toDateString = toDate.ToString("yyyy/MM/dd");

                // Build supplier filter
                string supplierFilter = "";
                string supplierName = "";
                
                if (!allSuppliers && !string.IsNullOrWhiteSpace(supplierAccount))
                {
                    supplierFilter = $"AND pi.ac_code = '{supplierAccount.Trim().Replace("'", "''")}'";
                    
                    // Get supplier name
                    var supplierNameQuery = $@"
                        SELECT TOP 1 RTRIM(ac_name) as SupplierName
                        FROM pur_inv
                        WHERE ac_code = '{supplierAccount.Trim().Replace("'", "''")}'
                        AND inv_date >= '{fromDateString}' 
                        AND inv_date <= '{toDateString}'
                    ";
                    var supplierNameResult = await _dataAccessService.ExecuteQueryAsync(user, supplierNameQuery, commandTimeout: 30);
                    if (supplierNameResult.Any() && supplierNameResult[0].ContainsKey("SupplierName"))
                    {
                        supplierName = supplierNameResult[0]["SupplierName"]?.ToString()?.Trim() ?? "";
                    }
                }

                // Base query for both Detail and Summary modes
                string baseQuery;
                
                if (reportType == "Detail")
                {
                    // Detail mode: Show individual invoice lines
                    baseQuery = $@"
                        SELECT 
                            pi.ac_code as SupplierAccount,
                            RTRIM(pi.ac_name) as SupplierName,
                            pi.inv_no as InvoiceNo,
                            pi.inv_date as Date,
                            RTRIM(pi.vehicle_no) as VehicleNo,
                            (sp.qty_jute + sp.qty_pp_100 + sp.qty_pp_50) as Qty,
                            sp.total_wght as TotalWeight,
                            sp.rate as Rate,
                            RTRIM(sp.as_per) as AsPer,
                            sp.grand_tot as NetAmt,
                            RTRIM(sp.item_name) as ItemName,
                            RTRIM(sp.variety) as ItemDescription,
                            sp.packing as Packing
                        FROM pur_inv pi
                        INNER JOIN sub_pinv sp ON pi.inv_no = sp.inv_no AND pi.inv_type = sp.inv_type
                        WHERE pi.inv_date >= '{fromDateString}' 
                          AND pi.inv_date <= '{toDateString}'
                          {supplierFilter}
                        ORDER BY pi.ac_code, pi.ac_name, pi.inv_date, pi.inv_no, sp.sr_no";
                }
                else
                {
                    // Summary mode: Group by supplier and item
                    baseQuery = $@"
                        SELECT 
                            pi.ac_code as SupplierAccount,
                            RTRIM(pi.ac_name) as SupplierName,
                            RTRIM(sp.item_code) as ItemCode,
                            RTRIM(sp.item_name) as ItemName,
                            sp.packing as PackSize,
                            SUM(sp.qty_jute + sp.qty_pp_100 + sp.qty_pp_50) as Qty,
                            SUM(sp.total_wght) as Weight,
                            SUM(sp.grand_tot) as Amount,
                            CASE 
                                WHEN SUM(sp.qty_jute + sp.qty_pp_100 + sp.qty_pp_50) > 0 
                                THEN SUM(sp.grand_tot) / SUM(sp.qty_jute + sp.qty_pp_100 + sp.qty_pp_50)
                                ELSE 0 
                            END as AverageRate,
                            CASE 
                                WHEN SUM(sp.qty_jute + sp.qty_pp_100 + sp.qty_pp_50) > 0 
                                THEN SUM(sp.total_wght) / SUM(sp.qty_jute + sp.qty_pp_100 + sp.qty_pp_50)
                                ELSE 0 
                            END as AverageKgs
                        FROM pur_inv pi
                        INNER JOIN sub_pinv sp ON pi.inv_no = sp.inv_no AND pi.inv_type = sp.inv_type
                        WHERE pi.inv_date >= '{fromDateString}' 
                          AND pi.inv_date <= '{toDateString}'
                          {supplierFilter}
                        GROUP BY pi.ac_code, pi.ac_name, sp.item_code, sp.item_name, sp.packing
                        ORDER BY pi.ac_code, pi.ac_name, sp.item_name, sp.packing";
                }

                var results = await _dataAccessService.ExecuteQueryAsync(user, baseQuery, commandTimeout: 120);
                
                var groups = new Dictionary<string, SupplierPurchaseLedgerGroup>();
                decimal grandTotalQty = 0;
                decimal grandTotalWeight = 0;
                decimal grandTotalAmount = 0;

                foreach (var row in results)
                {
                    var supplierAcc = row.ContainsKey("SupplierAccount") ? row["SupplierAccount"]?.ToString()?.Trim() ?? "" : "";
                    var supplierNm = row.ContainsKey("SupplierName") ? row["SupplierName"]?.ToString()?.Trim() ?? "" : "";
                    
                    if (string.IsNullOrEmpty(supplierAcc))
                        continue;

                    // Get or create group
                    if (!groups.ContainsKey(supplierAcc))
                    {
                        groups[supplierAcc] = new SupplierPurchaseLedgerGroup
                        {
                            SupplierAccount = supplierAcc,
                            SupplierName = supplierNm
                        };
                    }

                    var group = groups[supplierAcc];

                    if (reportType == "Detail")
                    {
                        var invoiceNo = row.ContainsKey("InvoiceNo") ? row["InvoiceNo"]?.ToString()?.Trim() ?? "" : "";
                        var date = row.ContainsKey("Date") && DateTime.TryParse(row["Date"]?.ToString(), out var d) ? d : (DateTime?)null;
                        var vehicleNo = row.ContainsKey("VehicleNo") ? row["VehicleNo"]?.ToString()?.Trim() ?? "" : "";
                        var qty = row.ContainsKey("Qty") && decimal.TryParse(row["Qty"]?.ToString(), out var q) ? q : 0;
                        var totalWeight = row.ContainsKey("TotalWeight") && decimal.TryParse(row["TotalWeight"]?.ToString(), out var w) ? w : 0;
                        var rate = row.ContainsKey("Rate") && decimal.TryParse(row["Rate"]?.ToString(), out var r) ? r : 0;
                        var asPer = row.ContainsKey("AsPer") ? row["AsPer"]?.ToString()?.Trim() ?? "" : "";
                        var netAmt = row.ContainsKey("NetAmt") && decimal.TryParse(row["NetAmt"]?.ToString(), out var a) ? a : 0;
                        var itemName = row.ContainsKey("ItemName") ? row["ItemName"]?.ToString()?.Trim() ?? "" : "";
                        var itemDesc = row.ContainsKey("ItemDescription") ? row["ItemDescription"]?.ToString()?.Trim() ?? "" : "";
                        var packing = row.ContainsKey("Packing") && decimal.TryParse(row["Packing"]?.ToString(), out var p) ? p : 0;

                        group.DetailItems.Add(new SupplierPurchaseLedgerDetailItem
                        {
                            InvoiceNo = invoiceNo,
                            Date = date,
                            VehicleNo = vehicleNo,
                            Qty = qty,
                            TotalWeight = totalWeight,
                            Rate = rate,
                            AsPer = asPer,
                            NetAmt = netAmt,
                            ItemName = itemName,
                            ItemDescription = itemDesc,
                            Packing = packing
                        });

                        group.TotalQty += qty;
                        group.TotalWeight += totalWeight;
                        group.TotalAmount += netAmt;
                    }
                    else
                    {
                        var itemCode = row.ContainsKey("ItemCode") ? row["ItemCode"]?.ToString()?.Trim() ?? "" : "";
                        var itemName = row.ContainsKey("ItemName") ? row["ItemName"]?.ToString()?.Trim() ?? "" : "";
                        var packSize = row.ContainsKey("PackSize") && decimal.TryParse(row["PackSize"]?.ToString(), out var ps) ? ps : 0;
                        var qty = row.ContainsKey("Qty") && decimal.TryParse(row["Qty"]?.ToString(), out var q) ? q : 0;
                        var weight = row.ContainsKey("Weight") && decimal.TryParse(row["Weight"]?.ToString(), out var w) ? w : 0;
                        var amount = row.ContainsKey("Amount") && decimal.TryParse(row["Amount"]?.ToString(), out var a) ? a : 0;
                        var avgRate = row.ContainsKey("AverageRate") && decimal.TryParse(row["AverageRate"]?.ToString(), out var ar) ? ar : 0;
                        var avgKgs = row.ContainsKey("AverageKgs") && decimal.TryParse(row["AverageKgs"]?.ToString(), out var ak) ? ak : 0;

                        group.SummaryItems.Add(new SupplierPurchaseLedgerSummaryItem
                        {
                            ItemCode = itemCode,
                            ItemName = itemName,
                            PackSize = packSize,
                            Qty = qty,
                            Weight = weight,
                            Amount = amount,
                            AverageRate = avgRate,
                            AverageKgs = avgKgs
                        });

                        group.TotalQty += qty;
                        group.TotalWeight += weight;
                        group.TotalAmount += amount;
                    }
                }

                // Calculate grand totals
                foreach (var group in groups.Values)
                {
                    grandTotalQty += group.TotalQty;
                    grandTotalWeight += group.TotalWeight;
                    grandTotalAmount += group.TotalAmount;
                }

                stopwatch.Stop();

                return Ok(new SupplierPurchaseLedgerResponse
                {
                    Success = true,
                    Message = "Supplier Purchase Ledger retrieved successfully",
                    Data = groups.Values.ToList(),
                    FromDate = fromDate,
                    ToDate = toDate,
                    SupplierAccount = supplierAccount ?? "",
                    SupplierName = supplierName,
                    ReportType = reportType,
                    GrandTotalQty = grandTotalQty,
                    GrandTotalWeight = grandTotalWeight,
                    GrandTotalAmount = grandTotalAmount
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error getting supplier purchase ledger");
                return StatusCode(500, new SupplierPurchaseLedgerResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        // Supplier Tax Ledger Report
        [HttpGet("supplier-tax-ledger")]
        public async Task<ActionResult<SupplierTaxLedgerResponse>> GetSupplierTaxLedger(
            [FromQuery] DateTime fromDate, 
            [FromQuery] DateTime toDate,
            [FromQuery] string? fromAccount = null,
            [FromQuery] string? uptoAccount = null,
            [FromQuery] bool taxCalculateAsPerBag = false,
            [FromQuery] decimal taxRatePerBag = 0,
            [FromQuery] string reportType = "Detail")
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest(new SupplierTaxLedgerResponse
                    {
                        Success = false,
                        Message = "No active database connection found. Please set up your database connection first."
                    });
                }

                // Set defaults
                var fromAcc = fromAccount ?? "1-00-00-0000";
                var uptoAcc = uptoAccount ?? "1-99-99-9999";

                var fromDateString = fromDate.ToString("yyyy/MM/dd");
                var toDateString = toDate.ToString("yyyy/MM/dd");

                // Convert account codes to numeric for range filtering
                var fromAccountNumeric = long.Parse(fromAcc.Replace("-", ""));
                var uptoAccountNumeric = long.Parse(uptoAcc.Replace("-", ""));

                string baseQuery;
                
                if (reportType == "Detail")
                {
                    // Detail mode: Show invoice-level details
                    if (taxCalculateAsPerBag)
                    {
                        // Per-bag calculation: Join with sub_pinv to get bag counts
                        baseQuery = $@"
                            SELECT 
                                pi.ac_code as SupplierAccount,
                                RTRIM(pi.ac_name) as SupplierName,
                                RTRIM(pi.ntn_no) as NTN,
                                pi.inv_no as InvoiceNo,
                                pi.inv_date as Date,
                                pi.grand_tot as InvAmount,
                                pi.commission as Commission,
                                {taxRatePerBag} as Tax1Rate,
                                (bag_counts.TotalBags * {taxRatePerBag}) as Tax1Amount,
                                pi.ot_taxrate as Tax2Rate,
                                pi.ot_taxamt as Tax2Amount,
                                (bag_counts.TotalBags * {taxRatePerBag} + pi.ot_taxamt) as Total
                            FROM pur_inv pi
                            INNER JOIN (
                                SELECT 
                                    inv_no,
                                    inv_type,
                                    SUM(qty_jute + qty_pp_100 + qty_pp_50) as TotalBags
                                FROM sub_pinv
                                GROUP BY inv_no, inv_type
                            ) bag_counts ON pi.inv_no = bag_counts.inv_no AND pi.inv_type = bag_counts.inv_type
                            WHERE pi.inv_date >= '{fromDateString}' 
                              AND pi.inv_date <= '{toDateString}'
                              AND CAST(REPLACE(pi.ac_code, '-', '') AS BIGINT) >= {fromAccountNumeric}
                              AND CAST(REPLACE(pi.ac_code, '-', '') AS BIGINT) <= {uptoAccountNumeric}
                            ORDER BY pi.ac_code, pi.ac_name, pi.inv_date, pi.inv_no";
                    }
                    else
                    {
                        // Normal calculation: Use tax_amount from pur_inv
                        baseQuery = $@"
                            SELECT 
                                pi.ac_code as SupplierAccount,
                                RTRIM(pi.ac_name) as SupplierName,
                                RTRIM(pi.ntn_no) as NTN,
                                pi.inv_no as InvoiceNo,
                                pi.inv_date as Date,
                                pi.grand_tot as InvAmount,
                                pi.commission as Commission,
                                pi.tax_rate as Tax1Rate,
                                pi.tax_amount as Tax1Amount,
                                pi.ot_taxrate as Tax2Rate,
                                pi.ot_taxamt as Tax2Amount,
                                (pi.tax_amount + pi.ot_taxamt) as Total
                            FROM pur_inv pi
                            WHERE pi.inv_date >= '{fromDateString}' 
                              AND pi.inv_date <= '{toDateString}'
                              AND CAST(REPLACE(pi.ac_code, '-', '') AS BIGINT) >= {fromAccountNumeric}
                              AND CAST(REPLACE(pi.ac_code, '-', '') AS BIGINT) <= {uptoAccountNumeric}
                            ORDER BY pi.ac_code, pi.ac_name, pi.inv_date, pi.inv_no";
                    }
                }
                else
                {
                    // Summary mode: Aggregate by supplier
                    if (taxCalculateAsPerBag)
                    {
                        // Per-bag calculation
                        baseQuery = $@"
                            SELECT 
                                pi.ac_code as SupplierAccount,
                                RTRIM(pi.ac_name) as SupplierName,
                                RTRIM(pi.ntn_no) as NTN,
                                SUM(pi.grand_tot) as Amount,
                                SUM(bag_counts.TotalWeight) as Weight,
                                SUM(pi.commission) as Commission,
                                SUM(bag_counts.TotalBags * {taxRatePerBag}) as IncomeTax
                            FROM pur_inv pi
                            INNER JOIN (
                                SELECT 
                                    inv_no,
                                    inv_type,
                                    SUM(qty_jute + qty_pp_100 + qty_pp_50) as TotalBags,
                                    SUM(total_wght) as TotalWeight
                                FROM sub_pinv
                                GROUP BY inv_no, inv_type
                            ) bag_counts ON pi.inv_no = bag_counts.inv_no AND pi.inv_type = bag_counts.inv_type
                            WHERE pi.inv_date >= '{fromDateString}' 
                              AND pi.inv_date <= '{toDateString}'
                              AND CAST(REPLACE(pi.ac_code, '-', '') AS BIGINT) >= {fromAccountNumeric}
                              AND CAST(REPLACE(pi.ac_code, '-', '') AS BIGINT) <= {uptoAccountNumeric}
                            GROUP BY pi.ac_code, pi.ac_name, pi.ntn_no
                            ORDER BY pi.ac_code, pi.ac_name";
                    }
                    else
                    {
                        // Normal calculation
                        baseQuery = $@"
                            SELECT 
                                pi.ac_code as SupplierAccount,
                                RTRIM(pi.ac_name) as SupplierName,
                                RTRIM(pi.ntn_no) as NTN,
                                SUM(pi.grand_tot) as Amount,
                                SUM(sp.total_wght) as Weight,
                                SUM(pi.commission) as Commission,
                                SUM(pi.tax_amount + pi.ot_taxamt) as IncomeTax
                            FROM pur_inv pi
                            LEFT JOIN sub_pinv sp ON pi.inv_no = sp.inv_no AND pi.inv_type = sp.inv_type
                            WHERE pi.inv_date >= '{fromDateString}' 
                              AND pi.inv_date <= '{toDateString}'
                              AND CAST(REPLACE(pi.ac_code, '-', '') AS BIGINT) >= {fromAccountNumeric}
                              AND CAST(REPLACE(pi.ac_code, '-', '') AS BIGINT) <= {uptoAccountNumeric}
                            GROUP BY pi.ac_code, pi.ac_name, pi.ntn_no
                            ORDER BY pi.ac_code, pi.ac_name";
                    }
                }

                var results = await _dataAccessService.ExecuteQueryAsync(user, baseQuery, commandTimeout: 120);
                
                var groups = new Dictionary<string, SupplierTaxLedgerGroup>();
                decimal grandTotal = 0;

                foreach (var row in results)
                {
                    var supplierAcc = row.ContainsKey("SupplierAccount") ? row["SupplierAccount"]?.ToString()?.Trim() ?? "" : "";
                    var supplierNm = row.ContainsKey("SupplierName") ? row["SupplierName"]?.ToString()?.Trim() ?? "" : "";
                    var ntn = row.ContainsKey("NTN") ? row["NTN"]?.ToString()?.Trim() ?? "" : "";
                    
                    if (string.IsNullOrEmpty(supplierAcc))
                        continue;

                    // Get or create group
                    if (!groups.ContainsKey(supplierAcc))
                    {
                        groups[supplierAcc] = new SupplierTaxLedgerGroup
                        {
                            SupplierAccount = supplierAcc,
                            SupplierName = supplierNm,
                            NTN = ntn
                        };
                    }

                    var group = groups[supplierAcc];

                    if (reportType == "Detail")
                    {
                        var invoiceNo = row.ContainsKey("InvoiceNo") ? row["InvoiceNo"]?.ToString()?.Trim() ?? "" : "";
                        var date = row.ContainsKey("Date") && DateTime.TryParse(row["Date"]?.ToString(), out var d) ? d : (DateTime?)null;
                        var invAmount = row.ContainsKey("InvAmount") && decimal.TryParse(row["InvAmount"]?.ToString(), out var ia) ? ia : 0;
                        var commission = row.ContainsKey("Commission") && decimal.TryParse(row["Commission"]?.ToString(), out var c) ? c : 0;
                        var tax1Rate = row.ContainsKey("Tax1Rate") && decimal.TryParse(row["Tax1Rate"]?.ToString(), out var t1r) ? t1r : 0;
                        var tax1Amount = row.ContainsKey("Tax1Amount") && decimal.TryParse(row["Tax1Amount"]?.ToString(), out var t1a) ? t1a : 0;
                        var tax2Rate = row.ContainsKey("Tax2Rate") && decimal.TryParse(row["Tax2Rate"]?.ToString(), out var t2r) ? t2r : 0;
                        var tax2Amount = row.ContainsKey("Tax2Amount") && decimal.TryParse(row["Tax2Amount"]?.ToString(), out var t2a) ? t2a : 0;
                        var total = row.ContainsKey("Total") && decimal.TryParse(row["Total"]?.ToString(), out var t) ? t : 0;

                        group.DetailItems.Add(new SupplierTaxLedgerDetailItem
                        {
                            InvoiceNo = invoiceNo,
                            Date = date,
                            InvAmount = invAmount,
                            Commission = commission,
                            Tax1Rate = tax1Rate,
                            Tax1Amount = tax1Amount,
                            Tax2Rate = tax2Rate,
                            Tax2Amount = tax2Amount,
                            Total = total
                        });

                        group.SubTotal += total;
                        grandTotal += total;
                    }
                    else
                    {
                        var amount = row.ContainsKey("Amount") && decimal.TryParse(row["Amount"]?.ToString(), out var a) ? a : 0;
                        var weight = row.ContainsKey("Weight") && decimal.TryParse(row["Weight"]?.ToString(), out var w) ? w : 0;
                        var commission = row.ContainsKey("Commission") && decimal.TryParse(row["Commission"]?.ToString(), out var c) ? c : 0;
                        var incomeTax = row.ContainsKey("IncomeTax") && decimal.TryParse(row["IncomeTax"]?.ToString(), out var it) ? it : 0;

                        group.SummaryItem = new SupplierTaxLedgerSummaryItem
                        {
                            SupplierName = supplierNm,
                            NTN = ntn,
                            Amount = amount,
                            Weight = weight,
                            Commission = commission,
                            IncomeTax = incomeTax
                        };

                        group.SubTotal = incomeTax;
                        grandTotal += incomeTax;
                    }
                }

                stopwatch.Stop();

                return Ok(new SupplierTaxLedgerResponse
                {
                    Success = true,
                    Message = "Supplier Tax Ledger retrieved successfully",
                    Data = groups.Values.ToList(),
                    FromDate = fromDate,
                    ToDate = toDate,
                    FromAccount = fromAcc,
                    UptoAccount = uptoAcc,
                    TaxCalculateAsPerBag = taxCalculateAsPerBag,
                    TaxRatePerBag = taxRatePerBag,
                    ReportType = reportType,
                    GrandTotal = grandTotal
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error getting supplier tax ledger");
                return StatusCode(500, new SupplierTaxLedgerResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        // Customer Aging Report
        [HttpGet("customer-aging")]
        public async Task<ActionResult<CustomerAgingResponse>> GetCustomerAging(
            [FromQuery] string? fromAccount = null,
            [FromQuery] string? uptoAccount = null,
            [FromQuery] DateTime? asOnDate = null,
            [FromQuery] string? reportType = null)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest(new CustomerAgingResponse
                    {
                        Success = false,
                        Message = "No active database connection found. Please set up your database connection first."
                    });
                }

                // Set defaults
                var fromAcc = fromAccount ?? "1-01-01-0000";
                var uptoAcc = uptoAccount ?? "1-01-01-9999";
                var asOn = asOnDate ?? DateTime.Now;
                var reportTypeValue = reportType?.ToLower() ?? "detailed";

                var asOnDateString = asOn.ToString("yyyy/MM/dd");

                // Convert account codes to numeric for range filtering
                var fromAccountNumeric = long.Parse(fromAcc.Replace("-", ""));
                var uptoAccountNumeric = long.Parse(uptoAcc.Replace("-", ""));

                if (reportTypeValue == "summary")
                {

                    // Simplified query for summary
                    var summaryQuerySimple = $@"
                        declare @asOnDate as date
                        set @asOnDate = '{asOnDateString}'

                        SELECT 
                            si.ac_code as AccCode,
                            si.ac_name as AccName,
                            si.inv_no as BillNo,
                            si.inv_date as BillDate,
                            si.grand_tot as BillAmount,
                            ISNULL((
                                SELECT SUM(ISNULL(cr_amount, 0))
                                FROM view_gjournal vg
                                WHERE vg.acc_code = si.ac_code
                                    AND vg.vouch_date <= @asOnDate
                                    AND LEFT(vg.vouch_no, 2) = 'SV'
                                    AND LTRIM(RIGHT(vg.vouch_no, LEN(vg.vouch_no) - 2), '0') = LTRIM(si.inv_no, '0')
                            ), 0) as PaidAmount,
                            si.grand_tot - ISNULL((
                                SELECT SUM(ISNULL(cr_amount, 0))
                                FROM view_gjournal vg
                                WHERE vg.acc_code = si.ac_code
                                    AND vg.vouch_date <= @asOnDate
                                    AND LEFT(vg.vouch_no, 2) = 'SV'
                                    AND LTRIM(RIGHT(vg.vouch_no, LEN(vg.vouch_no) - 2), '0') = LTRIM(si.inv_no, '0')
                            ), 0) as Pending,
                            DATEDIFF(day, si.inv_date, @asOnDate) as Days
                        FROM sale_inv si
                        WHERE CAST(REPLACE(si.ac_code, '-', '') AS BIGINT) >= {fromAccountNumeric}
                          AND CAST(REPLACE(si.ac_code, '-', '') AS BIGINT) <= {uptoAccountNumeric}
                          AND si.inv_date <= @asOnDate
                          AND si.grand_tot - ISNULL((
                                SELECT SUM(ISNULL(cr_amount, 0))
                                FROM view_gjournal vg
                                WHERE vg.acc_code = si.ac_code
                                    AND vg.vouch_date <= @asOnDate
                                    AND LEFT(vg.vouch_no, 2) = 'SV'
                                    AND LTRIM(RIGHT(vg.vouch_no, LEN(vg.vouch_no) - 2), '0') = LTRIM(si.inv_no, '0')
                            ), 0) > 0
                        ORDER BY si.ac_code, si.inv_date";

                    var results = await _dataAccessService.ExecuteQueryAsync(user, summaryQuerySimple, commandTimeout: 120);

                    // Group by customer and calculate aging buckets
                    var customerGroups = results
                        .GroupBy(row => 
                            row.ContainsKey("AccCode") ? row["AccCode"]?.ToString() ?? "" : ""
                        )
                        .Where(g => !string.IsNullOrEmpty(g.Key))
                        .ToList();

                    var summaryItems = new List<CustomerAgingSummaryItem>();
                    decimal totalBalance = 0;
                    decimal total1To30 = 0;
                    decimal total31To60 = 0;
                    decimal total61To90 = 0;
                    decimal total91To120 = 0;
                    decimal totalAbove120 = 0;

                    foreach (var customerGroup in customerGroups)
                    {
                        var firstRow = customerGroup.First();
                        var accCode = firstRow.ContainsKey("AccCode") ? firstRow["AccCode"]?.ToString() ?? "" : "";
                        var accName = firstRow.ContainsKey("AccName") ? firstRow["AccName"]?.ToString() ?? "" : "";
                        
                        decimal balance = 0;
                        decimal days1To30 = 0;
                        decimal days31To60 = 0;
                        decimal days61To90 = 0;
                        decimal days91To120 = 0;
                        decimal above120 = 0;

                        foreach (var row in customerGroup)
                        {
                            var pending = row.ContainsKey("Pending") && decimal.TryParse(row["Pending"]?.ToString(), out var p) ? p : 0;
                            var days = row.ContainsKey("Days") && int.TryParse(row["Days"]?.ToString(), out var d) ? d : 0;

                            balance += pending;

                            if (days <= 30)
                                days1To30 += pending;
                            else if (days <= 60)
                                days31To60 += pending;
                            else if (days <= 90)
                                days61To90 += pending;
                            else if (days <= 120)
                                days91To120 += pending;
                            else
                                above120 += pending;
                        }

                        if (balance > 0)
                        {
                            summaryItems.Add(new CustomerAgingSummaryItem
                            {
                                AccCode = accCode,
                                AccName = accName,
                                Balance = balance,
                                Days1To30 = days1To30,
                                Days31To60 = days31To60,
                                Days61To90 = days61To90,
                                Days91To120 = days91To120,
                                Above120 = above120
                            });

                            totalBalance += balance;
                            total1To30 += days1To30;
                            total31To60 += days31To60;
                            total61To90 += days61To90;
                            total91To120 += days91To120;
                            totalAbove120 += above120;
                        }
                    }

                    stopwatch.Stop();

                    return Ok(new CustomerAgingResponse
                    {
                        Success = true,
                        Message = "Customer Aging Summary Report retrieved successfully",
                        ReportType = "Summary",
                        SummaryData = summaryItems.OrderBy(x => x.AccCode).ToList(),
                        AsOnDate = asOn,
                        FromAccount = fromAcc,
                        UptoAccount = uptoAcc,
                        TotalBalance = totalBalance,
                        TotalDays1To30 = total1To30,
                        TotalDays31To60 = total31To60,
                        TotalDays61To90 = total61To90,
                        TotalDays91To120 = total91To120,
                        TotalAbove120 = totalAbove120
                    });
                }
                else
                {
                    // Detailed Report - Show individual bills
                    var detailedQuery = $@"
                        declare @asOnDate as date
                        set @asOnDate = '{asOnDateString}'

                        SELECT 
                            si.ac_code as AccCode,
                            si.ac_name as AccName,
                            CASE 
                                WHEN si.inv_no LIKE 'OB-%' THEN si.inv_no
                                ELSE 'SV' + RIGHT('000000' + CAST(si.inv_no AS VARCHAR(6)), 6)
                            END as BillNo,
                            si.inv_date as BillDate,
                            NULL as DueDate,
                            si.grand_tot as BillAmount,
                            ISNULL((
                                SELECT SUM(ISNULL(cr_amount, 0))
                                FROM view_gjournal vg
                                WHERE vg.acc_code = si.ac_code
                                    AND vg.vouch_date <= @asOnDate
                                    AND (
                                        (LEFT(vg.vouch_no, 2) = 'SV' AND LTRIM(RIGHT(vg.vouch_no, LEN(vg.vouch_no) - 2), '0') = LTRIM(si.inv_no, '0'))
                                        OR (LEFT(vg.vouch_no, 2) = 'OB' AND vg.vouch_no = si.inv_no)
                                    )
                            ), 0) as PaidAmount,
                            si.grand_tot - ISNULL((
                                SELECT SUM(ISNULL(cr_amount, 0))
                                FROM view_gjournal vg
                                WHERE vg.acc_code = si.ac_code
                                    AND vg.vouch_date <= @asOnDate
                                    AND (
                                        (LEFT(vg.vouch_no, 2) = 'SV' AND LTRIM(RIGHT(vg.vouch_no, LEN(vg.vouch_no) - 2), '0') = LTRIM(si.inv_no, '0'))
                                        OR (LEFT(vg.vouch_no, 2) = 'OB' AND vg.vouch_no = si.inv_no)
                                    )
                            ), 0) as Pending,
                            DATEDIFF(day, si.inv_date, @asOnDate) as Days
                        FROM sale_inv si
                        WHERE CAST(REPLACE(si.ac_code, '-', '') AS BIGINT) >= {fromAccountNumeric}
                          AND CAST(REPLACE(si.ac_code, '-', '') AS BIGINT) <= {uptoAccountNumeric}
                          AND si.inv_date <= @asOnDate
                          AND si.grand_tot - ISNULL((
                                SELECT SUM(ISNULL(cr_amount, 0))
                                FROM view_gjournal vg
                                WHERE vg.acc_code = si.ac_code
                                    AND vg.vouch_date <= @asOnDate
                                    AND (
                                        (LEFT(vg.vouch_no, 2) = 'SV' AND LTRIM(RIGHT(vg.vouch_no, LEN(vg.vouch_no) - 2), '0') = LTRIM(si.inv_no, '0'))
                                        OR (LEFT(vg.vouch_no, 2) = 'OB' AND vg.vouch_no = si.inv_no)
                                    )
                            ), 0) > 0
                        ORDER BY si.ac_code, si.inv_date";

                    // Also need to handle opening balances from view_gjournal directly
                    var openingBalanceQuery = $@"
                        declare @asOnDate as date
                        set @asOnDate = '{asOnDateString}'

                        SELECT 
                            acc_code as AccCode,
                            ISNULL((
                                SELECT acc_name FROM customer WHERE acc_code = vg.acc_code
                            ), '') as AccName,
                            vouch_no as BillNo,
                            vouch_date as BillDate,
                            NULL as DueDate,
                            CASE WHEN SUM(dr_amount) - SUM(cr_amount) > 0 
                                THEN SUM(dr_amount) - SUM(cr_amount) 
                                ELSE 0 END as BillAmount,
                            0 as PaidAmount,
                            CASE WHEN SUM(dr_amount) - SUM(cr_amount) > 0 
                                THEN SUM(dr_amount) - SUM(cr_amount) 
                                ELSE 0 END as Pending,
                            DATEDIFF(day, vouch_date, @asOnDate) as Days
                        FROM view_gjournal vg
                        WHERE LEFT(vouch_no, 2) = 'OB'
                            AND vouch_date <= @asOnDate
                            AND CAST(REPLACE(acc_code, '-', '') AS BIGINT) >= {fromAccountNumeric}
                            AND CAST(REPLACE(acc_code, '-', '') AS BIGINT) <= {uptoAccountNumeric}
                            AND acc_code IN (
                                SELECT DISTINCT ac_code FROM sale_inv 
                                WHERE CAST(REPLACE(ac_code, '-', '') AS BIGINT) >= {fromAccountNumeric}
                                  AND CAST(REPLACE(ac_code, '-', '') AS BIGINT) <= {uptoAccountNumeric}
                            )
                        GROUP BY acc_code, vouch_no, vouch_date
                        HAVING CASE WHEN SUM(dr_amount) - SUM(cr_amount) > 0 
                            THEN SUM(dr_amount) - SUM(cr_amount) 
                            ELSE 0 END > 0
                        ORDER BY acc_code, vouch_date";

                    var invoiceResults = await _dataAccessService.ExecuteQueryAsync(user, detailedQuery, commandTimeout: 120);
                    var openingBalanceResults = await _dataAccessService.ExecuteQueryAsync(user, openingBalanceQuery, commandTimeout: 120);

                    // Combine and process results
                    var allResults = new List<Dictionary<string, object>>();
                    allResults.AddRange(invoiceResults);
                    allResults.AddRange(openingBalanceResults);

                    // Group by customer and build detailed items
                    var customerGroups = allResults
                        .GroupBy(row => 
                            row.ContainsKey("AccCode") ? row["AccCode"]?.ToString() ?? "" : ""
                        )
                        .Where(g => !string.IsNullOrEmpty(g.Key))
                        .OrderBy(g => g.Key)
                        .ToList();

                    var detailedItems = new List<CustomerAgingDetailedItem>();
                    decimal totalPending = 0;

                    foreach (var customerGroup in customerGroups)
                    {
                        var firstRow = customerGroup.First();
                        var accCode = firstRow.ContainsKey("AccCode") ? firstRow["AccCode"]?.ToString() ?? "" : "";
                        var accName = firstRow.ContainsKey("AccName") ? firstRow["AccName"]?.ToString() ?? "" : "";
                        
                        decimal customerBalance = 0;

                        // Add customer header
                        detailedItems.Add(new CustomerAgingDetailedItem
                        {
                            AccCode = accCode,
                            AccName = accName,
                            IsCustomerHeader = true,
                            CustomerBalance = null
                        });

                        // Add bill items
                        foreach (var row in customerGroup.OrderBy(r => 
                            r.ContainsKey("BillDate") && DateTime.TryParse(r["BillDate"]?.ToString(), out var date) ? date : DateTime.MinValue))
                        {
                            var billNo = row.ContainsKey("BillNo") ? row["BillNo"]?.ToString() ?? "" : "";
                            var billDate = row.ContainsKey("BillDate") && DateTime.TryParse(row["BillDate"]?.ToString(), out var date) ? date : DateTime.MinValue;
                            var dueDate = row.ContainsKey("DueDate") && row["DueDate"] != null && DateTime.TryParse(row["DueDate"]?.ToString(), out var dDate) ? (DateTime?)dDate : null;
                            var days = row.ContainsKey("Days") && int.TryParse(row["Days"]?.ToString(), out var d) ? d : 0;
                            var billAmount = row.ContainsKey("BillAmount") && decimal.TryParse(row["BillAmount"]?.ToString(), out var ba) ? ba : 0;
                            var pending = row.ContainsKey("Pending") && decimal.TryParse(row["Pending"]?.ToString(), out var p) ? p : 0;

                            if (pending > 0)
                            {
                                detailedItems.Add(new CustomerAgingDetailedItem
                                {
                                    AccCode = accCode,
                                    AccName = accName,
                                    BillNo = billNo,
                                    BillDate = billDate,
                                    DueDate = dueDate,
                                    Days = days,
                                    BillAmount = billAmount,
                                    Pending = pending,
                                    IsCustomerHeader = false,
                                    IsTotalRow = false,
                                    CustomerBalance = null
                                });

                                customerBalance += pending;
                                totalPending += pending;
                            }
                        }

                        // Update customer header with balance
                        var customerHeaderIndex = detailedItems.FindLastIndex(item => item.AccCode == accCode && item.IsCustomerHeader);
                        if (customerHeaderIndex >= 0)
                        {
                            detailedItems[customerHeaderIndex].CustomerBalance = customerBalance;
                        }

                        // Add customer total row
                        if (customerBalance > 0)
                        {
                            detailedItems.Add(new CustomerAgingDetailedItem
                            {
                                AccCode = accCode,
                                AccName = accName,
                                IsTotalRow = true,
                                CustomerBalance = customerBalance
                            });
                        }
                    }

                    stopwatch.Stop();

                    return Ok(new CustomerAgingResponse
                    {
                        Success = true,
                        Message = "Customer Aging Detailed Report retrieved successfully",
                        ReportType = "Detailed",
                        DetailedData = detailedItems,
                        AsOnDate = asOn,
                        FromAccount = fromAcc,
                        UptoAccount = uptoAcc,
                        TotalPending = totalPending
                    });
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error getting customer aging report");
                return StatusCode(500, new CustomerAgingResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        // Supplier Aging Report
        [HttpGet("supplier-aging")]
        public async Task<ActionResult<SupplierAgingResponse>> GetSupplierAging(
            [FromQuery] string? fromAccount = null,
            [FromQuery] string? uptoAccount = null,
            [FromQuery] DateTime? asOnDate = null,
            [FromQuery] string? reportType = null,
            [FromQuery] decimal? minBalance = null)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest(new SupplierAgingResponse
                    {
                        Success = false,
                        Message = "No active database connection found. Please set up your database connection first."
                    });
                }

                // Set defaults
                var fromAcc = fromAccount ?? "0-00-00-0000";
                var uptoAcc = uptoAccount ?? "0-00-00-0000";
                var asOn = asOnDate ?? DateTime.Now;
                var reportTypeValue = reportType?.ToLower() ?? "detailed";
                var minBal = minBalance ?? 0;

                var asOnDateString = asOn.ToString("yyyy/MM/dd");

                // Convert account codes to numeric for range filtering
                var fromAccountNumeric = long.Parse(fromAcc.Replace("-", ""));
                var uptoAccountNumeric = long.Parse(uptoAcc.Replace("-", ""));

                if (reportTypeValue == "summary")
                {
                    // Query BR and JV vouchers (purchase bills) from view_gjournal and match payments
                    // Union with pur_inv entries that might not have vouchers
                    var summaryQuery = $@"
                        declare @asOnDate as date
                        set @asOnDate = '{asOnDateString}'

                        -- Get purchase bills from BR vouchers
                        SELECT 
                            br.acc_code as AccCode,
                            ISNULL(c.acc_name, '') as AccName,
                            br.vouch_no as BillNo,
                            br.vouch_date as BillDate,
                            SUM(br.cr_amount) as BillAmount,
                            ISNULL(SUM(payments.dr_amount), 0) as PaidAmount,
                            SUM(br.cr_amount) - ISNULL(SUM(payments.dr_amount), 0) as Pending,
                            DATEDIFF(day, br.vouch_date, @asOnDate) as Days
                        FROM view_gjournal br
                        LEFT JOIN customer c ON c.acc_code = br.acc_code
                        LEFT JOIN (
                            SELECT 
                                acc_code,
                                vouch_no,
                                SUM(dr_amount) as dr_amount
                            FROM view_gjournal
                            WHERE vouch_date <= @asOnDate
                                AND (LEFT(vouch_no, 2) = 'BP' OR LEFT(vouch_no, 2) = 'JV')
                            GROUP BY acc_code, vouch_no
                        ) payments ON payments.acc_code = br.acc_code
                            AND (
                                (LEFT(payments.vouch_no, 2) = 'BP' AND LTRIM(RIGHT(payments.vouch_no, LEN(payments.vouch_no) - 2), '0') = LTRIM(RIGHT(br.vouch_no, LEN(br.vouch_no) - 2), '0'))
                                OR (LEFT(payments.vouch_no, 2) = 'JV' AND LTRIM(RIGHT(payments.vouch_no, LEN(payments.vouch_no) - 2), '0') = LTRIM(RIGHT(br.vouch_no, LEN(br.vouch_no) - 2), '0'))
                            )
                        WHERE LEFT(br.vouch_no, 2) = 'BR'
                            AND br.vouch_date <= @asOnDate
                            AND CAST(REPLACE(br.acc_code, '-', '') AS BIGINT) >= {fromAccountNumeric}
                            AND CAST(REPLACE(br.acc_code, '-', '') AS BIGINT) <= {uptoAccountNumeric}
                            AND br.cr_amount > 0
                        GROUP BY br.acc_code, c.acc_name, br.vouch_no, br.vouch_date
                        HAVING SUM(br.cr_amount) - ISNULL(SUM(payments.dr_amount), 0) > {minBal}

                        UNION ALL

                        -- Get purchase bills from JV vouchers (when cr_amount > 0)
                        SELECT 
                            jv.acc_code as AccCode,
                            ISNULL(c.acc_name, '') as AccName,
                            jv.vouch_no as BillNo,
                            jv.vouch_date as BillDate,
                            SUM(jv.cr_amount) as BillAmount,
                            ISNULL(SUM(payments.dr_amount), 0) as PaidAmount,
                            SUM(jv.cr_amount) - ISNULL(SUM(payments.dr_amount), 0) as Pending,
                            DATEDIFF(day, jv.vouch_date, @asOnDate) as Days
                        FROM view_gjournal jv
                        LEFT JOIN customer c ON c.acc_code = jv.acc_code
                        LEFT JOIN (
                            SELECT 
                                acc_code,
                                vouch_no,
                                SUM(dr_amount) as dr_amount
                            FROM view_gjournal
                            WHERE vouch_date <= @asOnDate
                                AND (LEFT(vouch_no, 2) = 'BP' OR LEFT(vouch_no, 2) = 'JV')
                            GROUP BY acc_code, vouch_no
                        ) payments ON payments.acc_code = jv.acc_code
                            AND (
                                (LEFT(payments.vouch_no, 2) = 'BP' AND LTRIM(RIGHT(payments.vouch_no, LEN(payments.vouch_no) - 2), '0') = LTRIM(RIGHT(jv.vouch_no, LEN(jv.vouch_no) - 2), '0'))
                                OR (LEFT(payments.vouch_no, 2) = 'JV' AND LTRIM(RIGHT(payments.vouch_no, LEN(payments.vouch_no) - 2), '0') = LTRIM(RIGHT(jv.vouch_no, LEN(jv.vouch_no) - 2), '0'))
                            )
                        WHERE LEFT(jv.vouch_no, 2) = 'JV'
                            AND jv.vouch_date <= @asOnDate
                            AND CAST(REPLACE(jv.acc_code, '-', '') AS BIGINT) >= {fromAccountNumeric}
                            AND CAST(REPLACE(jv.acc_code, '-', '') AS BIGINT) <= {uptoAccountNumeric}
                            AND jv.cr_amount > 0
                            AND jv.vouch_no NOT IN (
                                SELECT DISTINCT vouch_no FROM view_gjournal 
                                WHERE LEFT(vouch_no, 2) = 'BR'
                            )
                        GROUP BY jv.acc_code, c.acc_name, jv.vouch_no, jv.vouch_date
                        HAVING SUM(jv.cr_amount) - ISNULL(SUM(payments.dr_amount), 0) > {minBal}

                        ORDER BY AccCode, BillDate";

                    var results = await _dataAccessService.ExecuteQueryAsync(user, summaryQuery, commandTimeout: 180);

                    // Group by supplier and calculate aging buckets
                    var supplierGroups = results
                        .GroupBy(row => 
                            row.ContainsKey("AccCode") ? row["AccCode"]?.ToString() ?? "" : ""
                        )
                        .Where(g => !string.IsNullOrEmpty(g.Key))
                        .ToList();

                    var summaryItems = new List<SupplierAgingSummaryItem>();
                    decimal totalBalance = 0;
                    decimal total1To30 = 0;
                    decimal total31To60 = 0;
                    decimal total61To90 = 0;
                    decimal total91To120 = 0;
                    decimal totalAbove120 = 0;

                    foreach (var supplierGroup in supplierGroups)
                    {
                        var firstRow = supplierGroup.First();
                        var accCode = firstRow.ContainsKey("AccCode") ? firstRow["AccCode"]?.ToString() ?? "" : "";
                        var accName = firstRow.ContainsKey("AccName") ? firstRow["AccName"]?.ToString() ?? "" : "";
                        
                        decimal balance = 0;
                        decimal days1To30 = 0;
                        decimal days31To60 = 0;
                        decimal days61To90 = 0;
                        decimal days91To120 = 0;
                        decimal above120 = 0;

                        foreach (var row in supplierGroup)
                        {
                            var pending = row.ContainsKey("Pending") && decimal.TryParse(row["Pending"]?.ToString(), out var p) ? p : 0;
                            var days = row.ContainsKey("Days") && int.TryParse(row["Days"]?.ToString(), out var d) ? d : 0;

                            balance += pending;

                            if (days <= 30)
                                days1To30 += pending;
                            else if (days <= 60)
                                days31To60 += pending;
                            else if (days <= 90)
                                days61To90 += pending;
                            else if (days <= 120)
                                days91To120 += pending;
                            else
                                above120 += pending;
                        }

                        if (balance > minBal)
                        {
                            summaryItems.Add(new SupplierAgingSummaryItem
                            {
                                AccCode = accCode,
                                AccName = accName,
                                Balance = balance,
                                Days1To30 = days1To30,
                                Days31To60 = days31To60,
                                Days61To90 = days61To90,
                                Days91To120 = days91To120,
                                Above120 = above120
                            });

                            totalBalance += balance;
                            total1To30 += days1To30;
                            total31To60 += days31To60;
                            total61To90 += days61To90;
                            total91To120 += days91To120;
                            totalAbove120 += above120;
                        }
                    }

                    stopwatch.Stop();

                    return Ok(new SupplierAgingResponse
                    {
                        Success = true,
                        Message = "Supplier Aging Summary Report retrieved successfully",
                        ReportType = "Summary",
                        SummaryData = summaryItems.OrderBy(x => x.AccCode).ToList(),
                        AsOnDate = asOn,
                        FromAccount = fromAcc,
                        UptoAccount = uptoAcc,
                        MinBalance = minBal,
                        TotalBalance = totalBalance,
                        TotalDays1To30 = total1To30,
                        TotalDays31To60 = total31To60,
                        TotalDays61To90 = total61To90,
                        TotalDays91To120 = total91To120,
                        TotalAbove120 = totalAbove120
                    });
                }
                else
                {
                    // Query BR and JV vouchers (purchase bills) from view_gjournal and match payments
                    var detailedQuery = $@"
                        declare @asOnDate as date
                        set @asOnDate = '{asOnDateString}'

                        -- Get purchase bills from BR vouchers
                        SELECT 
                            br.acc_code as AccCode,
                            ISNULL(c.acc_name, '') as AccName,
                            br.vouch_no as BillNo,
                            br.vouch_date as BillDate,
                            NULL as DueDate,
                            SUM(br.cr_amount) as BillAmount,
                            ISNULL(SUM(payments.dr_amount), 0) as PaidAmount,
                            SUM(br.cr_amount) - ISNULL(SUM(payments.dr_amount), 0) as Pending,
                            DATEDIFF(day, br.vouch_date, @asOnDate) as Days
                        FROM view_gjournal br
                        LEFT JOIN customer c ON c.acc_code = br.acc_code
                        LEFT JOIN (
                            SELECT 
                                acc_code,
                                vouch_no,
                                SUM(dr_amount) as dr_amount
                            FROM view_gjournal
                            WHERE vouch_date <= @asOnDate
                                AND (LEFT(vouch_no, 2) = 'BP' OR LEFT(vouch_no, 2) = 'JV')
                            GROUP BY acc_code, vouch_no
                        ) payments ON payments.acc_code = br.acc_code
                            AND (
                                (LEFT(payments.vouch_no, 2) = 'BP' AND LTRIM(RIGHT(payments.vouch_no, LEN(payments.vouch_no) - 2), '0') = LTRIM(RIGHT(br.vouch_no, LEN(br.vouch_no) - 2), '0'))
                                OR (LEFT(payments.vouch_no, 2) = 'JV' AND LTRIM(RIGHT(payments.vouch_no, LEN(payments.vouch_no) - 2), '0') = LTRIM(RIGHT(br.vouch_no, LEN(br.vouch_no) - 2), '0'))
                            )
                        WHERE LEFT(br.vouch_no, 2) = 'BR'
                            AND br.vouch_date <= @asOnDate
                            AND CAST(REPLACE(br.acc_code, '-', '') AS BIGINT) >= {fromAccountNumeric}
                            AND CAST(REPLACE(br.acc_code, '-', '') AS BIGINT) <= {uptoAccountNumeric}
                            AND br.cr_amount > 0
                        GROUP BY br.acc_code, c.acc_name, br.vouch_no, br.vouch_date
                        HAVING SUM(br.cr_amount) - ISNULL(SUM(payments.dr_amount), 0) > {minBal}

                        UNION ALL

                        -- Get purchase bills from JV vouchers (when cr_amount > 0)
                        SELECT 
                            jv.acc_code as AccCode,
                            ISNULL(c.acc_name, '') as AccName,
                            jv.vouch_no as BillNo,
                            jv.vouch_date as BillDate,
                            NULL as DueDate,
                            SUM(jv.cr_amount) as BillAmount,
                            ISNULL(SUM(payments.dr_amount), 0) as PaidAmount,
                            SUM(jv.cr_amount) - ISNULL(SUM(payments.dr_amount), 0) as Pending,
                            DATEDIFF(day, jv.vouch_date, @asOnDate) as Days
                        FROM view_gjournal jv
                        LEFT JOIN customer c ON c.acc_code = jv.acc_code
                        LEFT JOIN (
                            SELECT 
                                acc_code,
                                vouch_no,
                                SUM(dr_amount) as dr_amount
                            FROM view_gjournal
                            WHERE vouch_date <= @asOnDate
                                AND (LEFT(vouch_no, 2) = 'BP' OR LEFT(vouch_no, 2) = 'JV')
                            GROUP BY acc_code, vouch_no
                        ) payments ON payments.acc_code = jv.acc_code
                            AND (
                                (LEFT(payments.vouch_no, 2) = 'BP' AND LTRIM(RIGHT(payments.vouch_no, LEN(payments.vouch_no) - 2), '0') = LTRIM(RIGHT(jv.vouch_no, LEN(jv.vouch_no) - 2), '0'))
                                OR (LEFT(payments.vouch_no, 2) = 'JV' AND LTRIM(RIGHT(payments.vouch_no, LEN(payments.vouch_no) - 2), '0') = LTRIM(RIGHT(jv.vouch_no, LEN(jv.vouch_no) - 2), '0'))
                            )
                        WHERE LEFT(jv.vouch_no, 2) = 'JV'
                            AND jv.vouch_date <= @asOnDate
                            AND CAST(REPLACE(jv.acc_code, '-', '') AS BIGINT) >= {fromAccountNumeric}
                            AND CAST(REPLACE(jv.acc_code, '-', '') AS BIGINT) <= {uptoAccountNumeric}
                            AND jv.cr_amount > 0
                            AND jv.vouch_no NOT IN (
                                SELECT DISTINCT vouch_no FROM view_gjournal 
                                WHERE LEFT(vouch_no, 2) = 'BR'
                            )
                        GROUP BY jv.acc_code, c.acc_name, jv.vouch_no, jv.vouch_date
                        HAVING SUM(jv.cr_amount) - ISNULL(SUM(payments.dr_amount), 0) > {minBal}

                        ORDER BY AccCode, BillDate";

                    // Also need to handle opening balances from view_gjournal directly
                    var openingBalanceQuery = $@"
                        declare @asOnDate as date
                        set @asOnDate = '{asOnDateString}'

                        SELECT 
                            acc_code as AccCode,
                            ISNULL((
                                SELECT acc_name FROM customer WHERE acc_code = vg.acc_code
                            ), '') as AccName,
                            vouch_no as BillNo,
                            vouch_date as BillDate,
                            NULL as DueDate,
                            CASE WHEN SUM(cr_amount) - SUM(dr_amount) > 0 
                                THEN SUM(cr_amount) - SUM(dr_amount) 
                                ELSE 0 END as BillAmount,
                            0 as PaidAmount,
                            CASE WHEN SUM(cr_amount) - SUM(dr_amount) > 0 
                                THEN SUM(cr_amount) - SUM(dr_amount) 
                                ELSE 0 END as Pending,
                            DATEDIFF(day, vouch_date, @asOnDate) as Days
                        FROM view_gjournal vg
                        WHERE LEFT(vouch_no, 2) = 'OB'
                            AND vouch_date <= @asOnDate
                            AND CAST(REPLACE(acc_code, '-', '') AS BIGINT) >= {fromAccountNumeric}
                            AND CAST(REPLACE(acc_code, '-', '') AS BIGINT) <= {uptoAccountNumeric}
                            AND acc_code IN (
                                SELECT DISTINCT acc_code FROM view_gjournal 
                                WHERE (LEFT(vouch_no, 2) = 'BR' OR (LEFT(vouch_no, 2) = 'JV' AND cr_amount > 0))
                                  AND CAST(REPLACE(acc_code, '-', '') AS BIGINT) >= {fromAccountNumeric}
                                  AND CAST(REPLACE(acc_code, '-', '') AS BIGINT) <= {uptoAccountNumeric}
                            )
                        GROUP BY acc_code, vouch_no, vouch_date
                        HAVING CASE WHEN SUM(cr_amount) - SUM(dr_amount) > 0 
                            THEN SUM(cr_amount) - SUM(dr_amount) 
                            ELSE 0 END > {minBal}
                        ORDER BY acc_code, vouch_date";

                    var invoiceResults = await _dataAccessService.ExecuteQueryAsync(user, detailedQuery, commandTimeout: 180);
                    var openingBalanceResults = await _dataAccessService.ExecuteQueryAsync(user, openingBalanceQuery, commandTimeout: 180);

                    // Combine and process results
                    var allResults = new List<Dictionary<string, object>>();
                    allResults.AddRange(invoiceResults);
                    allResults.AddRange(openingBalanceResults);

                    // Group by supplier and build detailed items
                    var supplierGroups = allResults
                        .GroupBy(row => 
                            row.ContainsKey("AccCode") ? row["AccCode"]?.ToString() ?? "" : ""
                        )
                        .Where(g => !string.IsNullOrEmpty(g.Key))
                        .OrderBy(g => g.Key)
                        .ToList();

                    var detailedItems = new List<SupplierAgingDetailedItem>();
                    decimal totalPending = 0;

                    foreach (var supplierGroup in supplierGroups)
                    {
                        var firstRow = supplierGroup.First();
                        var accCode = firstRow.ContainsKey("AccCode") ? firstRow["AccCode"]?.ToString() ?? "" : "";
                        var accName = firstRow.ContainsKey("AccName") ? firstRow["AccName"]?.ToString() ?? "" : "";
                        
                        decimal supplierBalance = 0;

                        // Add supplier header
                        detailedItems.Add(new SupplierAgingDetailedItem
                        {
                            AccCode = accCode,
                            AccName = accName,
                            IsSupplierHeader = true,
                            SupplierBalance = null
                        });

                        // Add bill items
                        foreach (var row in supplierGroup.OrderBy(r => 
                            r.ContainsKey("BillDate") && DateTime.TryParse(r["BillDate"]?.ToString(), out var date) ? date : DateTime.MinValue))
                        {
                            var billNo = row.ContainsKey("BillNo") ? row["BillNo"]?.ToString() ?? "" : "";
                            var billDate = row.ContainsKey("BillDate") && DateTime.TryParse(row["BillDate"]?.ToString(), out var date) ? date : DateTime.MinValue;
                            var dueDate = row.ContainsKey("DueDate") && row["DueDate"] != null && DateTime.TryParse(row["DueDate"]?.ToString(), out var dDate) ? (DateTime?)dDate : null;
                            var days = row.ContainsKey("Days") && int.TryParse(row["Days"]?.ToString(), out var d) ? d : 0;
                            var billAmount = row.ContainsKey("BillAmount") && decimal.TryParse(row["BillAmount"]?.ToString(), out var ba) ? ba : 0;
                            var pending = row.ContainsKey("Pending") && decimal.TryParse(row["Pending"]?.ToString(), out var p) ? p : 0;

                            if (pending > minBal)
                            {
                                detailedItems.Add(new SupplierAgingDetailedItem
                                {
                                    AccCode = accCode,
                                    AccName = accName,
                                    BillNo = billNo,
                                    BillDate = billDate,
                                    DueDate = dueDate,
                                    Days = days,
                                    BillAmount = billAmount,
                                    Pending = pending,
                                    IsSupplierHeader = false,
                                    IsTotalRow = false,
                                    SupplierBalance = null
                                });

                                supplierBalance += pending;
                                totalPending += pending;
                            }
                        }

                        // Update supplier header with balance
                        var supplierHeaderIndex = detailedItems.FindLastIndex(item => item.AccCode == accCode && item.IsSupplierHeader);
                        if (supplierHeaderIndex >= 0)
                        {
                            detailedItems[supplierHeaderIndex].SupplierBalance = supplierBalance;
                        }

                        // Add supplier total row
                        if (supplierBalance > minBal)
                        {
                            detailedItems.Add(new SupplierAgingDetailedItem
                            {
                                AccCode = accCode,
                                AccName = accName,
                                IsTotalRow = true,
                                SupplierBalance = supplierBalance
                            });
                        }
                    }

                    stopwatch.Stop();

                    return Ok(new SupplierAgingResponse
                    {
                        Success = true,
                        Message = "Supplier Aging Detailed Report retrieved successfully",
                        ReportType = "Detailed",
                        DetailedData = detailedItems,
                        AsOnDate = asOn,
                        FromAccount = fromAcc,
                        UptoAccount = uptoAcc,
                        MinBalance = minBal,
                        TotalPending = totalPending
                    });
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error getting supplier aging report");
                return StatusCode(500, new SupplierAgingResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        // Item Purchase Summary Report
        [HttpGet("item-purchase-summary")]
        public async Task<ActionResult<ItemPurchaseSummaryResponse>> GetItemPurchaseSummary(
            [FromQuery] DateTime fromDate, 
            [FromQuery] DateTime toDate,
            [FromQuery] string? itemGroup = null)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest(new ItemPurchaseSummaryResponse
                    {
                        Success = false,
                        Message = "No active database connection found. Please set up your database connection first."
                    });
                }

                var fromDateString = fromDate.ToString("yyyy/MM/dd");
                var toDateString = toDate.ToString("yyyy/MM/dd");

                // Build item group filter
                string itemGroupFilter = "";
                string itemGroupName = "";
                if (!string.IsNullOrEmpty(itemGroup) && itemGroup.Trim().ToLower() != "all")
                {
                    itemGroupFilter = $"AND i.item_group = '{itemGroup.Trim()}'";
                    
                    // Get item group name
                    var groupNameQuery = $"SELECT RTRIM(group_name) as GroupName FROM item_group WHERE group_code = '{itemGroup.Trim()}'";
                    var groupNameResult = await _dataAccessService.ExecuteQueryAsync(user, groupNameQuery);
                    if (groupNameResult.Any())
                    {
                        itemGroupName = groupNameResult.First().ContainsKey("GroupName") ? 
                            groupNameResult.First()["GroupName"]?.ToString() ?? "" : "";
                    }
                }

                // Item Purchase Summary SQL query - aggregate by item with average rate calculations
                var itemPurchaseSummaryQuery = $@"
                    SELECT 
                        sp.item_code as ItemCode,
                        RTRIM(sp.item_name) as ItemName,
                        RTRIM(sp.variety) as Variety,
                        sp.packing as Packing,
                        RTRIM(sp.status) as Status,
                        SUM(sp.qty_jute + sp.qty_pp_100 + sp.qty_pp_50) as TotalQty,
                        SUM(sp.total_wght) as TotalWeight,
                        SUM(sp.grand_tot) as TotalAmount,
                        -- Average rate per Unit
                        CASE 
                            WHEN SUM(sp.qty_jute + sp.qty_pp_100 + sp.qty_pp_50) > 0 
                            THEN SUM(sp.grand_tot) / SUM(sp.qty_jute + sp.qty_pp_100 + sp.qty_pp_50)
                            ELSE 0 
                        END as AvgRatePerUnit,
                        -- Average rate per Kg
                        CASE 
                            WHEN SUM(sp.total_wght) > 0 
                            THEN SUM(sp.grand_tot) / SUM(sp.total_wght)
                            ELSE 0 
                        END as AvgRatePerKg,
                        -- Average rate per Mound (40 Kg)
                        CASE 
                            WHEN SUM(sp.total_wght) > 0 
                            THEN SUM(sp.grand_tot) / (SUM(sp.total_wght) / 40)
                            ELSE 0 
                        END as AvgRatePerMound
                    FROM pur_inv pi
                    INNER JOIN sub_pinv sp ON pi.inv_no = sp.inv_no AND pi.inv_type = sp.inv_type
                    LEFT JOIN item i ON sp.item_code = i.item_code
                    WHERE pi.inv_date >= '{fromDateString}' 
                      AND pi.inv_date <= '{toDateString}'
                      {itemGroupFilter}
                    GROUP BY sp.item_code, sp.item_name, sp.variety, sp.packing, sp.status
                    ORDER BY sp.item_name, sp.variety";

                var results = await _dataAccessService.ExecuteQueryAsync(user, itemPurchaseSummaryQuery, commandTimeout: 120);
                
                var itemPurchaseSummaryItems = new List<ItemPurchaseSummaryItem>();
                decimal grandTotalQty = 0;
                decimal grandTotalWeight = 0;
                decimal grandTotalAmount = 0;

                // Process each row
                foreach (var row in results)
                {
                    var itemCode = row.ContainsKey("ItemCode") ? row["ItemCode"]?.ToString() ?? "" : "";
                    var itemName = row.ContainsKey("ItemName") ? row["ItemName"]?.ToString() ?? "" : "";
                    var variety = row.ContainsKey("Variety") ? row["Variety"]?.ToString() ?? "" : "";
                    var packing = row.ContainsKey("Packing") && decimal.TryParse(row["Packing"]?.ToString(), out var p) ? p : 0;
                    var status = row.ContainsKey("Status") ? row["Status"]?.ToString() ?? "" : "";
                    var totalQty = row.ContainsKey("TotalQty") && decimal.TryParse(row["TotalQty"]?.ToString(), out var tq) ? tq : 0;
                    var totalWeight = row.ContainsKey("TotalWeight") && decimal.TryParse(row["TotalWeight"]?.ToString(), out var tw) ? tw : 0;
                    var totalAmount = row.ContainsKey("TotalAmount") && decimal.TryParse(row["TotalAmount"]?.ToString(), out var ta) ? ta : 0;
                    var avgRatePerUnit = row.ContainsKey("AvgRatePerUnit") && decimal.TryParse(row["AvgRatePerUnit"]?.ToString(), out var aru) ? aru : 0;
                    var avgRatePerKg = row.ContainsKey("AvgRatePerKg") && decimal.TryParse(row["AvgRatePerKg"]?.ToString(), out var ark) ? ark : 0;
                    var avgRatePerMound = row.ContainsKey("AvgRatePerMound") && decimal.TryParse(row["AvgRatePerMound"]?.ToString(), out var arm) ? arm : 0;

                    itemPurchaseSummaryItems.Add(new ItemPurchaseSummaryItem
                    {
                        ItemCode = itemCode,
                        ItemName = itemName,
                        Variety = variety,
                        Packing = packing,
                        Status = status,
                        TotalQty = totalQty,
                        TotalWeight = totalWeight,
                        TotalAmount = totalAmount,
                        AvgRatePerUnit = avgRatePerUnit,
                        AvgRatePerKg = avgRatePerKg,
                        AvgRatePerMound = avgRatePerMound
                    });

                    grandTotalQty += totalQty;
                    grandTotalWeight += totalWeight;
                    grandTotalAmount += totalAmount;
                }

                stopwatch.Stop();

                return Ok(new ItemPurchaseSummaryResponse
                {
                    Success = true,
                    Message = "Item Purchase Summary Report retrieved successfully",
                    Data = itemPurchaseSummaryItems,
                    FromDate = fromDate,
                    ToDate = toDate,
                    ItemGroup = itemGroup,
                    ItemGroupName = itemGroupName,
                    GrandTotalQty = grandTotalQty,
                    GrandTotalWeight = grandTotalWeight,
                    GrandTotalAmount = grandTotalAmount
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error getting item purchase summary");
                return StatusCode(500, new ItemPurchaseSummaryResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        // Item Sales Summary Report
        [HttpGet("item-sales-summary")]
        public async Task<ActionResult<ItemSalesSummaryResponse>> GetItemSalesSummary(
            [FromQuery] DateTime fromDate, 
            [FromQuery] DateTime toDate,
            [FromQuery] string? invoiceType = null,
            [FromQuery] string? itemGroup = null)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetActiveUserAsync(userId);
                
                if (user == null)
                {
                    return BadRequest(new ItemSalesSummaryResponse
                    {
                        Success = false,
                        Message = "No active database connection found. Please set up your database connection first."
                    });
                }

                var fromDateString = fromDate.ToString("yyyy/MM/dd");
                var toDateString = toDate.ToString("yyyy/MM/dd");

                // Build invoice type filter
                string invoiceTypeFilter = "";
                if (!string.IsNullOrEmpty(invoiceType) && invoiceType.Trim().ToLower() != "all")
                {
                    var invType = invoiceType.Trim();
                    // Map invoice types: Credit, Cash, Others
                    invoiceTypeFilter = $"AND si.inv_type = '{invType}'";
                }

                // Build item group filter
                string itemGroupFilter = "";
                string itemGroupName = "";
                if (!string.IsNullOrEmpty(itemGroup) && itemGroup.Trim().ToLower() != "all")
                {
                    itemGroupFilter = $"AND i.item_group = '{itemGroup.Trim()}'";
                    
                    // Get item group name
                    var groupNameQuery = $"SELECT RTRIM(group_name) as GroupName FROM item_group WHERE group_code = '{itemGroup.Trim()}'";
                    var groupNameResult = await _dataAccessService.ExecuteQueryAsync(user, groupNameQuery);
                    if (groupNameResult.Any())
                    {
                        itemGroupName = groupNameResult.First().ContainsKey("GroupName") ? 
                            groupNameResult.First()["GroupName"]?.ToString() ?? "" : "";
                    }
                }

                // Item Sales Summary SQL query - aggregate by item with average rate calculations
                var itemSalesSummaryQuery = $@"
                    SELECT 
                        ss.item_code as ItemCode,
                        RTRIM(ss.item_name) as ItemName,
                        RTRIM(ss.variety) as Variety,
                        ss.packing as Packing,
                        RTRIM(ss.status) as Status,
                        SUM(ss.qty) as TotalQty,
                        SUM(ss.total_wght) as TotalWeight,
                        SUM(ss.grand_tot) as TotalAmount,
                        -- Average rate per Unit
                        CASE 
                            WHEN SUM(ss.qty) > 0 
                            THEN SUM(ss.grand_tot) / SUM(ss.qty)
                            ELSE 0 
                        END as AvgRatePerUnit,
                        -- Average rate per Kg
                        CASE 
                            WHEN SUM(ss.total_wght) > 0 
                            THEN SUM(ss.grand_tot) / SUM(ss.total_wght)
                            ELSE 0 
                        END as AvgRatePerKg,
                        -- Average rate per Mound (40 Kg)
                        CASE 
                            WHEN SUM(ss.total_wght) > 0 
                            THEN SUM(ss.grand_tot) / (SUM(ss.total_wght) / 40)
                            ELSE 0 
                        END as AvgRatePerMound
                    FROM sale_inv si
                    INNER JOIN sub_sinv ss ON si.inv_no = ss.inv_no AND si.inv_type = ss.inv_type
                    LEFT JOIN item i ON ss.item_code = i.item_code
                    WHERE si.inv_date >= '{fromDateString}' 
                      AND si.inv_date <= '{toDateString}'
                      {invoiceTypeFilter}
                      {itemGroupFilter}
                    GROUP BY ss.item_code, ss.item_name, ss.variety, ss.packing, ss.status
                    ORDER BY ss.item_name, ss.variety";

                var results = await _dataAccessService.ExecuteQueryAsync(user, itemSalesSummaryQuery, commandTimeout: 120);
                
                var itemSalesSummaryItems = new List<ItemSalesSummaryItem>();
                decimal grandTotalQty = 0;
                decimal grandTotalWeight = 0;
                decimal grandTotalAmount = 0;

                // Process each row
                foreach (var row in results)
                {
                    var itemCode = row.ContainsKey("ItemCode") ? row["ItemCode"]?.ToString() ?? "" : "";
                    var itemName = row.ContainsKey("ItemName") ? row["ItemName"]?.ToString() ?? "" : "";
                    var variety = row.ContainsKey("Variety") ? row["Variety"]?.ToString() ?? "" : "";
                    var packing = row.ContainsKey("Packing") && decimal.TryParse(row["Packing"]?.ToString(), out var p) ? p : 0;
                    var status = row.ContainsKey("Status") ? row["Status"]?.ToString() ?? "" : "";
                    var totalQty = row.ContainsKey("TotalQty") && decimal.TryParse(row["TotalQty"]?.ToString(), out var tq) ? tq : 0;
                    var totalWeight = row.ContainsKey("TotalWeight") && decimal.TryParse(row["TotalWeight"]?.ToString(), out var tw) ? tw : 0;
                    var totalAmount = row.ContainsKey("TotalAmount") && decimal.TryParse(row["TotalAmount"]?.ToString(), out var ta) ? ta : 0;
                    var avgRatePerUnit = row.ContainsKey("AvgRatePerUnit") && decimal.TryParse(row["AvgRatePerUnit"]?.ToString(), out var aru) ? aru : 0;
                    var avgRatePerKg = row.ContainsKey("AvgRatePerKg") && decimal.TryParse(row["AvgRatePerKg"]?.ToString(), out var ark) ? ark : 0;
                    var avgRatePerMound = row.ContainsKey("AvgRatePerMound") && decimal.TryParse(row["AvgRatePerMound"]?.ToString(), out var arm) ? arm : 0;

                    itemSalesSummaryItems.Add(new ItemSalesSummaryItem
                    {
                        ItemCode = itemCode,
                        ItemName = itemName,
                        Variety = variety,
                        Packing = packing,
                        Status = status,
                        TotalQty = totalQty,
                        TotalWeight = totalWeight,
                        TotalAmount = totalAmount,
                        AvgRatePerUnit = avgRatePerUnit,
                        AvgRatePerKg = avgRatePerKg,
                        AvgRatePerMound = avgRatePerMound
                    });

                    grandTotalQty += totalQty;
                    grandTotalWeight += totalWeight;
                    grandTotalAmount += totalAmount;
                }

                stopwatch.Stop();

                return Ok(new ItemSalesSummaryResponse
                {
                    Success = true,
                    Message = "Item Sales Summary Report retrieved successfully",
                    Data = itemSalesSummaryItems,
                    FromDate = fromDate,
                    ToDate = toDate,
                    InvoiceType = invoiceType,
                    ItemGroup = itemGroup,
                    ItemGroupName = itemGroupName,
                    GrandTotalQty = grandTotalQty,
                    GrandTotalWeight = grandTotalWeight,
                    GrandTotalAmount = grandTotalAmount
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error getting item sales summary");
                return StatusCode(500, new ItemSalesSummaryResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        private int GetCurrentUserId()
        {
            // Try different claim types for user ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? 
                             User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier") ??
                             User.FindFirst("nameidentifier");
            
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                _logger.LogWarning("User ID not found in token. Available claims: {Claims}", 
                    string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}")));
                throw new UnauthorizedAccessException("User ID not found in token");
            }
            
            return userId;
        }

    }
}