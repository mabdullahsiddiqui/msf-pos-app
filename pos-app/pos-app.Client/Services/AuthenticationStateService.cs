using pos_app.Client.Models;
using Microsoft.JSInterop;
using System.Text.Json;

namespace pos_app.Client.Services
{
    public class AuthenticationStateService
    {
        private SuperAdminInfo? _currentSuperAdmin;
        private string? _token;
        private readonly AuthService _authService;
        private readonly IJSRuntime _jsRuntime;

        public AuthenticationStateService(AuthService authService, IJSRuntime jsRuntime)
        {
            _authService = authService;
            _jsRuntime = jsRuntime;
        }

        public bool IsAuthenticated => _currentSuperAdmin != null && !string.IsNullOrEmpty(_token);
        
        public SuperAdminInfo? CurrentSuperAdmin => _currentSuperAdmin;
        
        public string? Token => _token;

        public event Action? OnAuthenticationStateChanged;

        public async Task SetAuthenticationState(SuperAdminInfo superAdmin, string token)
        {
            _currentSuperAdmin = superAdmin;
            _token = token;
            
            // Store in localStorage for persistence
            await StoreSuperAdminDataAsync(superAdmin, token);
            
            OnAuthenticationStateChanged?.Invoke();
        }

        public async Task ClearAuthenticationState()
        {
            _currentSuperAdmin = null;
            _token = null;
            
            // Remove from localStorage
            await RemoveSuperAdminDataAsync();
            
            OnAuthenticationStateChanged?.Invoke();
        }

        public async Task Logout()
        {
            await ClearAuthenticationState();
            // Note: We don't clear main app authentication as Super Admin is separate
            // This prevents conflicts with regular user authentication
        }

        public async Task<bool> IsSuperAdminAuthenticatedAsync()
        {
            try
            {
                Console.WriteLine("AuthenticationStateService: Checking authentication...");
                
                // First check if we have data in memory
                if (_currentSuperAdmin != null && !string.IsNullOrEmpty(_token))
                {
                    Console.WriteLine("AuthenticationStateService: Already authenticated in memory");
                    return true;
                }

                Console.WriteLine("AuthenticationStateService: Not in memory, restoring from localStorage...");
                
                // If not, try to restore from localStorage
                await RestoreSuperAdminDataAsync();
                
                // Check if restoration was successful
                var isAuthenticated = _currentSuperAdmin != null && !string.IsNullOrEmpty(_token);
                
                Console.WriteLine($"AuthenticationStateService: Restoration result: {isAuthenticated}");
                
                // If we successfully restored, trigger the state change event
                if (isAuthenticated)
                {
                    Console.WriteLine("AuthenticationStateService: Triggering state change event");
                    OnAuthenticationStateChanged?.Invoke();
                }
                
                return isAuthenticated;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AuthenticationStateService: Exception during auth check: {ex.Message}");
                return false;
            }
        }

        private async Task StoreSuperAdminDataAsync(SuperAdminInfo superAdmin, string token)
        {
            try
            {
                var data = new
                {
                    SuperAdmin = superAdmin,
                    Token = token,
                    Timestamp = DateTime.UtcNow
                };
                
                var json = JsonSerializer.Serialize(data);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "superadmin_auth", json);
            }
            catch
            {
                // Ignore errors
            }
        }

        private async Task RestoreSuperAdminDataAsync()
        {
            try
            {
                var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "superadmin_auth");
                
                if (!string.IsNullOrEmpty(json))
                {
                    var data = JsonSerializer.Deserialize<SuperAdminAuthData>(json);
                    if (data != null && data.SuperAdmin != null && !string.IsNullOrEmpty(data.Token))
                    {
                        // Check if token is not too old (24 hours)
                        var timeDiff = DateTime.UtcNow - data.Timestamp;
                        
                        if (timeDiff < TimeSpan.FromHours(24))
                        {
                            _currentSuperAdmin = data.SuperAdmin;
                            _token = data.Token;
                            // Trigger the event to notify components
                            OnAuthenticationStateChanged?.Invoke();
                        }
                        else
                        {
                            // Token expired, remove it
                            await RemoveSuperAdminDataAsync();
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors
            }
        }

        private async Task RemoveSuperAdminDataAsync()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "superadmin_auth");
            }
            catch
            {
                // Ignore errors
            }
        }

        private class SuperAdminAuthData
        {
            public SuperAdminInfo? SuperAdmin { get; set; }
            public string? Token { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}
