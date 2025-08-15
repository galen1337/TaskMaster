using System.Security.Claims;
using Application.Services;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TaskMaster.Controllers;

[Authorize]
public class CardsController : Controller
{
	private readonly ICardService _cardService;

	public CardsController(ICardService cardService)
	{
		_cardService = cardService;
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Assign(int id, string userId)
	{
		string? currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (string.IsNullOrEmpty(currentUserId)) return Challenge();

		try
		{
			bool isAdmin = User.IsInRole("Admin");
			int boardId = await _cardService.AssignAsync(id, userId, currentUserId, isAdmin);
			TempData["Success"] = "Card assigned.";
			return RedirectToAction("Details", "Boards", new { id = boardId });
		}
		catch (UnauthorizedAccessException)
		{
			TempData["Error"] = "You are not allowed to assign this card.";
		}
		catch (ArgumentException ex)
		{
			TempData["Error"] = ex.Message;
		}
		catch (Exception)
		{
			TempData["Error"] = "Failed to assign card.";
		}

		return RedirectToAction("Index", "Projects");
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
			TempData["Success"] = "Card created.";
			return RedirectToAction("Details", "Boards", new { id = targetBoardId });
		}
		catch (Exception ex)
		{
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
			return RedirectToAction("Details", "Boards", new { id = boardId });
		}
		catch (Exception ex)
		{
			TempData["Error"] = ex.Message;
			return RedirectToAction("Index", "Projects");
		}
	}
} 