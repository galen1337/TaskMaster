using Domain.Enums;

namespace Domain.Entities;

public class BoardMember
{
    public int BoardId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public BoardRole Role { get; set; } = BoardRole.Member;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Board Board { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
} 