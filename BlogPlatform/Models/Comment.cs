using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Models;

public class Comment
{
    public int Id { get; set; }

    [Required]
    public string Content { get; set; } = null!;

    public int PostId { get; set; }
    public Post? Post { get; set; }

    public string? AuthorId { get; set; }
    public ApplicationUser? Author { get; set; }

    public int? ParentCommentId { get; set; }
    public Comment? ParentComment { get; set; }
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsApproved { get; set; } = true;
}
