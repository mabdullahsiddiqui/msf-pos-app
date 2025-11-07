using System.ComponentModel.DataAnnotations;

namespace pos_app.Client.Models
{
	public class LoginViewModel
	{
		[Required(ErrorMessage = "Email is required")]
		[EmailAddress(ErrorMessage = "Please enter a valid email address")]
		public string Email { get; set; } = string.Empty;

		[Required(ErrorMessage = "Password is required")]
		[StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
		public string Password { get; set; } = string.Empty;
	}

	public class SignupViewModel
	{
		[Required(ErrorMessage = "Company Name is required")]
		[RegularExpression("[A-Za-z0-9]{4,}", ErrorMessage = "Please enter a valid Company Name")]
		public string CompanyName { get; set; } = string.Empty;

		[Required(ErrorMessage = "Email is required")]
		[EmailAddress(ErrorMessage = "Please enter a valid email address")]
		public string Email { get; set; } = string.Empty;

		[Required(ErrorMessage = "Password is required")]
		[StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
		public string Password { get; set; } = string.Empty;

		[Required(ErrorMessage = "Confirm Password is required")]
		[Compare("Password", ErrorMessage = "Passwords do not match")]
		public string ConfirmPassword { get; set; } = string.Empty;

		[Required(ErrorMessage = "Contact Person is required")]
		public string ContactPerson { get; set; } = string.Empty;

		[Required(ErrorMessage = "Cell Number is required")]
		[RegularExpression("[0-9]{10,15}", ErrorMessage = "Please enter a valid phone number (10-15 digits)")]
		public string CellNo { get; set; } = string.Empty;

		// Database Connection Information
		[Required(ErrorMessage = "Database Name is required")]
		[StringLength(100, ErrorMessage = "Database name cannot exceed 100 characters")]
		public string DatabaseName { get; set; } = string.Empty;

		[Required(ErrorMessage = "Server Name is required")]
		[StringLength(100, ErrorMessage = "Server name cannot exceed 100 characters")]
		public string ServerName { get; set; } = string.Empty;

		[StringLength(100, ErrorMessage = "Username cannot exceed 100 characters")]
		public string? Username { get; set; }

		[StringLength(100, ErrorMessage = "Database password cannot exceed 100 characters")]
		public string? DatabasePassword { get; set; }

		public int? Port { get; set; } = 1433;

		[StringLength(100, ErrorMessage = "Connection name cannot exceed 100 characters")]
		public string ConnectionName { get; set; } = string.Empty;
	}
}
