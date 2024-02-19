using System.ComponentModel.DataAnnotations;

namespace Mango.Web.Models
{
	public class LoginRequestDto
	{
        [Required]
        [Display(Name = "User Name")]
        public string UserName { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
