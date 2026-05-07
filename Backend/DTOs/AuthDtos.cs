using System.ComponentModel.DataAnnotations;

namespace HeimdallBackend.DTOs
{
	public class RegisterDto
	{
		[Required]
		public string Username { get; set; }

		[Required]
		[EmailAddress]
		public string Email { get; set; }

		[Required]
		[MinLength(6)]
		public string Password { get; set; }

		public int RoleID { get; set; }
    }

	public class LoginDto
	{
		[Required]
		public string Email { get; set; }

		[Required]
		public string Password { get; set; }
	}
}