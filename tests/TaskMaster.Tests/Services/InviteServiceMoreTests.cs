using Domain.Entities;
using Domain.Enums;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace TaskMaster.Tests.Services;

public class InviteServiceMoreTests
{
	[Fact]
	public async Task GetInbox_Returns_Pending_Unexpired_For_Email()
	{
		using var db = TestHelpers.CreateInMemoryDb(nameof(GetInbox_Returns_Pending_Unexpired_For_Email));
		var owner = new ApplicationUser { Id = "owner", Email = "owner@test.com" };
		var project = new Project { Id = 1, Name = "P", OwnerId = owner.Id, CreatedAt = DateTime.UtcNow };
		db.Users.Add(owner);
		db.Projects.Add(project);
		db.Invites.AddRange(
			new Invite { ProjectId = 1, InvitedEmail = "u@test.com", InvitedByUserId = owner.Id, Token = "a", ExpiresAt = DateTime.UtcNow.AddDays(1), Status = InviteStatus.Pending, CreatedAt = DateTime.UtcNow },
			new Invite { ProjectId = 1, InvitedEmail = "u@test.com", InvitedByUserId = owner.Id, Token = "b", ExpiresAt = DateTime.UtcNow.AddDays(-1), Status = InviteStatus.Pending, CreatedAt = DateTime.UtcNow },
			new Invite { ProjectId = 1, InvitedEmail = "u@test.com", InvitedByUserId = owner.Id, Token = "c", ExpiresAt = DateTime.UtcNow.AddDays(1), Status = InviteStatus.Accepted, CreatedAt = DateTime.UtcNow },
			new Invite { ProjectId = 1, InvitedEmail = "x@test.com", InvitedByUserId = owner.Id, Token = "d", ExpiresAt = DateTime.UtcNow.AddDays(1), Status = InviteStatus.Pending, CreatedAt = DateTime.UtcNow }
		);
		await db.SaveChangesAsync();

		var svc = new InviteService(db);
		var inbox = await svc.GetInboxAsync("u@test.com");
		Assert.Single(inbox);
		Assert.Equal("a", inbox[0].Token);
	}
} 