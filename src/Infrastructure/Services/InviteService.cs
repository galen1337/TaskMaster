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
				Role = ProjectRole.Member,
				JoinedAt = DateTime.UtcNow
			});
		}

		invite.Status = InviteStatus.Accepted;
		await _db.SaveChangesAsync();
		return true;
	}

	public async Task<string> SendInviteAsync(int projectId, string email, string invitedByUserId, bool isPlatformAdmin)
	{
		if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required", nameof(email));
		bool canManage = isPlatformAdmin || await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == invitedByUserId && (pm.Role == ProjectRole.Owner || pm.Role == ProjectRole.Admin));
		if (!canManage) throw new UnauthorizedAccessException("Not allowed to invite to this project.");

		var token = Guid.NewGuid().ToString("N");
		var invite = new Invite
		{
			ProjectId = projectId,
			InvitedEmail = email.Trim(),
			InvitedByUserId = invitedByUserId,
			Token = token,
			ExpiresAt = DateTime.UtcNow.AddDays(7),
			Status = InviteStatus.Pending,
			CreatedAt = DateTime.UtcNow
		};
		_db.Invites.Add(invite);
		await _db.SaveChangesAsync();
		return token;
	}
} 