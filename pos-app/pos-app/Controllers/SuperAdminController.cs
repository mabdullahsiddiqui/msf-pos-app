using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pos_app.Data;
using pos_app.Models;
using pos_app.Services;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;

namespace pos_app.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SuperAdminController : ControllerBase
    {
        private readonly MasterDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly DataAccessService _dataAccessService;
        
        // In-memory storage for verification codes (for development)
        private static readonly Dictionary<string, (string code, DateTime expiry)> _verificationCodes = new();
        private static readonly Dictionary<string, (string token, DateTime expiry)> _resetTokens = new();

        public SuperAdminController(MasterDbContext context, IConfiguration configuration, DataAccessService dataAccessService)
        {
            _context = context;
            _configuration = configuration;
            _dataAccessService = dataAccessService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] SuperAdminLoginRequest request)
        {
            try
            {
                var superAdmin = await _context.SuperAdmins
                    .FirstOrDefaultAsync(sa => sa.Email == request.Username && sa.IsActive);

                if (superAdmin == null || !VerifyPassword(request.Password, superAdmin.PasswordHash))
                {
                    return Unauthorized(new SuperAdminLoginResponse
                    {
                        Success = false,
                        Message = "Invalid credentials"
                    });
                }

                // Update last login
                superAdmin.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Generate JWT token
                var token = GenerateJwtToken(superAdmin);

                return Ok(new SuperAdminLoginResponse
                {
                    Success = true,
                    Message = "Login successful",
                    Token = token,
                    SuperAdmin = new SuperAdminInfo
                    {
                        Id = superAdmin.Id,
                        Username = superAdmin.Username,
                        Email = superAdmin.Email,
                        FullName = superAdmin.FullName
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new SuperAdminLoginResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        [HttpGet("debug/superadmins")]
        public async Task<IActionResult> GetSuperAdmins()
        {
            try
            {
                var superAdmins = await _context.SuperAdmins.ToListAsync();
                return Ok(new { 
                    Count = superAdmins.Count,
                    SuperAdmins = superAdmins.Select(sa => new {
                        Id = sa.Id,
                        Username = sa.Username,
                        Email = sa.Email,
                        FullName = sa.FullName,
                        IsActive = sa.IsActive,
                        CreatedAt = sa.CreatedAt
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPost("debug/create-superadmin")]
        public async Task<IActionResult> CreateSuperAdmin([FromBody] CreateSuperAdminRequest request)
        {
            try
            {
                // Check if email already exists
                var existingSuperAdmin = await _context.SuperAdmins
                    .FirstOrDefaultAsync(sa => sa.Email == request.Email);

                if (existingSuperAdmin != null)
                {
                    return BadRequest(new { Message = "Super Admin with this email already exists" });
                }

                // Create new Super Admin
                var superAdmin = new SuperAdmin
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = HashPassword(request.Password),
                    FullName = request.FullName,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.SuperAdmins.Add(superAdmin);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    Message = "Super Admin created successfully",
                    Id = superAdmin.Id,
                    Email = superAdmin.Email,
                    Username = superAdmin.Username
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                // Check if email already exists
                var existingUserByEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);
                
                if (existingUserByEmail != null)
                {
                    return BadRequest(new { success = false, message = $"Email '{request.Email}' already exists. Please use a different email address." });
                }

                // Create new user
                var user = new User
                {
                    CompanyName = request.CompanyName,
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    ContactPerson = request.ContactPerson,
                    CellNo = request.CellNo,
                    DatabaseName = request.DatabaseName,
                    ServerName = request.ServerName,
                    Username = string.IsNullOrEmpty(request.Username) ? request.Email : request.Username,
                    DatabasePassword = request.DatabasePassword,
                    Port = request.Port,
                    ConnectionName = request.ConnectionName,
                    DatabaseType = DatabaseType.SQLServer, // Default to SQL Server
                    ConnectionTimeout = 30, // Default timeout
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "User created successfully",
                    userId = user.Id
                });
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE constraint failed") == true)
            {
                return BadRequest(new { success = false, message = "A user with this username or email already exists. Please choose different values." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _context.Users
                    .Where(u => u.IsActive)
                    .Select(u => new
                    {
                        id = u.Id,
                        companyName = u.CompanyName,
                        email = u.Email,
                        contactPerson = u.ContactPerson,
                        cellNo = u.CellNo,
                        databaseName = u.DatabaseName,
                        serverName = u.ServerName,
                        username = u.Username,
                        databasePassword = u.DatabasePassword,
                        port = u.Port,
                        connectionName = u.ConnectionName,
                        createdAt = u.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    users = users
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPut("users/{userId}")]
        public async Task<IActionResult> UpdateUser(int userId, [FromBody] UpdateUserRequest request)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                // Check if email already exists for another user
                var existingUserByEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.Id != userId);
                
                if (existingUserByEmail != null)
                {
                    return BadRequest(new { success = false, message = $"Email '{request.Email}' already exists. Please use a different email address." });
                }

                // Update user properties
                user.CompanyName = request.CompanyName;
                user.Email = request.Email;
                user.ContactPerson = request.ContactPerson;
                user.CellNo = request.CellNo;
                user.ConnectionName = request.ConnectionName;
                user.DatabaseName = request.DatabaseName;
                user.ServerName = request.ServerName;
                user.Username = request.Username;
                user.Port = request.Port;
                
                // Ensure required fields are set
                user.DatabaseType = DatabaseType.SQLServer; // Default to SQL Server
                user.ConnectionTimeout = 30; // Default timeout

                // Update database password only if provided
                if (!string.IsNullOrEmpty(request.DatabasePassword))
                {
                    user.DatabasePassword = request.DatabasePassword;
                }

                // Update password only if provided
                if (!string.IsNullOrEmpty(request.Password))
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "User updated successfully"
                });
            }
            catch (Exception ex)
            {
                // Log the full exception details for debugging
                var innerException = ex.InnerException?.Message ?? "No inner exception";
                return StatusCode(500, new { success = false, message = $"Error: {ex.Message}. Inner exception: {innerException}" });
            }
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] SuperAdminChangePasswordRequest request)
        {
            try
            {
                var superAdmin = await _context.SuperAdmins
                    .FirstOrDefaultAsync(sa => sa.Username == request.Username && sa.IsActive);

                if (superAdmin == null)
                {
                    return Ok(new SuperAdminChangePasswordResponse
                    {
                        Success = false,
                        Message = "Super admin not found."
                    });
                }

                // Verify current password
                if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, superAdmin.PasswordHash))
                {
                    return Ok(new SuperAdminChangePasswordResponse
                    {
                        Success = false,
                        Message = "Current password is incorrect."
                    });
                }

                // Validate new password
                if (request.NewPassword != request.ConfirmPassword)
                {
                    return Ok(new SuperAdminChangePasswordResponse
                    {
                        Success = false,
                        Message = "New passwords do not match."
                    });
                }

                // Hash the new password
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                superAdmin.PasswordHash = hashedPassword;

                await _context.SaveChangesAsync();

                return Ok(new SuperAdminChangePasswordResponse
                {
                    Success = true,
                    Message = "Password changed successfully."
                });
            }
            catch (Exception ex)
            {
                return Ok(new SuperAdminChangePasswordResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }




        [HttpPost("test-user-connection/{userId}")]
        public async Task<IActionResult> TestUserConnection(int userId)
        {
            try
            {
                // Get the user by userId
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                if (user == null)
                {
                    return NotFound(new SuperAdminTestConnectionResponse
                    {
                        Success = false,
                        Message = "User not found or inactive"
                    });
                }

                // Test the connection using DataAccessService
                var connectionResult = await _dataAccessService.TestConnectionAsync(user);

                // Prepare connection details
                var connectionDetails = new ConnectionDetails
                {
                    ServerName = user.ServerName,
                    DatabaseName = user.DatabaseName,
                    ConnectionName = user.ConnectionName,
                    DatabaseType = user.DatabaseType.ToString(),
                    LastConnectedAt = user.LastConnectedAt?.ToString("yyyy-MM-dd HH:mm:ss")
                };

                return Ok(new SuperAdminTestConnectionResponse
                {
                    Success = connectionResult,
                    Message = connectionResult ? "Connection successful" : $"Connection failed: {user.LastError ?? "Unknown error"}",
                    ConnectionDetails = connectionDetails
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new SuperAdminTestConnectionResponse
                {
                    Success = false,
                    Message = $"Error testing connection: {ex.Message}"
                });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // In a real implementation, you would invalidate the JWT token
                return Ok(new { success = true, message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // Forgot Password Flow Endpoints
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] SuperAdminForgotPasswordRequest request)
        {
            try
            {
                var superAdmin = await _context.SuperAdmins
                    .FirstOrDefaultAsync(sa => sa.Email == request.Email && sa.IsActive);

                if (superAdmin == null)
                {
                    return Ok(new ForgotPasswordResponse
                    {
                        Success = false,
                        Message = "Email not found or account is inactive."
                    });
                }

                // Generate 6-digit verification code
                var verificationCode = GenerateVerificationCode();
                var expirationTime = DateTime.UtcNow.AddMinutes(10);

                // Store verification code in memory (for development)
                _verificationCodes[request.Email] = (verificationCode, expirationTime);

                // In a real application, send email here
                // For development, we'll return the code in the response
                return Ok(new ForgotPasswordResponse
                {
                    Success = true,
                    Message = "Verification code sent to your email.",
                    VerificationCode = verificationCode // Remove this in production
                });
            }
            catch (Exception ex)
            {
                return Ok(new ForgotPasswordResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        [HttpPost("verify-code")]
        public async Task<IActionResult> VerifyCode([FromBody] VerifyCodeRequest request)
        {
            try
            {
                var superAdmin = await _context.SuperAdmins
                    .FirstOrDefaultAsync(sa => sa.Email == request.Email && sa.IsActive);

                if (superAdmin == null)
                {
                    return Ok(new VerifyCodeResponse
                    {
                        Success = false,
                        Message = "Email not found or account is inactive."
                    });
                }

                // Check verification code from memory
                if (!_verificationCodes.TryGetValue(request.Email, out var storedCode) ||
                    storedCode.code != request.Code ||
                    storedCode.expiry < DateTime.UtcNow)
                {
                    return Ok(new VerifyCodeResponse
                    {
                        Success = false,
                        Message = "Invalid or expired verification code."
                    });
                }

                // Generate reset token
                var resetToken = GenerateResetToken();
                _resetTokens[request.Email] = (resetToken, DateTime.UtcNow.AddMinutes(30));

                // Remove verification code after successful verification
                _verificationCodes.Remove(request.Email);

                return Ok(new VerifyCodeResponse
                {
                    Success = true,
                    Message = "Code verified successfully.",
                    ResetToken = resetToken
                });
            }
            catch (Exception ex)
            {
                return Ok(new VerifyCodeResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                var superAdmin = await _context.SuperAdmins
                    .FirstOrDefaultAsync(sa => sa.Email == request.Email && sa.IsActive);

                if (superAdmin == null)
                {
                    return Ok(new ResetPasswordResponse
                    {
                        Success = false,
                        Message = "Email not found or account is inactive."
                    });
                }

                // Check reset token from memory
                if (!_resetTokens.TryGetValue(request.Email, out var storedToken) ||
                    storedToken.token != request.ResetToken ||
                    storedToken.expiry < DateTime.UtcNow)
                {
                    return Ok(new ResetPasswordResponse
                    {
                        Success = false,
                        Message = "Invalid or expired reset token."
                    });
                }

                if (request.NewPassword != request.ConfirmPassword)
                {
                    return Ok(new ResetPasswordResponse
                    {
                        Success = false,
                        Message = "Passwords do not match."
                    });
                }

                // Hash the new password
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                superAdmin.PasswordHash = hashedPassword;
                await _context.SaveChangesAsync();

                // Remove reset token after successful password reset
                _resetTokens.Remove(request.Email);

                return Ok(new ResetPasswordResponse
                {
                    Success = true,
                    Message = "Password reset successfully."
                });
            }
            catch (Exception ex)
            {
                return Ok(new ResetPasswordResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        [HttpPost("resend-code")]
        public async Task<IActionResult> ResendCode([FromBody] ResendCodeRequest request)
        {
            try
            {
                var superAdmin = await _context.SuperAdmins
                    .FirstOrDefaultAsync(sa => sa.Email == request.Email && sa.IsActive);

                if (superAdmin == null)
                {
                    return Ok(new ResendCodeResponse
                    {
                        Success = false,
                        Message = "Email not found or account is inactive."
                    });
                }

                // Generate new verification code
                var verificationCode = GenerateVerificationCode();
                var expirationTime = DateTime.UtcNow.AddMinutes(10);

                // Store in memory
                _verificationCodes[request.Email] = (verificationCode, expirationTime);

                // In a real application, send email here
                return Ok(new ResendCodeResponse
                {
                    Success = true,
                    Message = "New verification code sent to your email.",
                    VerificationCode = verificationCode // Remove this in production
                });
            }
            catch (Exception ex)
            {
                return Ok(new ResendCodeResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        private string GenerateVerificationCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        private string GenerateResetToken()
        {
            return Guid.NewGuid().ToString("N");
        }

        // Profile Management Endpoints
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                // Get the current user from the authentication context
                // For now, we'll get the first active super admin as a placeholder
                var superAdmin = await _context.SuperAdmins
                    .FirstOrDefaultAsync(sa => sa.IsActive);

                if (superAdmin == null)
                {
                    return Ok(new SuperAdminProfileResponse
                    {
                        Success = false,
                        Message = "Super admin not found."
                    });
                }

                var profileInfo = new SuperAdminProfileInfo
                {
                    Id = superAdmin.Id,
                    Username = superAdmin.Username,
                    Email = superAdmin.Email,
                    FullName = superAdmin.FullName,
                    CreatedAt = superAdmin.CreatedAt,
                    LastLoginAt = superAdmin.LastLoginAt,
                    IsActive = superAdmin.IsActive
                };

                return Ok(new SuperAdminProfileResponse
                {
                    Success = true,
                    Message = "Profile retrieved successfully.",
                    Data = profileInfo
                });
            }
            catch (Exception ex)
            {
                return Ok(new SuperAdminProfileResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] SuperAdminUpdateProfileRequest request)
        {
            try
            {
                // Get the current user from the authentication context
                // For now, we'll get the first active super admin as a placeholder
                var superAdmin = await _context.SuperAdmins
                    .FirstOrDefaultAsync(sa => sa.IsActive);

                if (superAdmin == null)
                {
                    return Ok(new SuperAdminUpdateProfileResponse
                    {
                        Success = false,
                        Message = "Super admin not found."
                    });
                }

                // Check if username or email already exists (excluding current user)
                var existingUsername = await _context.SuperAdmins
                    .FirstOrDefaultAsync(sa => sa.Username == request.Username && sa.Id != superAdmin.Id);
                
                if (existingUsername != null)
                {
                    return Ok(new SuperAdminUpdateProfileResponse
                    {
                        Success = false,
                        Message = "Username already exists."
                    });
                }

                var existingEmail = await _context.SuperAdmins
                    .FirstOrDefaultAsync(sa => sa.Email == request.Email && sa.Id != superAdmin.Id);
                
                if (existingEmail != null)
                {
                    return Ok(new SuperAdminUpdateProfileResponse
                    {
                        Success = false,
                        Message = "Email already exists."
                    });
                }

                // Update profile information
                superAdmin.Username = request.Username;
                superAdmin.Email = request.Email;
                superAdmin.FullName = request.FullName;

                await _context.SaveChangesAsync();

                return Ok(new SuperAdminUpdateProfileResponse
                {
                    Success = true,
                    Message = "Profile updated successfully."
                });
            }
            catch (Exception ex)
            {
                return Ok(new SuperAdminUpdateProfileResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }


        private string HashPassword(string password)
        {
            // For testing purposes, allow empty password
            if (string.IsNullOrEmpty(password))
            {
                return ""; // Empty password for testing
            }
            
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        private bool VerifyPassword(string password, string hash)
        {
            // For testing purposes, allow empty password
            if (string.IsNullOrEmpty(password) && string.IsNullOrEmpty(hash))
            {
                return true; // Empty password matches empty hash
            }
            
            try
            {
                // Use BCrypt to verify password
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                // If BCrypt fails, fall back to SHA256 for backward compatibility
                return HashPassword(password) == hash;
            }
        }

        private string GenerateJwtToken(SuperAdmin superAdmin)
        {
            // This is a simplified JWT generation
            // In production, use proper JWT library
            var jwtKey = _configuration["JWT:Key"] ?? "your-secret-key";
            var token = $"{superAdmin.Id}:{superAdmin.Username}:{DateTime.UtcNow.Ticks}";
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(token));
        }

        private string GenerateTemporaryPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }

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

    public class SuperAdminChangePasswordRequest
    {
        public string Username { get; set; } = string.Empty;
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class SuperAdminForgotPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class SuperAdminUpdateProfileRequest
    {
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class SuperAdminChangePasswordResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class SuperAdminUpdateProfileResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class CreateSuperAdminRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }


    public class ForgotPasswordResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string VerificationCode { get; set; } = string.Empty;
    }

    public class VerifyCodeRequest
    {
        public string Email { get; set; } = string.Empty;
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
        public string Email { get; set; } = string.Empty;
        public string ResetToken { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ResetPasswordResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ResendCodeRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ResendCodeResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string VerificationCode { get; set; } = string.Empty;
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

    public class UpdateUserRequest
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
}
