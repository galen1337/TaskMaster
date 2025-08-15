namespace Application.Services;

using Domain.Enums;

public interface ICardService
{
	Task<int> AssignAsync(int cardId, string assigneeUserId, string currentUserId, bool isPlatformAdmin);
	Task<int> CreateAsync(int boardId, int columnId, string title, string? description, Priority priority, string? assigneeUserId, string currentUserId, bool isPlatformAdmin);
	Task<int> MoveAsync(int cardId, int targetColumnId, string currentUserId, bool isPlatformAdmin);
	Task<CardDto?> GetDetailsAsync(int cardId, string currentUserId, bool isPlatformAdmin);
	Task<int> UpdateAsync(int cardId, string title, string? description, Priority priority, string currentUserId, bool isPlatformAdmin);
	Task<int> DeleteAsync(int cardId, string currentUserId, bool isPlatformAdmin);
}

public record UserOption(string UserId, string Email);
public record CardDto(int Id, int BoardId, int ColumnId, string Title, string? Description, Priority Priority, string? AssigneeId, IReadOnlyList<UserOption> Assignees); 