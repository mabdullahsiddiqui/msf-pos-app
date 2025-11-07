using System.Text.Json; 
using System.Net.Http.Json;
using Microsoft.JSInterop;
using pos_app.Client.Models;

namespace pos_app.Client.Services
{
	public class AuthService
	{
		private readonly HttpClient _httpClient;
		private readonly ILogger<AuthService> _logger;
		private readonly IJSRuntime _jsRuntime;

		public AuthService(HttpClient httpClient, ILogger<AuthService> logger, IJSRuntime jsRuntime)
		{
			_httpClient = httpClient;
			_logger = logger;
			_jsRuntime = jsRuntime;
		}

		public async Task<LoginResponse> LoginAsync(LoginRequest request)
		{
			try
			{
				var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
				if (response.IsSuccessStatusCode)
				{
					var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
					if (result != null && !string.IsNullOrEmpty(result.Token))
					{
						await StoreTokenAsync(result.Token);
						return result;
					}
				}
				
				var errorResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
				return errorResponse ?? new LoginResponse 
				{ 
					Success = false, 
					Message = "Login failed" 
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error during login");
				return new LoginResponse 
				{ 
					Success = false, 
					Message = "Login error occurred" 
				};
			}
		}

		public async Task<SignupResponse> SignupAsync(SignupViewModel request)
		{
			try
			{
				var signupRequest = new
				{
					CompanyName = request.CompanyName,
					Email = request.Email,
					Password = request.Password,
					ContactPerson = request.ContactPerson,
					CellNo = request.CellNo,
					DatabaseName = request.DatabaseName,
					ServerName = request.ServerName,
					Username = request.Username,
					DatabasePassword = request.DatabasePassword,
					Port = request.Port,
					ConnectionName = request.ConnectionName
				};
				var response = await _httpClient.PostAsJsonAsync("api/auth/signup", signupRequest);
				if (response.IsSuccessStatusCode)
				{
					var result = await response.Content.ReadFromJsonAsync<SignupResponse>();
					if (result != null && result.Success && !string.IsNullOrEmpty(result.Token))
					{
						await StoreTokenAsync(result.Token);
						return result;
					}
				}
				
				var errorResponse = await response.Content.ReadFromJsonAsync<SignupResponse>();
				return errorResponse ?? new SignupResponse 
				{ 
					Success = false, 
					Message = "Signup failed" 
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error during signup");
				return new SignupResponse 
				{ 
					Success = false, 
					Message = "Signup error occurred" 
				};
			}
		}

		public async Task<ChangePasswordResponse> ChangePasswordAsync(ChangePasswordRequest request)
		{
			try
			{
				var token = await GetTokenAsync();
				if (!string.IsNullOrEmpty(token))
				{
					_httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
				}

				var response = await _httpClient.PostAsJsonAsync("api/auth/change-password", request);
				if (response.IsSuccessStatusCode)
				{
					var result = await response.Content.ReadFromJsonAsync<ChangePasswordResponse>();
					return result ?? new ChangePasswordResponse 
					{ 
						Success = false, 
						Message = "Password change failed" 
					};
				}
				
				var errorResponse = await response.Content.ReadFromJsonAsync<ChangePasswordResponse>();
				return errorResponse ?? new ChangePasswordResponse 
				{ 
					Success = false, 
					Message = "Password change failed" 
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error during password change");
				return new ChangePasswordResponse 
				{ 
					Success = false, 
					Message = "Password change error occurred" 
				};
			}
		}

		public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
		{
			try
			{
				var response = await _httpClient.PostAsJsonAsync("api/auth/user/forgot-password", request);
				if (response.IsSuccessStatusCode)
				{
					var result = await response.Content.ReadFromJsonAsync<ForgotPasswordResponse>();
					return result ?? new ForgotPasswordResponse 
					{ 
						Success = false, 
						Message = "Forgot password failed" 
					};
				}
				
				var errorResponse = await response.Content.ReadFromJsonAsync<ForgotPasswordResponse>();
				return errorResponse ?? new ForgotPasswordResponse 
				{ 
					Success = false, 
					Message = "Forgot password failed" 
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error during forgot password");
				return new ForgotPasswordResponse 
				{ 
					Success = false, 
					Message = "Forgot password error occurred" 
				};
			}
		}

		public async Task<UpdateProfileResponse> UpdateProfileAsync(UpdateProfileRequest request)
		{
			try
			{
				var token = await GetTokenAsync();
				if (!string.IsNullOrEmpty(token))
				{
					_httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
				}

				var response = await _httpClient.PostAsJsonAsync("api/auth/update-profile", request);
				if (response.IsSuccessStatusCode)
				{
					var result = await response.Content.ReadFromJsonAsync<UpdateProfileResponse>();
					return result ?? new UpdateProfileResponse 
					{ 
						Success = false, 
						Message = "Profile update failed" 
					};
				}
				
				var errorResponse = await response.Content.ReadFromJsonAsync<UpdateProfileResponse>();
				return errorResponse ?? new UpdateProfileResponse 
				{ 
					Success = false, 
					Message = "Profile update failed" 
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error during profile update");
				return new UpdateProfileResponse 
				{ 
					Success = false, 
					Message = "Profile update error occurred" 
				};
			}
		}

		public async Task LogoutAsync()
		{
			try
			{
				await _httpClient.PostAsync("api/auth/logout", null);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error during logout API call");
			}
			finally
			{
				await RemoveTokenAsync();
			}
		}

		public async Task<string?> GetTokenAsync()
		{
			try
			{
				return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "auth_token");
			}
			catch
			{
				return null;
			}
		}

		public async Task<bool> IsAuthenticatedAsync()
		{
			var token = await GetTokenAsync();
			return !string.IsNullOrEmpty(token);
		}

		private async Task StoreTokenAsync(string token)
		{
			try
			{
				await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "auth_token", token);
			}
			catch { }
		}

		private async Task RemoveTokenAsync()
		{
			try
			{
				await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "auth_token");
			}
			catch { }
		}

		// User Forgot Password Flow Methods
		public async Task<UserForgotPasswordResponse> UserForgotPasswordAsync(UserForgotPasswordRequest request)
		{
			try
			{
				var response = await _httpClient.PostAsJsonAsync("api/auth/user/forgot-password", request);
				if (response.IsSuccessStatusCode)
				{
					var result = await response.Content.ReadFromJsonAsync<UserForgotPasswordResponse>();
					return result ?? new UserForgotPasswordResponse 
					{ 
						Success = false,
						Message = "Failed to process request"
					};
				}
				
				var errorResponse = await response.Content.ReadFromJsonAsync<UserForgotPasswordResponse>();
				return errorResponse ?? new UserForgotPasswordResponse 
				{ 
					Success = false, 
					Message = "Forgot password failed" 
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error during user forgot password");
				return new UserForgotPasswordResponse 
				{ 
					Success = false, 
					Message = "Forgot password error occurred" 
				};
			}
		}

		public async Task<UserVerifyCodeResponse> VerifyCodeAsync(UserVerifyCodeRequest request)
		{
			try
			{
				var response = await _httpClient.PostAsJsonAsync("api/auth/user/verify-code", request);

				var responseContent = await response.Content.ReadAsStringAsync();
				var result = JsonSerializer.Deserialize<UserVerifyCodeResponse>(responseContent, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});

				return result ?? new UserVerifyCodeResponse { Success = false, Message = "Invalid response from server" };
			}
			catch (Exception ex)
			{
				return new UserVerifyCodeResponse { Success = false, Message = $"Error: {ex.Message}" };
			}
		}

		public async Task<UserResetPasswordResponse> ResetPasswordAsync(UserResetPasswordRequest request)
		{
			try
			{
				var response = await _httpClient.PostAsJsonAsync("api/auth/user/reset-password", request);

				var responseContent = await response.Content.ReadAsStringAsync();
				var result = JsonSerializer.Deserialize<UserResetPasswordResponse>(responseContent, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});

				return result ?? new UserResetPasswordResponse { Success = false, Message = "Invalid response from server" };
			}
			catch (Exception ex)
			{
				return new UserResetPasswordResponse { Success = false, Message = $"Error: {ex.Message}" };
			}
		}

		public async Task<UserResendCodeResponse> ResendCodeAsync(UserResendCodeRequest request)
		{
			try
			{
				var response = await _httpClient.PostAsJsonAsync("api/auth/user/resend-code", request);

				var responseContent = await response.Content.ReadAsStringAsync();
				var result = JsonSerializer.Deserialize<UserResendCodeResponse>(responseContent, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});

				return result ?? new UserResendCodeResponse { Success = false, Message = "Invalid response from server" };
			}
			catch (Exception ex)
			{
				return new UserResendCodeResponse { Success = false, Message = $"Error: {ex.Message}" };
			}
		}

		// User Profile Management Methods
		public async Task<UserProfileResponse> GetProfileAsync()
		{
			try
			{
				var token = await GetTokenAsync();
				if (string.IsNullOrEmpty(token))
				{
					return new UserProfileResponse { Success = false, Message = "No authentication token found" };
				}

				_httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
				var response = await _httpClient.GetAsync("api/auth/profile");

				var responseContent = await response.Content.ReadAsStringAsync();
				var result = JsonSerializer.Deserialize<UserProfileResponse>(responseContent, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});

				return result ?? new UserProfileResponse { Success = false, Message = "Invalid response from server" };
			}
			catch (Exception ex)
			{
				return new UserProfileResponse { Success = false, Message = $"Error: {ex.Message}" };
			}
		}

		public async Task<UserUpdateProfileResponse> UpdateUserProfileAsync(UserUpdateProfileRequest request)
		{
			try
			{
				var token = await GetTokenAsync();
				if (string.IsNullOrEmpty(token))
				{
					return new UserUpdateProfileResponse { Success = false, Message = "No authentication token found" };
				}

				_httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
				var response = await _httpClient.PutAsJsonAsync("api/auth/profile", request);

				var responseContent = await response.Content.ReadAsStringAsync();
				var result = JsonSerializer.Deserialize<UserUpdateProfileResponse>(responseContent, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});

				return result ?? new UserUpdateProfileResponse { Success = false, Message = "Invalid response from server" };
			}
			catch (Exception ex)
			{
				return new UserUpdateProfileResponse { Success = false, Message = $"Error: {ex.Message}" };
			}
		}

		public async Task<UserChangePasswordResponse> ChangeUserPasswordAsync(UserChangePasswordRequest request)
		{
			try
			{
				Console.WriteLine($"AuthService: ChangeUserPasswordAsync called with Username: {request.Username}");
				var token = await GetTokenAsync();
				if (string.IsNullOrEmpty(token))
				{
					Console.WriteLine("AuthService: No authentication token found");
					return new UserChangePasswordResponse { Success = false, Message = "No authentication token found" };
				}

				Console.WriteLine($"AuthService: Token found, calling API endpoint: api/auth/user/change-password");
				_httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
				var response = await _httpClient.PostAsJsonAsync("api/auth/user/change-password", request);

				Console.WriteLine($"AuthService: API response status: {response.StatusCode}");
				var responseContent = await response.Content.ReadAsStringAsync();
				Console.WriteLine($"AuthService: API response content: {responseContent}");
				
				var result = JsonSerializer.Deserialize<UserChangePasswordResponse>(responseContent, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});

				Console.WriteLine($"AuthService: Deserialized result - Success: {result?.Success}, Message: {result?.Message}");
				return result ?? new UserChangePasswordResponse { Success = false, Message = "Invalid response from server" };
			}
			catch (Exception ex)
			{
				Console.WriteLine($"AuthService: Exception in ChangeUserPasswordAsync: {ex.Message}");
				Console.WriteLine($"AuthService: Exception stack trace: {ex.StackTrace}");
				return new UserChangePasswordResponse { Success = false, Message = $"Error: {ex.Message}" };
			}
		}

		public async Task<UserChangePasswordResponse> ChangePasswordAsync(UserChangePasswordRequest request)
		{
			try
			{
				var token = await GetTokenAsync();
				if (string.IsNullOrEmpty(token))
				{
					return new UserChangePasswordResponse { Success = false, Message = "No authentication token found" };
				}

				_httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
				var response = await _httpClient.PostAsJsonAsync("api/auth/user/change-password", request);

				var responseContent = await response.Content.ReadAsStringAsync();
				var result = JsonSerializer.Deserialize<UserChangePasswordResponse>(responseContent, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});

				return result ?? new UserChangePasswordResponse { Success = false, Message = "Invalid response from server" };
			}
			catch (Exception ex)
			{
				return new UserChangePasswordResponse { Success = false, Message = $"Error: {ex.Message}" };
			}
		}
	}
}
