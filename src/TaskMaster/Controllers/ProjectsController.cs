using System.Security.Claims;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TaskMaster.Controllers;

[Authorize]
public class ProjectsController : Controller
{
	private readonly ApplicationDbContext _context;

	public ProjectsController(ApplicationDbContext context)
	{
		_context = context;
	}

	public async Task<IActionResult> Index()
	{
		string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (string.IsNullOrEmpty(userId)) return Challenge();

		var projects = await _context.ProjectMembers
			.Where(pm => pm.UserId == userId)
			.Select(pm => pm.Project)
			.OrderByDescending(p => p.CreatedAt)
			.ToListAsync();

		return View(projects);
	}

	public async Task<IActionResult> Details(int id)
	{
		string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (string.IsNullOrEmpty(userId)) return Challenge();

		bool isMember = await _context.ProjectMembers.AnyAsync(pm => pm.ProjectId == id && pm.UserId == userId);
		bool isAdmin = User.IsInRole("Admin");
		if (!isMember && !isAdmin) return Forbid();

		var project = await _context.Projects
			.Include(p => p.Boards)
			.Include(p => p.Members)
				.ThenInclude(m => m.User)
			.FirstOrDefaultAsync(p => p.Id == id);
		if (project == null) return NotFound();

		return View(project);
	}

	public IActionResult Create()
	{
		return View(new Project());
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Create(Project project)
	{
		string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (string.IsNullOrEmpty(userId)) return Challenge();

		if (string.IsNullOrWhiteSpace(project.Name))
		{
			ModelState.AddModelError("Name", "Name is required");
		}

		if (!ModelState.IsValid)
		{
			return View(project);
		}

		if (string.IsNullOrWhiteSpace(project.Key))
		{
			project.Key = GenerateProjectKey(project.Name);
		}

		project.OwnerId = userId;
		project.CreatedAt = DateTime.UtcNow;

		_context.Projects.Add(project);
		await _context.SaveChangesAsync();

		_context.ProjectMembers.Add(new ProjectMember
		{
			ProjectId = project.Id,
			UserId = userId,
			Role = Domain.Enums.ProjectRole.Owner,
			JoinedAt = DateTime.UtcNow
		});
		await _context.SaveChangesAsync();

		return RedirectToAction(nameof(Details), new { id = project.Id });
	}

	private static string GenerateProjectKey(string name)
	{
		var letters = new string(name.Where(char.IsLetter).ToArray()).ToUpperInvariant();
		if (letters.Length >= 4) return letters[..4];
		return letters.PadRight(3, 'X');
	}
} 