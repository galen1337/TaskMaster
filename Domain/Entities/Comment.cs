namespace Domain.Entities;

public class Comment
{
    public int Id { get; set; }
    public int CardId { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Card Card { get; set; } = null!;
    public ApplicationUser Author { get; set; } = null!;
} 