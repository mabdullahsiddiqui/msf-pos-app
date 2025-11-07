using pos_app.Client.Models;
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.JSInterop;

namespace pos_app.Client.Services
{
    public class SuperAdminService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IJSRuntime _jsRuntime;
        private readonly AuthenticationStateService _authStateService;

        public SuperAdminService(HttpClient httpClient, IConfiguration configuration, IJSRuntime jsRuntime, AuthenticationStateService authStateService)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _jsRuntime = jsRuntime;
            _authStateService = authStateService;
        }

        private async Task<HttpClient> CreateAuthedClientAsync()
        {
            var token = _authStateService.Token;
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            return _httpClient;
        }

        public async Task<SuperAdminLoginResponse> LoginAsync(SuperAdminLoginRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/superadmin/login", request);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<SuperAdminLoginResponse>();
                    if (result != null && !string.IsNullOrEmpty(result.Token))
                    {
                        return result;
                    }
                }
                
                var errorResponse = await response.Content.ReadFromJsonAsync<SuperAdminLoginResponse>();
                return errorResponse ?? new SuperAdminLoginResponse 
                { 
                    Success = false, 
                    Message = "Login failed" 
                };
            }
            catch (JsonException ex)
            {
                return new SuperAdminLoginResponse
                {
                    Success = false,
                    Message = "Invalid response from server. The API might not be fully deployed yet."
                };
            }
            catch (Exception ex)
            {
                return new SuperAdminLoginResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        public async Task<UsersResponse> GetUsersAsync()
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var response = await client.GetAsync("api/superadmin/users");
                var responseContent = await response.Content.ReadAsStringAsync();

                var result = JsonSerializer.Deserialize<UsersResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new UsersResponse { Success = false, Message = "Failed to fetch users" };
            }
            catch (Exception ex)
            {
                return new UsersResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        public async Task<SuperAdminLoginResponse> CreateUserAsync(CreateUserRequest request)
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var response = await client.PostAsJsonAsync("api/superadmin/create-user", request);
                var responseContent = await response.Content.ReadAsStringAsync();

                var result = JsonSerializer.Deserialize<SuperAdminLoginResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new SuperAdminLoginResponse { Success = false, Message = "Failed to create user" };
            }
            catch (Exception ex)
            {
                return new SuperAdminLoginResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        public async Task<SuperAdminTestConnectionResponse> TestUserConnectionAsync(int userId)
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var response = await client.PostAsync($"api/superadmin/test-user-connection/{userId}", null);
                var responseContent = await response.Content.ReadAsStringAsync();

                var result = JsonSerializer.Deserialize<SuperAdminTestConnectionResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new SuperAdminTestConnectionResponse { Success = false, Message = "Connection test failed" };
            }
            catch (Exception ex)
            {
                return new SuperAdminTestConnectionResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        public async Task<SuperAdminLoginResponse> UpdateUserAsync(EditUserRequest request)
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var response = await client.PutAsJsonAsync($"api/superadmin/users/{request.Id}", request);
                var responseContent = await response.Content.ReadAsStringAsync();

                var result = JsonSerializer.Deserialize<SuperAdminLoginResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new SuperAdminLoginResponse { Success = false, Message = "Failed to update user" };
            }
            catch (Exception ex)
            {
                return new SuperAdminLoginResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        public async Task<SalesReportData?> GetSalesReportAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var url = $"api/clientdata/sales-report/{userId}";
                if (startDate.HasValue && endDate.HasValue)
                {
                    url += $"?startDate={startDate.Value:yyyy-MM-dd}&endDate={endDate.Value:yyyy-MM-dd}";
                }

                var response = await _httpClient.GetAsync(url);
                var responseContent = await response.Content.ReadAsStringAsync();

                var result = JsonSerializer.Deserialize<ApiResponse<SalesReportData>>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result?.Data;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<InventoryReportData?> GetInventoryReportAsync(int userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/clientdata/inventory-report/{userId}");
                var responseContent = await response.Content.ReadAsStringAsync();

                var result = JsonSerializer.Deserialize<ApiResponse<InventoryReportData>>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result?.Data;
            }
            catch (Exception ex)
            {
                return null;
            }
        }




        // Forgot Password Flow Methods
        public async Task<SuperAdminForgotPasswordResponse> ForgotPasswordAsync(SuperAdminForgotPasswordRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/superadmin/forgot-password", request);

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<SuperAdminForgotPasswordResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new SuperAdminForgotPasswordResponse { Success = false, Message = "Invalid response from server" };
            }
            catch (Exception ex)
            {
                return new SuperAdminForgotPasswordResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<VerifyCodeResponse> VerifyCodeAsync(VerifyCodeRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/superadmin/verify-code", request);

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<VerifyCodeResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new VerifyCodeResponse { Success = false, Message = "Invalid response from server" };
            }
            catch (Exception ex)
            {
                return new VerifyCodeResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request)
        {
            try
            {
                Console.WriteLine("SuperAdminService ResetPassword request: " + System.Text.Json.JsonSerializer.Serialize(request));
                var response = await _httpClient.PostAsJsonAsync("api/superadmin/reset-password", request);

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine("SuperAdminService ResetPassword response: " + responseContent);
                var result = JsonSerializer.Deserialize<ResetPasswordResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new ResetPasswordResponse { Success = false, Message = "Invalid response from server" };
            }
            catch (Exception ex)
            {
                Console.WriteLine("SuperAdminService ResetPassword Exception: " + ex.Message);
                Console.WriteLine("SuperAdminService ResetPassword Exception StackTrace: " + ex.StackTrace);
                return new ResetPasswordResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<ResendCodeResponse> ResendCodeAsync(ResendCodeRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/superadmin/resend-code", request);

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ResendCodeResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new ResendCodeResponse { Success = false, Message = "Invalid response from server" };
            }
            catch (Exception ex)
            {
                return new ResendCodeResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        // Helper methods for storing data between steps
        public async Task SetForgotPasswordEmail(string email)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "forgot_password_email", email);
        }

        public async Task<string> GetForgotPasswordEmail()
        {
            return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "forgot_password_email") ?? string.Empty;
        }

        public async Task SetResetToken(string token)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "reset_token", token);
        }

        public async Task<string> GetResetToken()
        {
            return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "reset_token") ?? string.Empty;
        }

        public async Task ClearForgotPasswordData()
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "forgot_password_email");
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "reset_token");
        }

        // Profile Management Methods
        public async Task<SuperAdminProfileResponse> GetProfileAsync()
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var response = await client.GetAsync("api/superadmin/profile");

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"GetProfileAsync - Raw response: {responseContent}");
                
                var result = JsonSerializer.Deserialize<SuperAdminProfileResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                Console.WriteLine($"GetProfileAsync - Deserialized result: Success={result?.Success}, Message={result?.Message}");
                
                return result ?? new SuperAdminProfileResponse { Success = false, Message = "Invalid response from server" };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetProfileAsync - Exception: {ex.Message}");
                return new SuperAdminProfileResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<SuperAdminUpdateProfileResponse> UpdateProfileAsync(SuperAdminUpdateProfileRequest request)
        {
            try
            {
                Console.WriteLine($"SuperAdminService.UpdateProfileAsync called with: Username={request.Username}, Email={request.Email}, FullName={request.FullName}");
                
                var client = await CreateAuthedClientAsync();
                var response = await client.PutAsJsonAsync("api/superadmin/profile", request);

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Response: {responseContent}");
                
                var result = JsonSerializer.Deserialize<SuperAdminUpdateProfileResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new SuperAdminUpdateProfileResponse { Success = false, Message = "Invalid response from server" };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in SuperAdminService.UpdateProfileAsync: {ex.Message}");
                return new SuperAdminUpdateProfileResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<SuperAdminChangePasswordResponse> ChangePasswordAsync(SuperAdminChangePasswordRequest request)
        {
            try
            {
                var client = await CreateAuthedClientAsync();
                var response = await client.PostAsJsonAsync("api/superadmin/change-password", request);

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<SuperAdminChangePasswordResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new SuperAdminChangePasswordResponse { Success = false, Message = "Invalid response from server" };
            }
            catch (Exception ex)
            {
                return new SuperAdminChangePasswordResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }

    public class SuperAdminTestConnectionResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public ConnectionDetails? ConnectionDetails { get; set; }
    }

    public class ConnectionDetails
    {
        public string ServerName { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string ConnectionName { get; set; } = string.Empty;
        public string DatabaseType { get; set; } = string.Empty;
        public string? LastConnectedAt { get; set; }
    }

    public class EditUserRequest
    {
        public int Id { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string CellNo { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string ServerName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string DatabasePassword { get; set; } = string.Empty;
        public int Port { get; set; } = 1433;
        public string ConnectionName { get; set; } = string.Empty;
    }
}
