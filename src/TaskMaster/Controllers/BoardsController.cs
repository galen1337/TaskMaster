using System.Security.Claims;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TaskMaster.Controllers;

[Authorize]
public class BoardsController : Controller
{
	private readonly IBoardService _boardService;

	public BoardsController(IBoardService boardService)
	{
		_boardService = boardService;
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
} 