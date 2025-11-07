using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using pos_app.Models;
using pos_app.Services;
using pos_app.Data;
using System.Security.Claims;

namespace pos_app.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly MasterDbContext _context;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AuthService authService, MasterDbContext context, ILogger<AuthController> logger)
        {
            _authService = authService;
            _context = context;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                _logger.LogWarning("Login validation failed: {Errors}", errors);
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = $"Invalid request data: {errors}"
                });
            }

            var result = await _authService.LoginUserAsync(request);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }

        [HttpGet("profile")]
        public async Task<ActionResult<UserProfileResponse>> GetProfile()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Unauthorized(new UserProfileResponse
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var user = await _authService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new UserProfileResponse
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                return Ok(new UserProfileResponse
                {
                    Success = true,
                    Message = "Profile retrieved successfully",
                    Data = new UserProfileData
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        CompanyName = user.CompanyName,
                        ContactPerson = user.ContactPerson,
                        CellNo = user.CellNo,
                        ConnectionName = user.ConnectionName,
                        ServerName = user.ServerName,
                        DatabaseName = user.DatabaseName,
                        Port = user.Port,
                        DatabaseType = user.DatabaseType.ToString(),
                        CreatedAt = user.CreatedAt,
                        IsActive = user.IsActive
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user profile");
                return StatusCode(500, new UserProfileResponse
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        [HttpPost("signup")]
        public async Task<ActionResult<SignupResponse>> Signup([FromBody] SignupRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new SignupResponse
                {
                    Success = false,
                    Message = "Invalid request data"
                });
            }

            try
            {
                var result = await _authService.SignupUserAsync(request);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during signup");
                return StatusCode(500, new SignupResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message} {ex.InnerException?.Message}"
                });
            }
        }

        [HttpPost("change-password")]
        public async Task<ActionResult<ChangePasswordResponse>> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ChangePasswordResponse
                {
                    Success = false,
                    Message = "Invalid request data"
                });
            }

            try
            {
                var result = await _authService.ChangePasswordAsync(request);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password change");
                return StatusCode(500, new ChangePasswordResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        [HttpPost("user/change-password")]
        [Authorize]
        public async Task<ActionResult<UserChangePasswordResponse>> ChangeUserPassword([FromBody] UserChangePasswordRequest request)
        {
            _logger.LogInformation("ChangeUserPassword endpoint called");
            _logger.LogInformation($"Request - Username: {request.Username}, CurrentPassword length: {request.CurrentPassword?.Length}, NewPassword length: {request.NewPassword?.Length}, ConfirmPassword length: {request.ConfirmPassword?.Length}");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state is invalid");
                return BadRequest(new UserChangePasswordResponse
                {
                    Success = false,
                    Message = "Invalid request data"
                });
            }

            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation($"Current user ID: {userId}");
                
                if (userId == 0)
                {
                    _logger.LogWarning("User not authenticated");
                    return Unauthorized(new UserChangePasswordResponse
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var user = await _authService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"User with ID {userId} not found");
                    return NotFound(new UserChangePasswordResponse
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                _logger.LogInformation($"Found user: {user.Username}");

                // Verify current password
                bool passwordValid = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash);
                _logger.LogInformation($"Current password verification: {passwordValid}");
                
                if (!passwordValid)
                {
                    return Ok(new UserChangePasswordResponse
                    {
                        Success = false,
                        Message = "Current password is incorrect."
                    });
                }

                // Validate new password
                if (request.NewPassword != request.ConfirmPassword)
                {
                    _logger.LogWarning("New passwords do not match");
                    return Ok(new UserChangePasswordResponse
                    {
                        Success = false,
                        Message = "New passwords do not match."
                    });
                }

                // Hash the new password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Password changed successfully");
                return Ok(new UserChangePasswordResponse
                {
                    Success = true,
                    Message = "Password changed successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user password change");
                return StatusCode(500, new UserChangePasswordResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ForgotPasswordResponse
                {
                    Success = false,
                    Message = "Invalid request data"
                });
            }

            try
            {
                var result = await _authService.ForgotPasswordAsync(request);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot password");
                return StatusCode(500, new ForgotPasswordResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        [HttpPost("update-profile")]
        public async Task<ActionResult<UpdateProfileResponse>> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new UpdateProfileResponse
                {
                    Success = false,
                    Message = "Invalid request data"
                });
            }

            try
            {
                var result = await _authService.UpdateProfileAsync(request);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during profile update");
                return StatusCode(500, new UpdateProfileResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        [HttpPut("profile")]
        [Authorize]
        public async Task<ActionResult<UpdateProfileResponse>> UpdateUserProfile([FromBody] UpdateProfileRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new UpdateProfileResponse
                {
                    Success = false,
                    Message = "Invalid request data"
                });
            }

            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Unauthorized(new UpdateProfileResponse
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var user = await _authService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new UpdateProfileResponse
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                // Update profile fields
                user.CompanyName = request.CompanyName;
                user.ContactPerson = request.ContactPerson;
                user.CellNo = request.CellNo;

                await _context.SaveChangesAsync();

                return Ok(new UpdateProfileResponse
                {
                    Success = true,
                    Message = "Profile updated successfully",
                    User = new
                    {
                        id = user.Id,
                        companyName = user.CompanyName,
                        email = user.Email,
                        contactPerson = user.ContactPerson,
                        cellNo = user.CellNo
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during profile update");
                return StatusCode(500, new UpdateProfileResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        [HttpPost("logout")]
        public async Task<ActionResult<LogoutResponse>> Logout()
        {
            try
            {
                // In a real implementation, you would invalidate the JWT token
                // For now, we'll just return success
                return Ok(new LogoutResponse
                {
                    Success = true,
                    Message = "Logged out successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new LogoutResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        // Helper method to get current user ID
        private int GetCurrentUserId()
        {
            // Debug: Log all available claims
            _logger.LogInformation("User.Identity.IsAuthenticated: {IsAuthenticated}", User.Identity?.IsAuthenticated);
            _logger.LogInformation("User.Identity.Name: {Name}", User.Identity?.Name);
            _logger.LogInformation("User.Identity.AuthenticationType: {AuthType}", User.Identity?.AuthenticationType);
            
            var allClaims = User.Claims.Select(c => $"{c.Type}={c.Value}").ToList();
            _logger.LogInformation("All claims: {Claims}", string.Join(", ", allClaims));
            
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
