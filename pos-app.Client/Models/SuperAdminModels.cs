using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace pos_app.Client.Models
{
    public class SuperAdminLoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class SuperAdminLoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public SuperAdminInfo? SuperAdmin { get; set; }
    }

    public class SuperAdminInfo
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }

    public class CreateUserRequest
    {
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

    public class UserInfo
    {
        public int Id { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string CellNo { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string ServerName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string DatabasePassword { get; set; } = string.Empty;
        public int Port { get; set; } = 1433;
        public string ConnectionName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class UsersResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<UserInfo> Users { get; set; } = new();
    }

    public class SalesReportData
    {
        public int TotalSales { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class InventoryReportData
    {
        public List<InventoryItem> Inventory { get; set; } = new();
        public int TotalItems { get; set; }
    }

    public class InventoryItem
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalValue { get; set; }
    }

    public class ConnectionTestResponse
    {
        [JsonPropertyName("Success")]
        public bool Success { get; set; }
        
        [JsonPropertyName("Connected")]
        public bool Connected { get; set; }
        
        [JsonPropertyName("Message")]
        public string Message { get; set; } = string.Empty;
    }





    public class SuperAdminUpdateProfileResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class SuperAdminChangePasswordResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // Forgot Password Flow Models
    public class SuperAdminForgotPasswordRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;
    }

    public class SuperAdminForgotPasswordResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string VerificationCode { get; set; } = string.Empty; // For development/testing
    }

    public class VerifyCodeRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Verification code is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Verification code must be 6 digits")]
        public string Code { get; set; } = string.Empty;
    }

    public class VerifyCodeResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ResetToken { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Reset token is required")]
        public string ResetToken { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
        public string NewPassword { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ResetPasswordResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ResendCodeRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;
    }

    public class ResendCodeResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string VerificationCode { get; set; } = string.Empty; // For development/testing
    }

    // Profile Management Models
    public class SuperAdminProfileInfo
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class SuperAdminProfileResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public SuperAdminProfileInfo? Data { get; set; }
    }

    public class SuperAdminUpdateProfileRequest
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 100 characters")]
        public string Username { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters")]
        public string FullName { get; set; } = string.Empty;
    }

    public class SuperAdminChangePasswordRequest
    {
        public string Username { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Current password is required")]
        public string CurrentPassword { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
        public string NewPassword { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
