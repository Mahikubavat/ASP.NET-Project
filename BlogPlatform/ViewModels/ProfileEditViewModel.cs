using Microsoft.AspNetCore.Http;

namespace BlogPlatform.ViewModels
{
    public class ProfileEditViewModel
    {
        public string? Bio { get; set; }
        public IFormFile? ProfilePicture { get; set; }
        public string? ProfilePictureUrl { get; set; }

        public string? TwitterUrl { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? GitHubUrl { get; set; }

        // Notification preferences
        public bool NotifyOnLike { get; set; } = true;
        public bool NotifyOnComment { get; set; } = true;
    }
}
