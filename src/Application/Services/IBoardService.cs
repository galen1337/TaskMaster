namespace Application.Services;

using Domain.Entities;

public interface IBoardService
{
	Task<Board?> GetBoardDetailsAsync(int boardId, string currentUserId, bool isPlatformAdmin);
	Task<Board> CreateBoardAsync(int projectId, string currentUserId, bool isPlatformAdmin, CreateBoardDto dto);
} 