using Microsoft.AspNetCore.Identity;

namespace Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<ProjectMember> ProjectMemberships { get; set; } = new List<ProjectMember>();
    public ICollection<BoardMember> BoardMemberships { get; set; } = new List<BoardMember>();
    public ICollection<Card> AssignedCards { get; set; } = new List<Card>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Project> OwnedProjects { get; set; } = new List<Project>();
    public ICollection<Invite> SentInvites { get; set; } = new List<Invite>();
} 