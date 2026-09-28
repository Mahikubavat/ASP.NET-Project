using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.ViewModels
{
    public class PostCreateEditViewModel
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = null!;

        public string? Slug { get; set; }

        [Required]
        public string Content { get; set; } = null!;

        public IFormFile? FeaturedImage { get; set; }
        public string? ExistingFeaturedImageUrl { get; set; }

        public int? CategoryId { get; set; }
        public bool IsPublished { get; set; } = true;

        public List<int>? SelectedTagIds { get; set; }
        public string? TagsCommaSeparated { get; set; }
    }
}
