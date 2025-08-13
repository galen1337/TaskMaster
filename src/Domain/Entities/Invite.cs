using Domain.Enums;

namespace Domain.Entities;

public class Invite
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string InvitedEmail { get; set; } = string.Empty;
    public string InvitedByUserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public InviteStatus Status { get; set; } = InviteStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Project Project { get; set; } = null!;
    public ApplicationUser InvitedBy { get; set; } = null!;
} 