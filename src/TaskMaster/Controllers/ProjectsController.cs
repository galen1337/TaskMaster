using System.Security.Claims;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskMaster.Models;
using Application.Services;
using Domain.Enums;

namespace TaskMaster.Controllers;

[Authorize]
public class ProjectsController : Controller
{
	private readonly ApplicationDbContext _context;
	private readonly UserManager<ApplicationUser> _userManager;
	private readonly IProjectService _projectService;

	public ProjectsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IProjectService projectService)
	{
		_context = context;
		_userManager = userManager;
		_projectService = projectService;
	}

	public async Task<IActionResult> Index()
	{
		string? userId = _userManager.GetUserId(User);
		if (string.IsNullOrEmpty(userId)) return Unauthorized();

		var projects = await _context.ProjectMembers
			.Where(pm => pm.UserId == userId)
			.Select(pm => pm.Project)
			.OrderByDescending(p => p.CreatedAt)
			.ToListAsync();

		return View(projects);
	}

	public async Task<IActionResult> Details(int id)
	{
		string? userId = _userManager.GetUserId(User);
		if (string.IsNullOrEmpty(userId)) return Unauthorized();

		bool isMember = await _context.ProjectMembers.AnyAsync(pm => pm.ProjectId == id && pm.UserId == userId);
		bool isPlatformAdmin = User.IsInRole("Admin");
		if (!isMember && !isPlatformAdmin) return Forbid();

		var project = await _context.Projects
			.Include(p => p.Boards)
			.Include(p => p.Members)
				.ThenInclude(m => m.User)
			.FirstOrDefaultAsync(p => p.Id == id);
		if (project == null) return NotFound();

		var myRole = await _context.ProjectMembers.Where(pm => pm.ProjectId == id && pm.UserId == userId).Select(pm => pm.Role).FirstOrDefaultAsync();
		ViewBag.CanManageProject = isPlatformAdmin || myRole == ProjectRole.Owner || myRole == ProjectRole.Admin;
		ViewBag.CurrentUserId = userId;

		return View(project);
	}

	public IActionResult Create()
	{
		return View(new CreateProjectViewModel());
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Create(CreateProjectViewModel model)
	{
		string? userId = _userManager.GetUserId(User);
		if (string.IsNullOrEmpty(userId)) return Unauthorized();

		if (!ModelState.IsValid)
		{
			// Add debugging information
			ViewBag.Debug = $"ModelState invalid. Errors: {string.Join(", ", ModelState.SelectMany(x => x.Value.Errors.Select(e => $"{x.Key}: {e.ErrorMessage}")))}";
			return View(model);
		}

		try
		{
			// Create project entity from view model
			var project = new Project
			{
				Name = model.Name,
				Description = model.Description,
				Key = GenerateProjectKey(model.Name),
				OwnerId = userId,
				CreatedAt = DateTime.UtcNow
			};

			_context.Projects.Add(project);
			await _context.SaveChangesAsync();

			// Add the creator as the project owner
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
		catch (Exception ex)
		{
			ModelState.AddModelError("", $"Error creating project: {ex.Message}");
			ViewBag.Debug = $"Exception: {ex}";
			return View(model);
		}
	}

	private static string GenerateProjectKey(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
			return "PROJ";

		// Extract letters and convert to uppercase
		var letters = new string(name.Where(char.IsLetter).ToArray()).ToUpperInvariant();
		
		// If we have enough letters, take the first 4
		if (letters.Length >= 4) 
			return letters[..4];
		
		// If we have some letters but not enough, pad with 'X'
		if (letters.Length > 0)
			return letters.PadRight(4, 'X');
		
		// If no letters at all, use default
		return "PROJ";
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> ChangeRole(int projectId, string userId, ProjectRole role)
	{
		string? actingUserId = _userManager.GetUserId(User);
		if (string.IsNullOrEmpty(actingUserId)) return Challenge();
		try
		{
			bool isAdmin = User.IsInRole("Admin");
			await _projectService.ChangeMemberRoleAsync(projectId, userId, role, actingUserId, isAdmin);
			TempData["Success"] = "Role updated.";
		}
		catch (Exception ex)
		{
			TempData["Error"] = ex.Message;
		}
		return RedirectToAction(nameof(Details), new { id = projectId });
	}

	[HttpGet]
	public async Task<IActionResult> UserList()
	{
		string? userId = _userManager.GetUserId(User);
		if (string.IsNullOrEmpty(userId)) return Unauthorized();
		var items = await _projectService.GetUserProjectsAsync(userId);
		return Json(items.Select(p => new { id = p.Id, name = p.Name }));
	}
} 