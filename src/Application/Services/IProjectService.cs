namespace Application.Services;

using Domain.Enums;

public interface IProjectService
{
	Task ChangeMemberRoleAsync(int projectId, string memberUserId, ProjectRole newRole, string currentUserId, bool isPlatformAdmin);
	Task<IReadOnlyList<ProjectOption>> GetUserProjectsAsync(string userId);
}

public record ProjectOption(int Id, string Name); 