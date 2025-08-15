namespace Application.Services;

using Domain.Enums;

public interface IProjectService
{
	Task ChangeMemberRoleAsync(int projectId, string targetUserId, ProjectRole newRole, string actingUserId, bool isPlatformAdmin);
} 