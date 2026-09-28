using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Models;

public class Notification
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = null!;
    public ApplicationUser? User { get; set; }

    public string? TriggeredByUserId { get; set; }
    public ApplicationUser? TriggeredByUser { get; set; }

    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = null!; // "Like", "Comment", "Reply"

    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = null!;

    [MaxLength(500)]
    public string? Url { get; set; }

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
