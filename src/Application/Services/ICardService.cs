namespace Application.Services;

using Domain.Enums;

public interface ICardService
{
	Task<int> AssignAsync(int cardId, string assigneeUserId, string currentUserId, bool isPlatformAdmin);
	Task<int> CreateAsync(int boardId, int columnId, string title, string? description, Priority priority, string? assigneeUserId, string currentUserId, bool isPlatformAdmin);
	Task<int> MoveAsync(int cardId, int targetColumnId, string currentUserId, bool isPlatformAdmin);
} 