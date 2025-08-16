using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class ProjectService : IProjectService
{
	private readonly ApplicationDbContext _db;
	public ProjectService(ApplicationDbContext db) { _db = db; }

	public async Task ChangeMemberRoleAsync(int projectId, string memberUserId, ProjectRole newRole, string currentUserId, bool isPlatformAdmin)
	{
		var project = await _db.Projects.Include(p => p.Members).FirstOrDefaultAsync(p => p.Id == projectId);
		if (project == null) throw new KeyNotFoundException("Project not found");
		bool currentUserIsOwnerOrAdmin = await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == currentUserId && (pm.Role == ProjectRole.Owner || pm.Role == ProjectRole.Admin));
		if (!currentUserIsOwnerOrAdmin && !isPlatformAdmin) throw new UnauthorizedAccessException("Not allowed");
		var member = await _db.ProjectMembers.FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == memberUserId);
		if (member == null) throw new KeyNotFoundException("Member not found");
		if (member.Role == ProjectRole.Owner && newRole != ProjectRole.Owner)
		{
			bool otherOwnersExist = await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.UserId != memberUserId && pm.Role == ProjectRole.Owner);
			if (!otherOwnersExist) throw new InvalidOperationException("Cannot remove the last owner");
		}
		member.Role = newRole;
		await _db.SaveChangesAsync();
	}

	public async Task<IReadOnlyList<ProjectOption>> GetUserProjectsAsync(string userId)
	{
		return await _db.ProjectMembers
			.Where(pm => pm.UserId == userId)
			.Select(pm => new ProjectOption(pm.ProjectId, pm.Project.Name))
			.Distinct()
			.OrderBy(p => p.Name)
			.ToListAsync();
	}
} 