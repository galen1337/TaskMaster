using System.ComponentModel.DataAnnotations;

namespace TaskMaster.Models;

public class CreateProjectViewModel
{
    [Required]
    [Display(Name = "Project Name")]
    public string Name { get; set; } = string.Empty;
    
    [Display(Name = "Description")]
    public string? Description { get; set; }
} 