using Domain.Entities;
using Domain.Enums;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace TaskMaster.Tests.Services;

public class InviteServiceTests
{
	[Fact]
	public async Task SendInvite_Creates_Pending_Invite_With_Token()
	{
		using var db = TestHelpers.CreateInMemoryDb(nameof(SendInvite_Creates_Pending_Invite_With_Token));
		var owner = new ApplicationUser { Id = "owner", Email = "owner@test.com" };
		var project = new Project { Id = 1, Name = "P", OwnerId = owner.Id, CreatedAt = DateTime.UtcNow };
		db.Users.Add(owner);
		db.Projects.Add(project);
		db.ProjectMembers.Add(new ProjectMember { ProjectId = 1, UserId = owner.Id, Role = ProjectRole.Owner });
		await db.SaveChangesAsync();

		var svc = new InviteService(db);
		var token = await svc.SendInviteAsync(1, "invitee@test.com", owner.Id, isPlatformAdmin: false);
		Assert.False(string.IsNullOrWhiteSpace(token));
		Assert.True(await db.Invites.AnyAsync(i => i.ProjectId == 1 && i.Token == token && i.Status == InviteStatus.Pending));
	}

	[Fact]
	public async Task AcceptInvite_Adds_Member_And_Marks_Accepted()
	{
		using var db = TestHelpers.CreateInMemoryDb(nameof(AcceptInvite_Adds_Member_And_Marks_Accepted));
		var owner = new ApplicationUser { Id = "owner", Email = "owner@test.com" };
		var joiner = new ApplicationUser { Id = "joiner", Email = "joiner@test.com" };
		var project = new Project { Id = 1, Name = "P", OwnerId = owner.Id, CreatedAt = DateTime.UtcNow };
		db.Users.AddRange(owner, joiner);
		db.Projects.Add(project);
		db.ProjectMembers.Add(new ProjectMember { ProjectId = 1, UserId = owner.Id, Role = ProjectRole.Owner });
		var invite = new Invite { ProjectId = 1, InvitedEmail = "joiner@test.com", InvitedByUserId = owner.Id, Token = "t", ExpiresAt = DateTime.UtcNow.AddDays(1), Status = InviteStatus.Pending, CreatedAt = DateTime.UtcNow };
		db.Invites.Add(invite);
		await db.SaveChangesAsync();

		var svc = new InviteService(db);
		var ok = await svc.AcceptAsync("t", joiner.Id);
		Assert.True(ok);
		Assert.True(await db.ProjectMembers.AnyAsync(pm => pm.ProjectId == 1 && pm.UserId == joiner.Id));
		Assert.Equal(InviteStatus.Accepted, (await db.Invites.FirstAsync()).Status);
	}
} 