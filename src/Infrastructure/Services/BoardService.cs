using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class BoardService : IBoardService
{
	private readonly ApplicationDbContext _db;

	public BoardService(ApplicationDbContext db)
	{
		_db = db;
	}

	public async Task<Board?> GetBoardDetailsAsync(int boardId, string currentUserId, bool isPlatformAdmin)
	{
		var board = await _db.Boards
			.Include(b => b.Project)
			.Include(b => b.Columns)
			.Include(b => b.Cards)
			.FirstOrDefaultAsync(b => b.Id == boardId);
		if (board == null) return null;

		bool isProjectMember = await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == board.ProjectId && pm.UserId == currentUserId);
		if (!isProjectMember && !isPlatformAdmin && board.IsPrivate)
		{
			return null;
		}

		return board;
	}

	public async Task<Board> CreateBoardAsync(int projectId, string currentUserId, bool isPlatformAdmin, CreateBoardDto dto)
	{
		if (string.IsNullOrWhiteSpace(dto.Name))
			throw new ArgumentException("Board name is required", nameof(dto.Name));

		bool canManageProject = isPlatformAdmin || await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == currentUserId && (pm.Role == ProjectRole.Owner || pm.Role == ProjectRole.Admin));
		if (!canManageProject)
		{
			throw new UnauthorizedAccessException("Not allowed to create boards for this project.");
		}

		var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
		if (project == null) throw new KeyNotFoundException("Project not found");

		var board = new Board
		{
			ProjectId = projectId,
			Name = dto.Name.Trim(),
			IsPrivate = dto.IsPrivate,
			CreatedAt = DateTime.UtcNow
		};

		_db.Boards.Add(board);
		await _db.SaveChangesAsync();

		// Ensure creator is a board admin by default
		bool creatorAlreadyMember = await _db.BoardMembers.AnyAsync(bm => bm.BoardId == board.Id && bm.UserId == currentUserId);
		if (!creatorAlreadyMember)
		{
			_db.BoardMembers.Add(new BoardMember
			{
				BoardId = board.Id,
				UserId = currentUserId,
				Role = BoardRole.Admin,
				AssignedAt = DateTime.UtcNow
			});
			await _db.SaveChangesAsync();
		}

		// Seed default columns
		var defaultColumns = new[] { "To Do", "In Progress", "Done" };
		int order = 0;
		foreach (var name in defaultColumns)
		{
			_db.Columns.Add(new Column
			{
				BoardId = board.Id,
				Name = name,
				Order = order++
			});
		}
		await _db.SaveChangesAsync();

		return board;
	}
} 