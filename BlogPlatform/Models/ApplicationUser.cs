using Microsoft.AspNetCore.Identity;

namespace BlogPlatform.Models
{
	public class ApplicationUser : IdentityUser
	{
		public string? Bio { get; set; }
		public string? AvatarUrl { get; set; }
		public string? SocialLinks { get; set; }
	}
}