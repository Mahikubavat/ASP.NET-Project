namespace BlogPlatform.Models;

public class Like
{
    // composite key of PostId + UserId configured in ApplicationDbContext
    public int PostId { get; set; }
    public Post? Post { get; set; }

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
