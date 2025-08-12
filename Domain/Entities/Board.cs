namespace Domain.Entities;

public class Board
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsPrivate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Project Project { get; set; } = null!;
    public ICollection<Column> Columns { get; set; } = new List<Column>();
    public ICollection<Card> Cards { get; set; } = new List<Card>();
    public ICollection<BoardMember> Members { get; set; } = new List<BoardMember>();
} 