using System.ComponentModel.DataAnnotations;
using Domain.Common;

namespace Domain.Entities;

public class DailyUpdate
{
	public int Id { get; set; }
	
	[Required]
	public int ProjectId { get; set; }
	
	[Required]
	public string AuthorId { get; set; } = string.Empty;
	
	[Required]
	[StringLength(ValidationConstants.DailyUpdateContentMaxLength)]
	public string Content { get; set; } = string.Empty;
	
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	
	// Navigation
	public Project Project { get; set; } = null!;
	public ApplicationUser Author { get; set; } = null!;
} 