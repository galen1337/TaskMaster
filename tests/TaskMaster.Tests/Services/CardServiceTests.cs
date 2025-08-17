using Domain.Entities;
using Domain.Enums;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace TaskMaster.Tests.Services;

public class CardServiceTests
{
	[Fact]
	public async Task CreateCard_Validates_Membership_And_Assignee()
	{
		using var db = TestHelpers.CreateInMemoryDb(nameof(CreateCard_Validates_Membership_And_Assignee));
		var owner = new ApplicationUser { Id = "owner", Email = "owner@test.com" };
		var member = new ApplicationUser { Id = "member", Email = "member@test.com" };
		var nonMember = new ApplicationUser { Id = "nomem", Email = "nomem@test.com" };
		var project = new Project { Id = 1, Name = "P", OwnerId = owner.Id, CreatedAt = DateTime.UtcNow };
		var board = new Board { Id = 10, Name = "B", ProjectId = 1 };
		var col = new Column { Id = 100, BoardId = 10, Name = "To Do", Order = 0 };
		db.Users.AddRange(owner, member, nonMember);
		db.Projects.Add(project);
		db.Boards.Add(board);
		db.Columns.Add(col);
		db.ProjectMembers.AddRange(
			new ProjectMember { ProjectId = 1, UserId = owner.Id, Role = ProjectRole.Owner },
			new ProjectMember { ProjectId = 1, UserId = member.Id, Role = ProjectRole.Member }
		);
		await db.SaveChangesAsync();

		var svc = new CardService(db);
		// Should succeed assigning to member
		var boardId = await svc.CreateAsync(10, 100, "Task A", null, Priority.Medium, member.Id, owner.Id, false);
		Assert.Equal(10, boardId);

		// Should fail assigning to non-member
		await Assert.ThrowsAsync<ArgumentException>(async () =>
			await svc.CreateAsync(10, 100, "Task B", null, Priority.Medium, nonMember.Id, owner.Id, false));
	}

	[Fact]
	public async Task MoveCard_Requires_Permissions_And_Valid_Target()
	{
		using var db = TestHelpers.CreateInMemoryDb(nameof(MoveCard_Requires_Permissions_And_Valid_Target));
		var owner = new ApplicationUser { Id = "owner", Email = "owner@test.com" };
		var other = new ApplicationUser { Id = "other", Email = "other@test.com" };
		var project = new Project { Id = 1, Name = "P", OwnerId = owner.Id, CreatedAt = DateTime.UtcNow };
		var board = new Board { Id = 10, Name = "B", ProjectId = 1 };
		var c1 = new Column { Id = 100, BoardId = 10, Name = "To Do", Order = 0 };
		var c2 = new Column { Id = 101, BoardId = 10, Name = "In Progress", Order = 1 };
		var card = new Card { Id = 1000, BoardId = 10, ColumnId = 100, Title = "T" };
		db.Users.AddRange(owner, other);
		db.Projects.Add(project);
		db.Boards.Add(board);
		db.Columns.AddRange(c1, c2);
		db.Cards.Add(card);
		db.ProjectMembers.Add(new ProjectMember { ProjectId = 1, UserId = owner.Id, Role = ProjectRole.Owner });
		await db.SaveChangesAsync();

		var svc = new CardService(db);
		// Unauthorized user
		await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
			await svc.MoveAsync(1000, 101, other.Id, false));

		// Authorized
		var resultBoardId = await svc.MoveAsync(1000, 101, owner.Id, false);
		Assert.Equal(10, resultBoardId);
		Assert.Equal(101, (await db.Cards.FindAsync(1000))!.ColumnId);
	}
} 