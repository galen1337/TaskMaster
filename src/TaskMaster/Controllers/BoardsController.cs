using System.Security.Claims;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Domain.Enums;

namespace TaskMaster.Controllers;

[Authorize]
public class BoardsController : Controller
{
	private readonly IBoardService _boardService;
	private readonly ApplicationDbContext _db;

	public BoardsController(IBoardService boardService, ApplicationDbContext db)
	{
		_boardService = boardService;
		_db = db;
	}

	public async Task<IActionResult> Details(int id)
	{
		string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (string.IsNullOrEmpty(userId)) return Challenge();

		bool isAdmin = User.IsInRole("Admin");
		var board = await _boardService.GetBoardDetailsAsync(id, userId, isAdmin);
		if (board == null) return Forbid();

		return View(board);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Create(int projectId, CreateBoardDto dto)
	{
		string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (string.IsNullOrEmpty(userId)) return Challenge();

		try
		{
			bool isAdmin = User.IsInRole("Admin");
			var board = await _boardService.CreateBoardAsync(projectId, userId, isAdmin, dto);
			TempData["Success"] = "Board created.";
			return RedirectToAction("Details", "Projects", new { id = board.ProjectId });
		}
		catch (UnauthorizedAccessException)
		{
			TempData["Error"] = "You are not allowed to create a board for this project.";
		}
		catch (ArgumentException ex)
		{
			TempData["Error"] = ex.Message;
		}
		catch (Exception)
		{
			TempData["Error"] = "Failed to create board.";
		}

		return RedirectToAction("Details", "Projects", new { id = projectId });
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Delete(int id)
	{
		string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (string.IsNullOrEmpty(userId)) return Challenge();

		var board = await _db.Boards.FirstOrDefaultAsync(b => b.Id == id);
		if (board == null)
		{
			TempData["Error"] = "Board not found.";
			return RedirectToAction("Index", "Projects");
		}
		int projectId = board.ProjectId;

		bool isPlatformAdmin = User.IsInRole("Admin");
		bool canManageProject = isPlatformAdmin || await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId && (pm.Role == ProjectRole.Owner || pm.Role == ProjectRole.Admin));
		if (!canManageProject)
		{
			TempData["Error"] = "You are not allowed to delete this board.";
			return RedirectToAction("Details", "Projects", new { id = projectId });
		}

		try
		{
			_db.Boards.Remove(board);
			await _db.SaveChangesAsync();
			TempData["Success"] = "Board deleted.";
		}
		catch
		{
			TempData["Error"] = "Failed to delete board.";
		}
		return RedirectToAction("Details", "Projects", new { id = projectId });
	}
} 