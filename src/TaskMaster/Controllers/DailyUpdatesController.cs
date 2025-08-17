using System.Security.Claims;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TaskMaster.Controllers;

[Authorize]
public class DailyUpdatesController : Controller
{
	private readonly ApplicationDbContext _db;
	private readonly UserManager<ApplicationUser> _userManager;

	public DailyUpdatesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
	{
		_db = db;
		_userManager = userManager;
	}

	// GET: /DailyUpdates/Project/5
	[HttpGet]
	public async Task<IActionResult> Project(int id, int page = 1, int pageSize = 20)
	{
		string? userId = _userManager.GetUserId(User);
		if (string.IsNullOrEmpty(userId)) return Unauthorized();

		bool isMember = await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == id && pm.UserId == userId);
		bool isPlatformAdmin = User.IsInRole("Admin");
		if (!isMember && !isPlatformAdmin) return Forbid();

		var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id);
		if (project == null) return NotFound();

		var updatesQuery = _db.DailyUpdates
			.Where(u => u.ProjectId == id)
			.Include(u => u.Author)
			.OrderByDescending(u => u.CreatedAt);

		int total = await updatesQuery.CountAsync();
		var updates = await updatesQuery
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync();

		ViewBag.Project = project;
		ViewBag.ProjectId = id;
		ViewBag.CurrentPage = page;
		ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);

		return View("Project", updates);
	}

	// POST: /DailyUpdates/Create
	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Create(int projectId, string content)
	{
		string? userId = _userManager.GetUserId(User);
		if (string.IsNullOrEmpty(userId)) return Unauthorized();

		bool isMember = await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
		bool isPlatformAdmin = User.IsInRole("Admin");
		if (!isMember && !isPlatformAdmin) return Forbid();

		if (string.IsNullOrWhiteSpace(content))
		{
			TempData["Error"] = "Update content cannot be empty.";
			return RedirectToAction(nameof(Project), new { id = projectId });
		}
		if (content.Length > 2000)
		{
			TempData["Error"] = "Update is too long (max 2000 characters).";
			return RedirectToAction(nameof(Project), new { id = projectId });
		}

		_db.DailyUpdates.Add(new DailyUpdate
		{
			ProjectId = projectId,
			AuthorId = userId,
			Content = content.Trim(),
			CreatedAt = DateTime.UtcNow
		});
		await _db.SaveChangesAsync();
		TempData["Success"] = "Daily update posted.";
		return RedirectToAction(nameof(Project), new { id = projectId });
	}
} 