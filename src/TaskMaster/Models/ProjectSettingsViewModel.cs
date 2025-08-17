using System.ComponentModel.DataAnnotations;
using Domain.Common;

namespace TaskMaster.Models;

public class ProjectSettingsViewModel
{
	public int Id { get; set; }

	[Required]
	[StringLength(ValidationConstants.ProjectNameMaxLength, MinimumLength = ValidationConstants.ProjectNameMinLength)]
	[Display(Name = "Project Name")]
	public string Name { get; set; } = string.Empty;

	[Required]
	[StringLength(ValidationConstants.ProjectDescriptionMaxLength)]
	[Display(Name = "Description")]
	public string Description { get; set; } = string.Empty;
} 