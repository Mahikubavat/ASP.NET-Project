using Microsoft.AspNetCore.Identity;

namespace BlogPlatform.Models
{
	public class ApplicationUser : IdentityUser
	{
		public string? Bio { get; set; }
		public string? AvatarUrl { get; set; }
		// new: preferred property for profile picture
		public string? ProfilePictureUrl { get; set; }
		// separate social link properties
		public string? TwitterUrl { get; set; }
		public string? LinkedInUrl { get; set; }
		public string? GitHubUrl { get; set; }
		public string? SocialLinks { get; set; }

		// Notification preferences
		public bool NotifyOnLike { get; set; } = true;
		public bool NotifyOnComment { get; set; } = true;

		// navigation properties
		public ICollection<Post>? Posts { get; set; }
		public ICollection<Comment>? Comments { get; set; }
		public ICollection<Like>? Likes { get; set; }
	}
}