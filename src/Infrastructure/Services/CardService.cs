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

		var projectId = await _db.Boards.Where(b => b.Id == card.BoardId).Select(b => b.ProjectId).FirstAsync();
		bool currentUserIsBoardAdmin = await _db.BoardMembers.AnyAsync(bm => bm.BoardId == card.BoardId && bm.UserId == currentUserId && bm.Role == BoardRole.Admin);
		bool currentUserIsProjectOwnerOrAdmin = await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == currentUserId && (pm.Role == ProjectRole.Owner || pm.Role == ProjectRole.Admin));

		// Authorization: board admin, project Owner/Admin, or platform admin can assign/unassign
		if (!currentUserIsBoardAdmin && !currentUserIsProjectOwnerOrAdmin && !isPlatformAdmin)
			throw new UnauthorizedAccessException("Not allowed to assign this card.");

		// Empty means unassign
		if (string.IsNullOrWhiteSpace(assigneeUserId))
		{
			card.AssigneeId = null;
			card.UpdatedAt = DateTime.UtcNow;
			await _db.SaveChangesAsync();
			return card.BoardId;
		}

		// Verify assignee is a member of the project when assigning
		bool assigneeIsProjectMember = await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == assigneeUserId);
		if (!assigneeIsProjectMember)
			throw new ArgumentException("Assignee must be a member of the project.");

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

		// Only board members, project Owner/Admin, or platform admin can create
		bool isBoardMember = await _db.BoardMembers.AnyAsync(bm => bm.BoardId == boardId && bm.UserId == currentUserId);
		bool isProjectOwnerOrAdmin = await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == currentUserId && (pm.Role == ProjectRole.Owner || pm.Role == ProjectRole.Admin));
		if (!isBoardMember && !isProjectOwnerOrAdmin && !isPlatformAdmin)
			throw new UnauthorizedAccessException("Not allowed to create cards on this board.");

		var normalizedAssigneeId = string.IsNullOrWhiteSpace(assigneeUserId) ? null : assigneeUserId;
		if (normalizedAssigneeId != null)
		{
			bool assigneeIsProjectMember = await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == normalizedAssigneeId);
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
			AssigneeId = normalizedAssigneeId,
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

		// Only board members, project Owner/Admin, or platform admin can move
		bool isBoardMember = await _db.BoardMembers.AnyAsync(bm => bm.BoardId == boardId && bm.UserId == currentUserId);
		bool isProjectOwnerOrAdmin = await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == board.ProjectId && pm.UserId == currentUserId && (pm.Role == ProjectRole.Owner || pm.Role == ProjectRole.Admin));
		if (!isBoardMember && !isProjectOwnerOrAdmin && !isPlatformAdmin)
			throw new UnauthorizedAccessException("Not allowed to move cards on this board.");

		// Validate target column belongs to same board
		bool columnMatchesBoard = await _db.Columns.AnyAsync(c => c.Id == targetColumnId && c.BoardId == boardId);
		if (!columnMatchesBoard) throw new ArgumentException("Target column does not belong to the board");

		card.ColumnId = targetColumnId;
		card.UpdatedAt = DateTime.UtcNow;
		await _db.SaveChangesAsync();
		return boardId;
	}

	public async Task<CardDto?> GetDetailsAsync(int cardId, string currentUserId, bool isPlatformAdmin)
	{
		var card = await _db.Cards.Include(c => c.Assignee).FirstOrDefaultAsync(c => c.Id == cardId);
		if (card == null) return null;
		int boardId = card.BoardId;
		var board = await _db.Boards.FirstAsync(b => b.Id == boardId);
		bool isBoardMember = await _db.BoardMembers.AnyAsync(bm => bm.BoardId == boardId && bm.UserId == currentUserId);
		bool isProjectMember = await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == board.ProjectId && pm.UserId == currentUserId);
		bool isProjectOwnerOrAdmin = await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == board.ProjectId && pm.UserId == currentUserId && (pm.Role == ProjectRole.Owner || pm.Role == ProjectRole.Admin));
		if (!isBoardMember && !isProjectMember && !isPlatformAdmin) return null;

		var options = await _db.ProjectMembers
			.Where(pm => pm.ProjectId == board.ProjectId)
			.Select(pm => new UserOption(pm.UserId, pm.User.Email))
			.ToListAsync();

		return new CardDto(card.Id, card.BoardId, card.ColumnId, card.Title, card.Description, card.Priority, card.AssigneeId, options, isProjectOwnerOrAdmin || isPlatformAdmin);
	}

	public async Task<int> UpdateAsync(int cardId, string title, string? description, Priority priority, string currentUserId, bool isPlatformAdmin)
	{
		if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title required", nameof(title));
		var card = await _db.Cards.FirstOrDefaultAsync(c => c.Id == cardId);
		if (card == null) throw new KeyNotFoundException("Card not found");
		int boardId = card.BoardId;
		var board = await _db.Boards.FirstAsync(b => b.Id == boardId);
		bool isProjectOwnerOrAdmin = await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == board.ProjectId && pm.UserId == currentUserId && (pm.Role == ProjectRole.Owner || pm.Role == ProjectRole.Admin));
		if (!isProjectOwnerOrAdmin && !isPlatformAdmin) throw new UnauthorizedAccessException("Not allowed to edit this card.");
		card.Title = title.Trim();
		card.Description = string.IsNullOrWhiteSpace(description) ? null : description;
		card.Priority = priority;
		card.UpdatedAt = DateTime.UtcNow;
		await _db.SaveChangesAsync();
		return boardId;
	}

	public async Task<int> DeleteAsync(int cardId, string currentUserId, bool isPlatformAdmin)
	{
		var card = await _db.Cards.FirstOrDefaultAsync(c => c.Id == cardId);
		if (card == null) throw new KeyNotFoundException("Card not found");
		int boardId = card.BoardId;
		var board = await _db.Boards.FirstAsync(b => b.Id == boardId);
		bool isProjectOwnerOrAdmin = await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == board.ProjectId && pm.UserId == currentUserId && (pm.Role == ProjectRole.Owner || pm.Role == ProjectRole.Admin));
		if (!isProjectOwnerOrAdmin && !isPlatformAdmin) throw new UnauthorizedAccessException("Not allowed to delete this card.");
		_db.Cards.Remove(card);
		await _db.SaveChangesAsync();
		return boardId;
	}
} 