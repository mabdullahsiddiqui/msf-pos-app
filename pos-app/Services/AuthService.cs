using Microsoft.EntityFrameworkCore;
using pos_app.Data;
using pos_app.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace pos_app.Services
{
    public class AuthService
    {
        private readonly MasterDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(MasterDbContext context, IConfiguration configuration, ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<SignupResponse> SignupUserAsync(SignupRequest request)
        {
            try
            {
                // Check if user already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == request.Email);

                if (existingUser != null)
                {
                    return new SignupResponse
                    {
                        Success = false,
                        Message = "User with this email already exists"
                    };
                }

                // Create new user with database connection info
                var newUser = new User
                {
                    Username = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    CompanyName = request.CompanyName,
                    Email = request.Email,
                    ContactPerson = request.ContactPerson,
                    CellNo = request.CellNo,
                    DatabaseName = request.DatabaseName,
                    ServerName = request.ServerName,
                    DatabasePassword = request.DatabasePassword,
                    Port = request.Port ?? 1433,
                    ConnectionName = request.ConnectionName,
                    DatabaseType = DatabaseType.SQLServer,
                    ConnectionTimeout = 30,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                // Generate JWT token
                var token = GenerateJwtToken(newUser);

                return new SignupResponse
                {
                    Success = true,
                    Message = "User created successfully",
                    Token = token,
                    ClientName = newUser.CompanyName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return new SignupResponse
                {
                    Success = false,
                    Message = $"Error creating user: {ex.Message}"
                };
            }
        }

        public async Task<LoginResponse> LoginUserAsync(LoginRequest request)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

                if (user == null)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    };
                }

                // Try to verify password with BCrypt
                bool passwordValid = false;
                try
                {
                    passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
                }
                catch (BCrypt.Net.SaltParseException)
                {
                    // Handle old password format - for now, treat as invalid
                    // In production, you might want to migrate old passwords
                    _logger.LogWarning($"Invalid salt version for user {user.Username}. Password needs to be reset.");
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Password format is outdated. Please reset your password or contact support."
                    };
                }

                if (!passwordValid)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    };
                }
                
                // Update last login time
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Generate JWT token
                var token = GenerateJwtToken(user);

                return new LoginResponse
                {
                    Success = true,
                    Message = "Login successful",
                    Token = token,
                    ClientName = user.CompanyName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return new LoginResponse
                {
                    Success = false,
                    Message = "Login failed due to server error"
                };
            }
        }

        public async Task<ChangePasswordResponse> ChangePasswordAsync(ChangePasswordRequest request)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

                if (user == null)
                {
                    return new ChangePasswordResponse
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                // Verify current password
                bool currentPasswordValid = false;
                try
                {
                    currentPasswordValid = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash);
                }
                catch (BCrypt.Net.SaltParseException)
                {
                    return new ChangePasswordResponse
                    {
                        Success = false,
                        Message = "Current password format is outdated. Please contact support."
                    };
                }

                if (!currentPasswordValid)
                {
                    return new ChangePasswordResponse
                    {
                        Success = false,
                        Message = "Current password is incorrect"
                    };
                }

                // Update password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                await _context.SaveChangesAsync();

                return new ChangePasswordResponse
                {
                    Success = true,
                    Message = "Password changed successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return new ChangePasswordResponse
                {
                    Success = false,
                    Message = "Error changing password"
                };
            }
        }

        public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

                if (user == null)
                {
                    return new ForgotPasswordResponse
                    {
                        Success = false,
                        Message = "User not found with this email"
                    };
                }

                // Generate temporary password
                var tempPassword = GenerateTemporaryPassword();
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);
                await _context.SaveChangesAsync();

                return new ForgotPasswordResponse
                {
                    Success = true,
                    Message = "Temporary password generated successfully",
                    TemporaryPassword = tempPassword // In production, send via email
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating temporary password");
                return new ForgotPasswordResponse
                {
                    Success = false,
                    Message = "Error generating temporary password"
                };
            }
        }

        public async Task<UpdateProfileResponse> UpdateProfileAsync(UpdateProfileRequest request)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

                if (user == null)
                {
                    return new UpdateProfileResponse
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                // Update profile (users cannot change database settings)
                user.CompanyName = request.CompanyName;
                user.ContactPerson = request.ContactPerson;
                user.CellNo = request.CellNo;
                await _context.SaveChangesAsync();

                return new UpdateProfileResponse
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
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile");
                return new UpdateProfileResponse
                {
                    Success = false,
                    Message = "Error updating profile"
                };
            }
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "POS-API";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "POS-Client";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.GivenName, user.CompanyName),
                new Claim("ClientName", user.CompanyName)
            };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateTemporaryPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            try
            {
                return await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by ID: {UserId}", userId);
                return null;
            }
        }

        public async Task<User?> GetActiveUserAsync(int userId)
        {
            try
            {
                return await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active user by ID: {UserId}", userId);
                return null;
            }
        }

        public async Task<bool> UpdateUserConnectionAsync(int userId, string serverName, string databaseName, 
            string databasePassword, int port, DatabaseType databaseType, string? filePath = null)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                if (user == null)
                    return false;

                user.ServerName = serverName;
                user.DatabaseName = databaseName;
                user.DatabasePassword = databasePassword;
                user.Port = port;
                user.DatabaseType = databaseType;
                user.FilePath = filePath;
                user.LastError = null; // Clear any previous errors

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user connection for user ID: {UserId}", userId);
                return false;
            }
        }
    }
}
