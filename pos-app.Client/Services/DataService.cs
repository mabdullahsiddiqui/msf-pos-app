using System.Net.Http.Json;
using pos_app.Client.Models;

namespace pos_app.Client.Services
{
    public class DataService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthService _authService;
        private readonly ILogger<DataService> _logger;

        public DataService(HttpClient httpClient, AuthService authService, ILogger<DataService> logger)
        {
            _httpClient = httpClient;
            _authService = authService;
            _logger = logger;
        }

        private async Task<HttpClient> CreateAuthedClientAsync()
        {
            var token = await _authService.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            return _httpClient;
        }

        // User Connection Management Methods
        public async Task<User?> GetUserProfileAsync()
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var response = await client.GetAsync("api/auth/profile");
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<User>();
                    return result;
                }

                _logger.LogWarning($"Failed to fetch user profile: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user profile");
                return null;
            }
        }

        public async Task<bool> UpdateUserConnectionAsync(User user)
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var response = await client.PutAsJsonAsync("api/auth/update-connection", user);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user connection");
                return false;
            }
        }

        public async Task<bool> TestUserConnectionAsync()
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var response = await client.PostAsync("api/clientdata/test-connection", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing user connection");
                return false;
            }
        }

        // Existing Data Methods
        public async Task<List<Dictionary<string, object>>> GetSalesAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var client = await CreateAuthedClientAsync();

                var queryParams = new List<string>();
                if (startDate.HasValue)
                    queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
                if (endDate.HasValue)
                    queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");

                var url = "api/data/sales";
                if (queryParams.Any())
                    url += "?" + string.Join("&", queryParams);

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
                    return result ?? new List<Dictionary<string, object>>();
                }

                _logger.LogWarning($"Failed to fetch sales data: {response.StatusCode}");
                return new List<Dictionary<string, object>>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching sales data");
                return new List<Dictionary<string, object>>();
            }
        }

        public async Task<List<Dictionary<string, object>>> GetInventoryAsync()
        {
            try
            {
                var client = await CreateAuthedClientAsync();

                var response = await client.GetAsync("api/data/inventory");
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
                    return result ?? new List<Dictionary<string, object>>();
                }

                _logger.LogWarning($"Failed to fetch inventory data: {response.StatusCode}");
                return new List<Dictionary<string, object>>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching inventory data");
                return new List<Dictionary<string, object>>();
            }
        }

        public async Task<object> GetSummaryReportAsync()
        {
            try
            {
                var client = await CreateAuthedClientAsync();

                var response = await client.GetAsync("api/data/reports/summary");
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<object>();
                    return result ?? new { };
                }

                throw new HttpRequestException($"Failed to fetch summary report: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching summary report");
                throw;
            }
        }

        public async Task<List<Dictionary<string, object>>> ExecuteCustomQueryAsync(string sql)
        {
            try
            {
                var client = await CreateAuthedClientAsync();

                var response = await client.GetAsync($"api/data/query?sql={Uri.EscapeDataString(sql)}");
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
                    return result ?? new List<Dictionary<string, object>>();
                }

                throw new HttpRequestException($"Failed to execute custom query: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing custom query");
                throw;
            }
        }

        // FlourDb Client Data Methods
        public async Task<List<ClientCustomer>> GetCustomersAsync()
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var response = await client.GetAsync("api/clientdata/customers/simple");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var customers = System.Text.Json.JsonSerializer.Deserialize<List<ClientCustomer>>(content, new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return customers ?? new List<ClientCustomer>();
                }

                _logger.LogWarning($"Failed to fetch customers: {response.StatusCode}");
                return new List<ClientCustomer>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching customers");
                return new List<ClientCustomer>();
            }
        }

        public async Task<List<ClientItem>> GetClientItemsAsync()
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var response = await client.GetAsync("api/clientdata/items");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var items = System.Text.Json.JsonSerializer.Deserialize<List<ClientItem>>(content, new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return items ?? new List<ClientItem>();
                }

                _logger.LogWarning($"Failed to fetch items: {response.StatusCode}");
                return new List<ClientItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching items");
                return new List<ClientItem>();
            }
        }

        public async Task<List<ClientSaleInvoice>> GetClientRecentSalesAsync(int count = 10)
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var response = await client.GetAsync($"api/clientdata/recent-sales?count={count}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var sales = System.Text.Json.JsonSerializer.Deserialize<List<ClientSaleInvoice>>(content, new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return sales ?? new List<ClientSaleInvoice>();
                }

                _logger.LogWarning($"Failed to fetch recent sales: {response.StatusCode}");
                return new List<ClientSaleInvoice>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recent sales");
                return new List<ClientSaleInvoice>();
            }
        }

        public async Task<bool> TestClientConnectionAsync()
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var response = await client.GetAsync("api/clientdata/debug/connection");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(content);
                    return result.TryGetProperty("connectionSuccessful", out var isConnected) && isConnected.GetBoolean();
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing client connection");
                return false;
            }
        }

        public async Task<ClientDashboardData> GetDashboardDataAsync()
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var response = await client.GetAsync("api/clientdata/dashboard");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(content);
                    
                    return new ClientDashboardData
                    {
                        TotalSales = result.TryGetProperty("totalSales", out var totalSales) ? totalSales.GetDecimal() : 0,
                        TotalPurchases = result.TryGetProperty("totalPurchases", out var totalPurchases) ? totalPurchases.GetDecimal() : 0,
                        TotalCustomers = result.TryGetProperty("customerCount", out var customerCount) ? customerCount.GetInt32() : 0,
                        TotalItems = result.TryGetProperty("totalItems", out var totalItems) ? totalItems.GetInt32() : 0,
                        NetProfit = result.TryGetProperty("netProfit", out var netProfit) ? netProfit.GetDecimal() : 0,
                        RecentSales = new List<ClientSaleInvoice>(),
                        MonthName = result.TryGetProperty("monthName", out var monthName) ? monthName.GetString() ?? "Current Month" : "Current Month"
                    };
                }
                return new ClientDashboardData();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard data");
                return new ClientDashboardData();
            }
        }


        // Reports Methods
        
        /*
         * COMMENTED OUT - Simple Trial Balance (kept for backward compatibility)
         * This calls the old simple trial balance endpoint.
         * Now using GetTrialBalanceCSharpAsync as the primary method.
         * Uncomment if needed for backward compatibility.
         */
        /*
        public async Task<TrialBalanceResponse> GetTrialBalanceAsync(DateTime? asOfDate = null)
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var url = "api/reports/trial-balance";
                if (asOfDate.HasValue)
                {
                    url += $"?asOfDate={asOfDate.Value:yyyy-MM-dd}";
                }

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<TrialBalanceResponse>();
                    return result ?? new TrialBalanceResponse { Success = false, Message = "Invalid response from server" };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return new TrialBalanceResponse { Success = false, Message = $"Failed to fetch trial balance: {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching trial balance");
                return new TrialBalanceResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }
        */


        // Hierarchical Trial Balance - C# Logic Implementation
        public async Task<TrialBalanceResponse> GetTrialBalanceCSharpAsync(DateTime? asOfDate = null)
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var url = "api/reports/trial-balance-csharp";
                if (asOfDate.HasValue)
                {
                    url += $"?asOfDate={asOfDate.Value:yyyy-MM-dd}";
                }

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<TrialBalanceResponse>();
                    return result ?? new TrialBalanceResponse { Success = false, Message = "Invalid response from server" };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return new TrialBalanceResponse { Success = false, Message = $"Failed to fetch hierarchical trial balance (C#): {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching hierarchical trial balance (C#)");
                return new TrialBalanceResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        // Hierarchical Trial Balance - SQL Implementation
        public async Task<TrialBalanceResponse> GetTrialBalanceSqlAsync(DateTime? asOfDate = null)
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var url = "api/reports/trial-balance-sql";
                if (asOfDate.HasValue)
                {
                    url += $"?asOfDate={asOfDate.Value:yyyy-MM-dd}";
                }

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<TrialBalanceResponse>();
                    return result ?? new TrialBalanceResponse { Success = false, Message = "Invalid response from server" };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return new TrialBalanceResponse { Success = false, Message = $"Failed to fetch hierarchical trial balance (SQL): {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching hierarchical trial balance (SQL)");
                return new TrialBalanceResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        // Monthly Account Balance Report
        public async Task<MonthlyAccountBalanceResponse> GetMonthlyAccountBalanceAsync(
            DateTime? fromDate = null, 
            DateTime? toDate = null,
            string? fromAccount = null,
            string? uptoAccount = null)
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var url = "api/reports/monthly-account-balance";
                var queryParams = new List<string>();
                
                if (fromDate.HasValue)
                {
                    queryParams.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
                }
                if (toDate.HasValue)
                {
                    queryParams.Add($"toDate={toDate.Value:yyyy-MM-dd}");
                }
                if (!string.IsNullOrEmpty(fromAccount))
                {
                    queryParams.Add($"fromAccount={Uri.EscapeDataString(fromAccount)}");
                }
                if (!string.IsNullOrEmpty(uptoAccount))
                {
                    queryParams.Add($"uptoAccount={Uri.EscapeDataString(uptoAccount)}");
                }
                
                if (queryParams.Any())
                {
                    url += "?" + string.Join("&", queryParams);
                }

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<MonthlyAccountBalanceResponse>();
                    return result ?? new MonthlyAccountBalanceResponse { Success = false, Message = "Invalid response from server" };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return new MonthlyAccountBalanceResponse { Success = false, Message = $"Failed to fetch monthly account balance: {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching monthly account balance");
                return new MonthlyAccountBalanceResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        // 3 Trial Balance Report
        public async Task<ThreeTrialBalanceResponse> GetThreeTrialBalanceAsync(
            DateTime? fromDate = null, 
            DateTime? toDate = null,
            string? fromAccount = null,
            string? uptoAccount = null)
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var url = "api/reports/three-trial-balance";
                var queryParams = new List<string>();
                
                if (fromDate.HasValue)
                {
                    queryParams.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
                }
                if (toDate.HasValue)
                {
                    queryParams.Add($"toDate={toDate.Value:yyyy-MM-dd}");
                }
                if (!string.IsNullOrEmpty(fromAccount))
                {
                    queryParams.Add($"fromAccount={Uri.EscapeDataString(fromAccount)}");
                }
                if (!string.IsNullOrEmpty(uptoAccount))
                {
                    queryParams.Add($"uptoAccount={Uri.EscapeDataString(uptoAccount)}");
                }
                
                if (queryParams.Any())
                {
                    url += "?" + string.Join("&", queryParams);
                }

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ThreeTrialBalanceResponse>();
                    return result ?? new ThreeTrialBalanceResponse { Success = false, Message = "Invalid response from server" };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return new ThreeTrialBalanceResponse { Success = false, Message = $"Failed to fetch 3 trial balance: {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching 3 trial balance");
                return new ThreeTrialBalanceResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        // Account Ledger Report
        public async Task<LedgerReportResponse> GetAccountLedgerAsync(string accountId, DateTime fromDate, DateTime toDate)
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var url = $"api/reports/ledger?accountId={Uri.EscapeDataString(accountId)}&fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}";

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LedgerReportResponse>();
                    return result ?? new LedgerReportResponse { Success = false, Message = "Invalid response from server" };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return new LedgerReportResponse { Success = false, Message = $"Failed to fetch account ledger: {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching account ledger");
                return new LedgerReportResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        // Account Position Report
        public async Task<AccountPositionResponse> GetAccountPositionAsync(string accountId, DateTime uptoDate)
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var url = $"api/reports/account-position?accountId={Uri.EscapeDataString(accountId)}&uptoDate={uptoDate:yyyy-MM-dd}";

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AccountPositionResponse>();
                    return result ?? new AccountPositionResponse { Success = false, Message = "Invalid response from server" };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return new AccountPositionResponse { Success = false, Message = $"Failed to fetch account position: {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching account position");
                return new AccountPositionResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        // Cash Book Report
        public async Task<CashBookResponse> GetCashBookAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var url = "api/reports/cash-book";
                var queryParams = new List<string>();
                
                if (fromDate.HasValue)
                {
                    queryParams.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
                }
                if (toDate.HasValue)
                {
                    queryParams.Add($"toDate={toDate.Value:yyyy-MM-dd}");
                }
                
                if (queryParams.Any())
                {
                    url += "?" + string.Join("&", queryParams);
                }

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CashBookResponse>();
                    return result ?? new CashBookResponse { Success = false, Message = "Invalid response from server" };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return new CashBookResponse { Success = false, Message = $"Failed to fetch cash book: {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching cash book");
                return new CashBookResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        // Journal Book Report
        public async Task<JournalBookResponse> GetJournalBookAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var url = "api/reports/journal-book";
                var queryParams = new List<string>();
                
                if (fromDate.HasValue)
                {
                    queryParams.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
                }
                if (toDate.HasValue)
                {
                    queryParams.Add($"toDate={toDate.Value:yyyy-MM-dd}");
                }
                
                if (queryParams.Any())
                {
                    url += "?" + string.Join("&", queryParams);
                }

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<JournalBookResponse>();
                    return result ?? new JournalBookResponse { Success = false, Message = "Invalid response from server" };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return new JournalBookResponse { Success = false, Message = $"Failed to fetch journal book: {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching journal book");
                return new JournalBookResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        // Transaction Journal Report
        public async Task<TransactionJournalResponse> GetTransactionJournalAsync(
            DateTime fromDate, 
            DateTime toDate, 
            List<string> documentTypes)
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var url = "api/reports/transaction-journal";
                var queryParams = new List<string>();
                
                queryParams.Add($"fromDate={fromDate:yyyy-MM-dd}");
                queryParams.Add($"toDate={toDate:yyyy-MM-dd}");
                
                if (documentTypes != null && documentTypes.Any())
                {
                    var documentTypesParam = string.Join(",", documentTypes);
                    queryParams.Add($"documentTypes={Uri.EscapeDataString(documentTypesParam)}");
                }
                
                if (queryParams.Any())
                {
                    url += "?" + string.Join("&", queryParams);
                }

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<TransactionJournalResponse>();
                    return result ?? new TransactionJournalResponse { Success = false, Message = "Invalid response from server" };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return new TransactionJournalResponse { Success = false, Message = $"Failed to fetch transaction journal: {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching transaction journal");
                return new TransactionJournalResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        // Purchase Journal Report
        public async Task<PurchaseJournalResponse> GetPurchaseJournalAsync(
            DateTime fromDate, 
            DateTime toDate, 
            string invoiceType = "All")
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var url = "api/reports/purchase-journal";
                var queryParams = new List<string>();
                
                queryParams.Add($"fromDate={fromDate:yyyy-MM-dd}");
                queryParams.Add($"toDate={toDate:yyyy-MM-dd}");
                
                if (!string.IsNullOrEmpty(invoiceType))
                {
                    queryParams.Add($"invoiceType={Uri.EscapeDataString(invoiceType)}");
                }
                
                if (queryParams.Any())
                {
                    url += "?" + string.Join("&", queryParams);
                }

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<PurchaseJournalResponse>();
                    return result ?? new PurchaseJournalResponse { Success = false, Message = "Invalid response from server" };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return new PurchaseJournalResponse { Success = false, Message = $"Failed to fetch purchase journal: {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching purchase journal");
                return new PurchaseJournalResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        // Sales Journal Report
        public async Task<SalesJournalResponse> GetSalesJournalAsync(
            DateTime fromDate, 
            DateTime toDate, 
            string invoiceType = "All")
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var url = "api/reports/sales-journal";
                var queryParams = new List<string>();
                
                queryParams.Add($"fromDate={fromDate:yyyy-MM-dd}");
                queryParams.Add($"toDate={toDate:yyyy-MM-dd}");
                
                if (!string.IsNullOrEmpty(invoiceType))
                {
                    queryParams.Add($"invoiceType={Uri.EscapeDataString(invoiceType)}");
                }
                
                if (queryParams.Any())
                {
                    url += "?" + string.Join("&", queryParams);
                }

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<SalesJournalResponse>();
                    return result ?? new SalesJournalResponse { Success = false, Message = "Invalid response from server" };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return new SalesJournalResponse { Success = false, Message = $"Failed to fetch sales journal: {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching sales journal");
                return new SalesJournalResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        // Purchase Register Report
        public async Task<PurchaseRegisterResponse> GetPurchaseRegisterAsync(
            DateTime fromDate, 
            DateTime toDate, 
            string invoiceType = "All")
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var url = "api/reports/purchase-register";
                var queryParams = new List<string>();
                
                queryParams.Add($"fromDate={fromDate:yyyy-MM-dd}");
                queryParams.Add($"toDate={toDate:yyyy-MM-dd}");
                
                if (!string.IsNullOrEmpty(invoiceType))
                {
                    queryParams.Add($"invoiceType={Uri.EscapeDataString(invoiceType)}");
                }
                
                if (queryParams.Any())
                {
                    url += "?" + string.Join("&", queryParams);
                }

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<PurchaseRegisterResponse>();
                    return result ?? new PurchaseRegisterResponse { Success = false, Message = "Invalid response from server" };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return new PurchaseRegisterResponse { Success = false, Message = $"Failed to fetch purchase register: {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching purchase register");
                return new PurchaseRegisterResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        // Sales Register Report
        public async Task<SalesRegisterResponse> GetSalesRegisterAsync(
            DateTime fromDate, 
            DateTime toDate,
            string invoiceType = "All")
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var url = "api/reports/sales-register";
                var queryParams = new List<string>();
                
                queryParams.Add($"fromDate={fromDate:yyyy-MM-dd}");
                queryParams.Add($"toDate={toDate:yyyy-MM-dd}");
                
                if (!string.IsNullOrEmpty(invoiceType))
                {
                    queryParams.Add($"invoiceType={Uri.EscapeDataString(invoiceType)}");
                }
                
                if (queryParams.Any())
                {
                    url += "?" + string.Join("&", queryParams);
                }

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<SalesRegisterResponse>();
                    return result ?? new SalesRegisterResponse { Success = false, Message = "Invalid response from server" };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return new SalesRegisterResponse { Success = false, Message = $"Failed to fetch sales register: {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching sales register");
                return new SalesRegisterResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        // Broker Sales Report
        public async Task<BrokerSalesReportResponse> GetBrokerSalesReportAsync(
            DateTime fromDate, 
            DateTime toDate,
            string? brokerAccountCode = null)
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var url = "api/reports/broker-sales-report";
                var queryParams = new List<string>();
                
                queryParams.Add($"fromDate={fromDate:yyyy-MM-dd}");
                queryParams.Add($"toDate={toDate:yyyy-MM-dd}");
                
                if (!string.IsNullOrEmpty(brokerAccountCode))
                {
                    queryParams.Add($"brokerAccountCode={Uri.EscapeDataString(brokerAccountCode)}");
                }
                
                if (queryParams.Any())
                {
                    url += "?" + string.Join("&", queryParams);
                }

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<BrokerSalesReportResponse>();
                    return result ?? new BrokerSalesReportResponse { Success = false, Message = "Invalid response from server" };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return new BrokerSalesReportResponse { Success = false, Message = $"Failed to fetch broker sales report: {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching broker sales report");
                return new BrokerSalesReportResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        // Item Purchase Ledger Report
        public async Task<ItemPurchaseLedgerResponse> GetItemPurchaseLedgerAsync(
            DateTime fromDate, 
            DateTime toDate, 
            string itemCode,
            string itemName,
            string? variety = null,
            decimal? packSize = null,
            string? status = null)
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var url = "api/reports/item-purchase-ledger";
                var queryParams = new List<string>();
                
                queryParams.Add($"fromDate={fromDate:yyyy-MM-dd}");
                queryParams.Add($"toDate={toDate:yyyy-MM-dd}");
                queryParams.Add($"itemCode={Uri.EscapeDataString(itemCode)}");
                queryParams.Add($"itemName={Uri.EscapeDataString(itemName)}");
                
                if (!string.IsNullOrEmpty(variety))
                {
                    queryParams.Add($"variety={Uri.EscapeDataString(variety)}");
                }
                
                if (packSize.HasValue)
                {
                    queryParams.Add($"packSize={packSize.Value}");
                }
                
                if (!string.IsNullOrEmpty(status))
                {
                    queryParams.Add($"status={Uri.EscapeDataString(status)}");
                }
                
                if (queryParams.Any())
                {
                    url += "?" + string.Join("&", queryParams);
                }

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ItemPurchaseLedgerResponse>();
                    return result ?? new ItemPurchaseLedgerResponse { Success = false, Message = "Invalid response from server" };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return new ItemPurchaseLedgerResponse { Success = false, Message = $"Failed to fetch item purchase ledger: {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching item purchase ledger");
                return new ItemPurchaseLedgerResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        // Customer Sales Ledger Report
        public async Task<CustomerSalesLedgerResponse> GetCustomerSalesLedgerAsync(
            DateTime fromDate,
            DateTime toDate,
            string? customerAccount = null,
            string? customerName = null,
            string reportType = "Summary",
            bool taxReport = false,
            bool taxReportSummary = false,
            string? itemCode = null,
            string? itemName = null,
            string? variety = null,
            decimal? packSize = null,
            string? status = null)
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var url = "api/reports/customer-sales-ledger";
                var queryParams = new List<string>();
                
                queryParams.Add($"fromDate={fromDate:yyyy-MM-dd}");
                queryParams.Add($"toDate={toDate:yyyy-MM-dd}");
                queryParams.Add($"reportType={Uri.EscapeDataString(reportType)}");
                
                if (!string.IsNullOrEmpty(customerAccount))
                {
                    queryParams.Add($"customerAccount={Uri.EscapeDataString(customerAccount)}");
                }
                
                if (!string.IsNullOrEmpty(customerName))
                {
                    queryParams.Add($"customerName={Uri.EscapeDataString(customerName)}");
                }
                
                if (taxReport)
                {
                    queryParams.Add("taxReport=true");
                }
                
                if (taxReportSummary)
                {
                    queryParams.Add("taxReportSummary=true");
                }
                
                if (!string.IsNullOrEmpty(itemCode))
                {
                    queryParams.Add($"itemCode={Uri.EscapeDataString(itemCode)}");
                }
                
                if (!string.IsNullOrEmpty(itemName))
                {
                    queryParams.Add($"itemName={Uri.EscapeDataString(itemName)}");
                }
                
                if (!string.IsNullOrEmpty(variety))
                {
                    queryParams.Add($"variety={Uri.EscapeDataString(variety)}");
                }
                
                if (packSize.HasValue)
                {
                    queryParams.Add($"packSize={packSize.Value}");
                }
                
                if (!string.IsNullOrEmpty(status))
                {
                    queryParams.Add($"status={Uri.EscapeDataString(status)}");
                }
                
                if (queryParams.Any())
                {
                    url += "?" + string.Join("&", queryParams);
                }

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CustomerSalesLedgerResponse>();
                    return result ?? new CustomerSalesLedgerResponse { Success = false, Message = "Invalid response from server" };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return new CustomerSalesLedgerResponse { Success = false, Message = $"Failed to fetch customer sales ledger: {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching customer sales ledger");
                return new CustomerSalesLedgerResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        // Item Sales Ledger Report
        public async Task<ItemSalesLedgerResponse> GetItemSalesLedgerAsync(
            DateTime fromDate, 
            DateTime toDate, 
            string itemCode,
            string itemName,
            string? variety = null,
            decimal? packSize = null,
            string? status = null)
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var url = "api/reports/item-sales-ledger";
                var queryParams = new List<string>();
                
                queryParams.Add($"fromDate={fromDate:yyyy-MM-dd}");
                queryParams.Add($"toDate={toDate:yyyy-MM-dd}");
                queryParams.Add($"itemCode={Uri.EscapeDataString(itemCode)}");
                queryParams.Add($"itemName={Uri.EscapeDataString(itemName)}");
                
                if (!string.IsNullOrEmpty(variety))
                {
                    queryParams.Add($"variety={Uri.EscapeDataString(variety)}");
                }
                
                if (packSize.HasValue)
                {
                    queryParams.Add($"packSize={packSize.Value}");
                }
                
                if (!string.IsNullOrEmpty(status))
                {
                    queryParams.Add($"status={Uri.EscapeDataString(status)}");
                }
                
                if (queryParams.Any())
                {
                    url += "?" + string.Join("&", queryParams);
                }

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ItemSalesLedgerResponse>();
                    return result ?? new ItemSalesLedgerResponse { Success = false, Message = "Invalid response from server" };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return new ItemSalesLedgerResponse { Success = false, Message = $"Failed to fetch item sales ledger: {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching item sales ledger");
                return new ItemSalesLedgerResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        // Supplier Purchase Ledger Report
        public async Task<SupplierPurchaseLedgerResponse> GetSupplierPurchaseLedgerAsync(
            DateTime fromDate, 
            DateTime toDate, 
            string? supplierAccount = null,
            string reportType = "Summary",
            bool allSuppliers = true)
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var url = "api/reports/supplier-purchase-ledger";
                var queryParams = new List<string>();
                
                queryParams.Add($"fromDate={fromDate:yyyy-MM-dd}");
                queryParams.Add($"toDate={toDate:yyyy-MM-dd}");
                queryParams.Add($"reportType={Uri.EscapeDataString(reportType)}");
                queryParams.Add($"allSuppliers={allSuppliers}");
                
                if (!string.IsNullOrEmpty(supplierAccount))
                {
                    queryParams.Add($"supplierAccount={Uri.EscapeDataString(supplierAccount)}");
                }
                
                if (queryParams.Any())
                {
                    url += "?" + string.Join("&", queryParams);
                }

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<SupplierPurchaseLedgerResponse>();
                    return result ?? new SupplierPurchaseLedgerResponse { Success = false, Message = "Invalid response from server" };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return new SupplierPurchaseLedgerResponse { Success = false, Message = $"Failed to fetch supplier purchase ledger: {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching supplier purchase ledger");
                return new SupplierPurchaseLedgerResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        // Supplier Tax Ledger Report
        public async Task<SupplierTaxLedgerResponse> GetSupplierTaxLedgerAsync(
            DateTime fromDate, 
            DateTime toDate, 
            string fromAccount, 
            string uptoAccount,
            bool taxCalculateAsPerBag,
            decimal taxRatePerBag,
            string reportType)
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var url = "api/reports/supplier-tax-ledger";
                var queryParams = new List<string>();
                
                queryParams.Add($"fromDate={fromDate:yyyy-MM-dd}");
                queryParams.Add($"toDate={toDate:yyyy-MM-dd}");
                queryParams.Add($"fromAccount={Uri.EscapeDataString(fromAccount)}");
                queryParams.Add($"uptoAccount={Uri.EscapeDataString(uptoAccount)}");
                queryParams.Add($"taxCalculateAsPerBag={taxCalculateAsPerBag}");
                queryParams.Add($"taxRatePerBag={taxRatePerBag}");
                queryParams.Add($"reportType={Uri.EscapeDataString(reportType)}");
                
                if (queryParams.Any())
                {
                    url += "?" + string.Join("&", queryParams);
                }

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<SupplierTaxLedgerResponse>();
                    return result ?? new SupplierTaxLedgerResponse { Success = false, Message = "Invalid response from server" };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return new SupplierTaxLedgerResponse { Success = false, Message = $"Failed to fetch supplier tax ledger: {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching supplier tax ledger");
                return new SupplierTaxLedgerResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        // Item Purchase Register Report
        public async Task<ItemPurchaseRegisterResponse> GetItemPurchaseRegisterAsync(
            DateTime fromDate, 
            DateTime toDate)
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var url = "api/reports/item-purchase-register";
                var queryParams = new List<string>();
                
                queryParams.Add($"fromDate={fromDate:yyyy-MM-dd}");
                queryParams.Add($"toDate={toDate:yyyy-MM-dd}");
                
                if (queryParams.Any())
                {
                    url += "?" + string.Join("&", queryParams);
                }

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ItemPurchaseRegisterResponse>();
                    return result ?? new ItemPurchaseRegisterResponse { Success = false, Message = "Invalid response from server" };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return new ItemPurchaseRegisterResponse { Success = false, Message = $"Failed to fetch item purchase register: {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching item purchase register");
                return new ItemPurchaseRegisterResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        // Item Sales Register Report
        public async Task<ItemSalesRegisterResponse> GetItemSalesRegisterAsync(
            DateTime fromDate, 
            DateTime toDate)
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var url = "api/reports/item-sales-register";
                var queryParams = new List<string>();
                
                queryParams.Add($"fromDate={fromDate:yyyy-MM-dd}");
                queryParams.Add($"toDate={toDate:yyyy-MM-dd}");
                
                if (queryParams.Any())
                {
                    url += "?" + string.Join("&", queryParams);
                }

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ItemSalesRegisterResponse>();
                    return result ?? new ItemSalesRegisterResponse { Success = false, Message = "Invalid response from server" };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return new ItemSalesRegisterResponse { Success = false, Message = $"Failed to fetch item sales register: {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching item sales register");
                return new ItemSalesRegisterResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        // Customer Aging Report
        public async Task<CustomerAgingResponse> GetCustomerAgingAsync(
            string? fromAccount = null,
            string? uptoAccount = null,
            DateTime? asOnDate = null,
            string? reportType = null)
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var url = "api/reports/customer-aging";
                var queryParams = new List<string>();
                
                if (!string.IsNullOrEmpty(fromAccount))
                {
                    queryParams.Add($"fromAccount={Uri.EscapeDataString(fromAccount)}");
                }
                if (!string.IsNullOrEmpty(uptoAccount))
                {
                    queryParams.Add($"uptoAccount={Uri.EscapeDataString(uptoAccount)}");
                }
                if (asOnDate.HasValue)
                {
                    queryParams.Add($"asOnDate={asOnDate.Value:yyyy-MM-dd}");
                }
                if (!string.IsNullOrEmpty(reportType))
                {
                    queryParams.Add($"reportType={Uri.EscapeDataString(reportType)}");
                }
                
                if (queryParams.Any())
                {
                    url += "?" + string.Join("&", queryParams);
                }

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CustomerAgingResponse>();
                    return result ?? new CustomerAgingResponse { Success = false, Message = "Invalid response from server" };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return new CustomerAgingResponse { Success = false, Message = $"Failed to fetch customer aging report: {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching customer aging report");
                return new CustomerAgingResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<SupplierAgingResponse> GetSupplierAgingAsync(
            string? fromAccount = null,
            string? uptoAccount = null,
            DateTime? asOnDate = null,
            string? reportType = null,
            decimal? minBalance = null)
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var url = "api/reports/supplier-aging";
                var queryParams = new List<string>();
                
                if (!string.IsNullOrEmpty(fromAccount))
                {
                    queryParams.Add($"fromAccount={Uri.EscapeDataString(fromAccount)}");
                }
                if (!string.IsNullOrEmpty(uptoAccount))
                {
                    queryParams.Add($"uptoAccount={Uri.EscapeDataString(uptoAccount)}");
                }
                if (asOnDate.HasValue)
                {
                    queryParams.Add($"asOnDate={asOnDate.Value:yyyy-MM-dd}");
                }
                if (!string.IsNullOrEmpty(reportType))
                {
                    queryParams.Add($"reportType={Uri.EscapeDataString(reportType)}");
                }
                if (minBalance.HasValue)
                {
                    queryParams.Add($"minBalance={minBalance.Value}");
                }
                
                if (queryParams.Any())
                {
                    url += "?" + string.Join("&", queryParams);
                }

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<SupplierAgingResponse>();
                    return result ?? new SupplierAgingResponse { Success = false, Message = "Invalid response from server" };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return new SupplierAgingResponse { Success = false, Message = $"Failed to fetch supplier aging report: {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching supplier aging report");
                return new SupplierAgingResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        // Session Methods
        public async Task<DateTime?> GetSessionStartDateAsync()
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var response = await client.GetAsync("api/clientdata/session/start-date");
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<DateTime?>();
                    return result;
                }

                _logger.LogWarning($"Failed to fetch session start date: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching session start date");
                return null;
            }
        }

        // Item Purchase Summary Report
        public async Task<ItemPurchaseSummaryResponse> GetItemPurchaseSummaryAsync(
            DateTime fromDate, 
            DateTime toDate, 
            string? itemGroup = null)
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var url = "api/reports/item-purchase-summary";
                var queryParams = new List<string>();
                
                queryParams.Add($"fromDate={fromDate:yyyy-MM-dd}");
                queryParams.Add($"toDate={toDate:yyyy-MM-dd}");
                
                if (!string.IsNullOrEmpty(itemGroup))
                {
                    queryParams.Add($"itemGroup={Uri.EscapeDataString(itemGroup)}");
                }
                
                if (queryParams.Any())
                {
                    url += "?" + string.Join("&", queryParams);
                }

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ItemPurchaseSummaryResponse>();
                    return result ?? new ItemPurchaseSummaryResponse { Success = false, Message = "Invalid response from server" };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return new ItemPurchaseSummaryResponse { Success = false, Message = $"Failed to fetch item purchase summary: {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching item purchase summary");
                return new ItemPurchaseSummaryResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        // Item Sales Summary Report
        public async Task<ItemSalesSummaryResponse> GetItemSalesSummaryAsync(
            DateTime fromDate, 
            DateTime toDate, 
            string? invoiceType = null, 
            string? itemGroup = null)
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var url = $"api/reports/item-sales-summary?fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}";
                
                if (!string.IsNullOrEmpty(invoiceType))
                {
                    url += $"&invoiceType={Uri.EscapeDataString(invoiceType)}";
                }
                
                if (!string.IsNullOrEmpty(itemGroup))
                {
                    url += $"&itemGroup={Uri.EscapeDataString(itemGroup)}";
                }
                
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ItemSalesSummaryResponse>();
                    return result ?? new ItemSalesSummaryResponse { Success = false, Message = "Invalid response from server" };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return new ItemSalesSummaryResponse { Success = false, Message = $"Failed to fetch item sales summary: {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching item sales summary");
                return new ItemSalesSummaryResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        // Get Item Groups for dropdown
        public async Task<List<ClientItemGroup>> GetItemGroupsAsync()
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var response = await client.GetAsync("api/clientdata/item-groups");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    // The API returns List<Dictionary<string, object>>, so we need to parse it
                    var rawData = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(content, new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    if (rawData == null)
                        return new List<ClientItemGroup>();
                    
                    var itemGroups = new List<ClientItemGroup>();
                    foreach (var item in rawData)
                    {
                        itemGroups.Add(new ClientItemGroup
                        {
                            GroupCode = item.ContainsKey("GroupCode") ? item["GroupCode"]?.ToString() ?? "" : "",
                            GroupName = item.ContainsKey("GroupName") ? item["GroupName"]?.ToString() ?? "" : ""
                        });
                    }
                    return itemGroups;
                }

                _logger.LogWarning($"Failed to fetch item groups: {response.StatusCode}");
                return new List<ClientItemGroup>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching item groups");
                return new List<ClientItemGroup>();
            }
        }
    }
}
