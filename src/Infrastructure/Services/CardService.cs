using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class CardService : ICardService
{
	private readonly ApplicationDbContext _db;

	public CardService(ApplicationDbContext db)
	{
		_db = db;
	}

	public async Task<int> AssignAsync(int cardId, string assigneeUserId, string currentUserId, bool isPlatformAdmin)
	{
		var card = await _db.Cards.Include(c => c.Board).FirstOrDefaultAsync(c => c.Id == cardId);
		if (card == null) throw new KeyNotFoundException("Card not found");

		// Verify assignee is a member of the project
		var projectId = await _db.Boards.Where(b => b.Id == card.BoardId).Select(b => b.ProjectId).FirstAsync();
		bool currentUserIsProjectMember = await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == currentUserId);
		bool assigneeIsProjectMember = await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == assigneeUserId);
		if (!assigneeIsProjectMember)
			throw new ArgumentException("Assignee must be a member of the project.");

		// Authorization: member of project or platform admin can assign
		if (!currentUserIsProjectMember && !isPlatformAdmin)
			throw new UnauthorizedAccessException("Not allowed to assign this card.");

		card.AssigneeId = assigneeUserId;
		card.UpdatedAt = DateTime.UtcNow;
		await _db.SaveChangesAsync();

		return card.BoardId;
	}

	public async Task<int> CreateAsync(int boardId, int columnId, string title, string? description, Priority priority, string? assigneeUserId, string currentUserId, bool isPlatformAdmin)
	{
		if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required", nameof(title));

		var board = await _db.Boards.FirstOrDefaultAsync(b => b.Id == boardId);
		if (board == null) throw new KeyNotFoundException("Board not found");

		var projectId = board.ProjectId;

		// Only board members or platform admin can create
		bool isBoardMember = await _db.BoardMembers.AnyAsync(bm => bm.BoardId == boardId && bm.UserId == currentUserId);
		if (!isBoardMember && !isPlatformAdmin)
			throw new UnauthorizedAccessException("Not allowed to create cards on this board.");

		if (assigneeUserId != null)
		{
			bool assigneeIsProjectMember = await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == assigneeUserId);
			if (!assigneeIsProjectMember)
				throw new ArgumentException("Assignee must be a project member.");
		}

		// Validate column belongs to board
		bool columnMatchesBoard = await _db.Columns.AnyAsync(c => c.Id == columnId && c.BoardId == boardId);
		if (!columnMatchesBoard) throw new ArgumentException("Column does not belong to board");

		var card = new Card
		{
			BoardId = boardId,
			ColumnId = columnId,
			Title = title.Trim(),
			Description = string.IsNullOrWhiteSpace(description) ? null : description,
			Priority = priority,
			AssigneeId = assigneeUserId,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};
		_db.Cards.Add(card);
		await _db.SaveChangesAsync();
		return card.BoardId;
	}

	public async Task<int> MoveAsync(int cardId, int targetColumnId, string currentUserId, bool isPlatformAdmin)
	{
		var card = await _db.Cards.FirstOrDefaultAsync(c => c.Id == cardId);
		if (card == null) throw new KeyNotFoundException("Card not found");

		var boardId = card.BoardId;
		var board = await _db.Boards.FirstAsync(b => b.Id == boardId);

		// Only board members or platform admin can move
		bool isBoardMember = await _db.BoardMembers.AnyAsync(bm => bm.BoardId == boardId && bm.UserId == currentUserId);
		if (!isBoardMember && !isPlatformAdmin)
			throw new UnauthorizedAccessException("Not allowed to move cards on this board.");

		// Validate target column belongs to same board
		bool columnMatchesBoard = await _db.Columns.AnyAsync(c => c.Id == targetColumnId && c.BoardId == boardId);
		if (!columnMatchesBoard) throw new ArgumentException("Target column does not belong to the board");

		card.ColumnId = targetColumnId;
		card.UpdatedAt = DateTime.UtcNow;
		await _db.SaveChangesAsync();
		return boardId;
	}
} 