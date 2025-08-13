using Domain.Enums;

namespace Domain.Entities;

public class ProjectMember
{
    public int ProjectId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ProjectRole Role { get; set; } = ProjectRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Project Project { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
} 