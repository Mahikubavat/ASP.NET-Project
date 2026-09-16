using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Models;

public class Category
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [MaxLength(100)]
    public string? Slug { get; set; }

    public ICollection<Post>? Posts { get; set; }
}
