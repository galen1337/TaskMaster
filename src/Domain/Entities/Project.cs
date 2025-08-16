using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class Project
{
    public int Id { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ApplicationUser Owner { get; set; } = null!;
    public ICollection<Board> Boards { get; set; } = new List<Board>();
    public ICollection<Label> Labels { get; set; } = new List<Label>();
    public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
    public ICollection<Invite> Invites { get; set; } = new List<Invite>();
} 