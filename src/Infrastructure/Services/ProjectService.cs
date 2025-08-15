using Application.Services;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class ProjectService : IProjectService
{
	private readonly ApplicationDbContext _db;

	public ProjectService(ApplicationDbContext db)
	{
		_db = db;
	}

	public async Task ChangeMemberRoleAsync(int projectId, string targetUserId, ProjectRole newRole, string actingUserId, bool isPlatformAdmin)
	{
		bool actingIsOwnerOrAdmin = isPlatformAdmin || await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == actingUserId && (pm.Role == ProjectRole.Owner || pm.Role == ProjectRole.Admin));
		if (!actingIsOwnerOrAdmin) throw new UnauthorizedAccessException("Not allowed to change roles.");

		var targetMember = await _db.ProjectMembers.FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == targetUserId);
		if (targetMember == null) throw new KeyNotFoundException("Member not found");

		// Prevent demoting the last Owner
		if (targetMember.Role == ProjectRole.Owner && newRole != ProjectRole.Owner)
		{
			int ownerCount = await _db.ProjectMembers.CountAsync(pm => pm.ProjectId == projectId && pm.Role == ProjectRole.Owner);
			if (ownerCount <= 1) throw new InvalidOperationException("Cannot demote the last Owner.");
		}

		targetMember.Role = newRole;
		await _db.SaveChangesAsync();
	}
} 