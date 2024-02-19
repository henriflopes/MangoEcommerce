using System.ComponentModel.DataAnnotations;

namespace Mango.Web.Models
{
	public class RegistrationRequestDto
	{
        [Required]
        public string Email { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        [Display( Name = "Phone Number")]
        public string PhoneNumber { get; set; }
        [Required]
        public string Password { get; set; }
		public string? Role { get; set; }
	}
}
