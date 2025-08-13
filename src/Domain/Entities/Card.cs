using Domain.Enums;

namespace Domain.Entities;

public class Card
{
    public int Id { get; set; }
    public int BoardId { get; set; }
    public int ColumnId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Priority Priority { get; set; } = Priority.Medium;
    public string? AssigneeId { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    
    // Navigation properties
    public Board Board { get; set; } = null!;
    public Column Column { get; set; } = null!;
    public ApplicationUser? Assignee { get; set; }
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<CardLabel> CardLabels { get; set; } = new List<CardLabel>();
} 