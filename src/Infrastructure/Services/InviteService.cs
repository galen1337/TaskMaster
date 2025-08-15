using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class InviteService : IInviteService
{
	private readonly ApplicationDbContext _db;

	public InviteService(ApplicationDbContext db)
	{
		_db = db;
	}

	public async Task<IReadOnlyList<Invite>> GetInboxAsync(string userEmail)
	{
		if (string.IsNullOrWhiteSpace(userEmail)) return Array.Empty<Invite>();
		var now = DateTime.UtcNow;
		return await _db.Invites
			.Where(i => i.InvitedEmail == userEmail && i.Status == InviteStatus.Pending && i.ExpiresAt > now)
			.OrderByDescending(i => i.CreatedAt)
			.ToListAsync();
	}

	public async Task<bool> AcceptAsync(string token, string userId)
	{
		var invite = await _db.Invites.FirstOrDefaultAsync(i => i.Token == token);
		if (invite == null || invite.Status != InviteStatus.Pending || invite.ExpiresAt <= DateTime.UtcNow)
			return false;

		bool alreadyMember = await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == invite.ProjectId && pm.UserId == userId);
		if (!alreadyMember)
		{
			_db.ProjectMembers.Add(new ProjectMember
			{
				ProjectId = invite.ProjectId,
				UserId = userId,
				Role = Domain.Enums.ProjectRole.Member,
				JoinedAt = DateTime.UtcNow
			});
		}

		invite.Status = InviteStatus.Accepted;
		await _db.SaveChangesAsync();
		return true;
	}
} 