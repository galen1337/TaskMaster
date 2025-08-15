using System.Security.Claims;
using Application.Services;
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
} 