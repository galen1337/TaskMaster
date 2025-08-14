using System.Security.Claims;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TaskMaster.Controllers;

[Authorize]
public class BoardsController : Controller
{
	private readonly ApplicationDbContext _context;

	public BoardsController(ApplicationDbContext context)
	{
		_context = context;
	}

	public async Task<IActionResult> Details(int id)
	{
		string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (string.IsNullOrEmpty(userId)) return Challenge();

		var board = await _context.Boards
			.Include(b => b.Project)
			.Include(b => b.Columns)
			.Include(b => b.Cards)
			.FirstOrDefaultAsync(b => b.Id == id);
		if (board == null) return NotFound();

		// Minimal membership check via project membership
		bool isMember = await _context.ProjectMembers.AnyAsync(pm => pm.ProjectId == board.ProjectId && pm.UserId == userId);
		bool isAdmin = User.IsInRole("Admin");
		if (!isMember && !isAdmin && board.IsPrivate)
		{
			return Forbid();
		}

		return View(board);
	}
} 