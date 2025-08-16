using System.Security.Claims;
using Application.Services;
using Domain.Enums;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TaskMaster.Controllers;

[Authorize]
public class CardsController : Controller
{
	private readonly ICardService _cardService;
	private readonly IBoardService _boardService;

	public CardsController(ICardService cardService, IBoardService boardService)
	{
		_cardService = cardService;
		_boardService = boardService;
	}

	[HttpGet]
	public async Task<IActionResult> CreateForm(int boardId)
	{
		string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (string.IsNullOrEmpty(userId)) return Challenge();
		bool isAdmin = User.IsInRole("Admin");
		var board = await _boardService.GetBoardDetailsAsync(boardId, userId, isAdmin);
		if (board == null) return Forbid();
		var assignees = board.Project.Members.Select(m => new UserOption(m.UserId, m.User.Email)).ToList();
		return PartialView("~/Views/Cards/_CardCreate.cshtml", (board.Id, board.Columns.ToList(), assignees));
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Assign(int id, string? userId)
	{
		string? currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (string.IsNullOrEmpty(currentUserId)) return Challenge();

		try
		{
			bool isAdmin = User.IsInRole("Admin");
			int boardId = await _cardService.AssignAsync(id, userId ?? string.Empty, currentUserId, isAdmin);
			if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Ok(new { reload = true });
			TempData["Success"] = "Card assignment updated.";
			return RedirectToAction("Details", "Boards", new { id = boardId });
		}
		catch (UnauthorizedAccessException)
		{
			if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Forbid();
			TempData["Error"] = "You are not allowed to assign this card.";
		}
		catch (ArgumentException ex)
		{
			if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return BadRequest(new { error = ex.Message });
			TempData["Error"] = ex.Message;
		}
		catch (Exception)
		{
			if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return StatusCode(500, new { error = "Failed to assign card." });
			TempData["Error"] = "Failed to assign card.";
		}

		return Redirect(Request.Headers["Referer"].ToString() ?? Url.Action("Index", "Projects")!);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Create(int boardId, int columnId, string title, string? description, Priority priority, string? assigneeId)
	{
		string? currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (string.IsNullOrEmpty(currentUserId)) return Challenge();

		try
		{
			bool isAdmin = User.IsInRole("Admin");
			int targetBoardId = await _cardService.CreateAsync(boardId, columnId, title, description, priority, assigneeId, currentUserId, isAdmin);
			if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Ok(new { reload = true });
			TempData["Success"] = "Card created.";
			return RedirectToAction("Details", "Boards", new { id = targetBoardId });
		}
		catch (Exception ex)
		{
			if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return BadRequest(new { error = ex.Message });
			TempData["Error"] = ex.Message;
			return RedirectToAction("Details", "Boards", new { id = boardId });
		}
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Move(int id, int targetColumnId)
	{
		string? currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (string.IsNullOrEmpty(currentUserId)) return Challenge();

		try
		{
			bool isAdmin = User.IsInRole("Admin");
			int boardId = await _cardService.MoveAsync(id, targetColumnId, currentUserId, isAdmin);
			if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Ok(new { reload = false });
			return RedirectToAction("Details", "Boards", new { id = boardId });
		}
		catch (Exception ex)
		{
			if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return BadRequest(new { error = ex.Message });
			TempData["Error"] = ex.Message;
			return RedirectToAction("Details", "Boards", new { id = Request.Headers["RefererBoardId"] });
		}
	}

	[HttpGet]
	public async Task<IActionResult> Details(int id)
	{
		string? currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (string.IsNullOrEmpty(currentUserId)) return Challenge();
		bool isAdmin = User.IsInRole("Admin");
		var dto = await _cardService.GetDetailsAsync(id, currentUserId, isAdmin);
		if (dto == null) return Forbid();
		if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
			return PartialView("_CardDetails", dto);
		return View("_CardDetails", dto);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Edit(int id, string title, string? description, Priority priority, string? assigneeId)
	{
		string? currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (string.IsNullOrEmpty(currentUserId)) return Challenge();
		bool isAdmin = User.IsInRole("Admin");
		try
		{
			int boardId = await _cardService.UpdateAsync(id, title, description, priority, currentUserId, isAdmin);
			// Also update assignment if supplied
			if (assigneeId != null)
			{
				await _cardService.AssignAsync(id, assigneeId, currentUserId, isAdmin);
			}
			if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Ok(new { reload = true });
			TempData["Success"] = "Card updated.";
			return RedirectToAction("Details", "Boards", new { id = boardId });
		}
		catch (Exception ex)
		{
			if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return BadRequest(new { error = ex.Message });
			TempData["Error"] = ex.Message;
			return Redirect(Request.Headers["Referer"].ToString());
		}
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Delete(int id)
	{
		string? currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (string.IsNullOrEmpty(currentUserId)) return Challenge();
		bool isAdmin = User.IsInRole("Admin");
		try
		{
			int boardId = await _cardService.DeleteAsync(id, currentUserId, isAdmin);
			if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Ok(new { reload = true });
			TempData["Success"] = "Card deleted.";
			return RedirectToAction("Details", "Boards", new { id = boardId });
		}
		catch (Exception ex)
		{
			if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return BadRequest(new { error = ex.Message });
			TempData["Error"] = ex.Message;
			return Redirect(Request.Headers["Referer"].ToString());
		}
	}
} 