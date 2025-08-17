using Domain.Entities;
using Domain.Enums;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace TaskMaster.Tests.Services;

public class CardServiceMoreTests
{
	[Fact]
	public async Task AssignAsync_Allows_BoardAdmin_And_Unassigns_On_Empty()
	{
		using var db = TestHelpers.CreateInMemoryDb(nameof(AssignAsync_Allows_BoardAdmin_And_Unassigns_On_Empty));
		var boardAdmin = new ApplicationUser { Id = "admin", Email = "admin@test.com" };
		var assignee = new ApplicationUser { Id = "ass", Email = "ass@test.com" };
		var owner = new ApplicationUser { Id = "owner", Email = "owner@test.com" };
		var project = new Project { Id = 1, Name = "P", OwnerId = owner.Id, CreatedAt = DateTime.UtcNow };
		var board = new Board { Id = 10, Name = "B", ProjectId = 1 };
		var col = new Column { Id = 100, BoardId = 10, Name = "To Do", Order = 0 };
		var card = new Card { Id = 1000, BoardId = 10, ColumnId = 100, Title = "T" };
		db.Users.AddRange(boardAdmin, assignee, owner);
		db.Projects.Add(project);
		db.Boards.Add(board);
		db.Columns.Add(col);
		db.Cards.Add(card);
		db.ProjectMembers.AddRange(
			new ProjectMember { ProjectId = 1, UserId = owner.Id, Role = ProjectRole.Owner },
			new ProjectMember { ProjectId = 1, UserId = assignee.Id, Role = ProjectRole.Member }
		);
		db.BoardMembers.Add(new BoardMember { BoardId = 10, UserId = boardAdmin.Id, Role = BoardRole.Admin, AssignedAt = DateTime.UtcNow });
		await db.SaveChangesAsync();

		var svc = new CardService(db);
		// Assign
		var boardId = await svc.AssignAsync(1000, assignee.Id, boardAdmin.Id, isPlatformAdmin: false);
		Assert.Equal(10, boardId);
		Assert.Equal(assignee.Id, (await db.Cards.FindAsync(1000))!.AssigneeId);
		// Unassign
		boardId = await svc.AssignAsync(1000, " ", boardAdmin.Id, isPlatformAdmin: false);
		Assert.Null((await db.Cards.FindAsync(1000))!.AssigneeId);
	}

	[Fact]
	public async Task AssignAsync_Blocks_Unauthorized_User()
	{
		using var db = TestHelpers.CreateInMemoryDb(nameof(AssignAsync_Blocks_Unauthorized_User));
		var other = new ApplicationUser { Id = "other", Email = "other@test.com" };
		var owner = new ApplicationUser { Id = "owner", Email = "owner@test.com" };
		var assignee = new ApplicationUser { Id = "ass", Email = "ass@test.com" };
		var project = new Project { Id = 1, Name = "P", OwnerId = owner.Id, CreatedAt = DateTime.UtcNow };
		var board = new Board { Id = 10, Name = "B", ProjectId = 1 };
		var col = new Column { Id = 100, BoardId = 10, Name = "To Do", Order = 0 };
		var card = new Card { Id = 1000, BoardId = 10, ColumnId = 100, Title = "T" };
		db.Users.AddRange(other, owner, assignee);
		db.Projects.Add(project);
		db.Boards.Add(board);
		db.Columns.Add(col);
		db.Cards.Add(card);
		db.ProjectMembers.AddRange(
			new ProjectMember { ProjectId = 1, UserId = owner.Id, Role = ProjectRole.Owner },
			new ProjectMember { ProjectId = 1, UserId = assignee.Id, Role = ProjectRole.Member }
		);
		await db.SaveChangesAsync();

		var svc = new CardService(db);
		await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
			await svc.AssignAsync(1000, assignee.Id, other.Id, isPlatformAdmin: false));
	}

	[Fact]
	public async Task Update_And_Delete_Respect_Permissions()
	{
		using var db = TestHelpers.CreateInMemoryDb(nameof(Update_And_Delete_Respect_Permissions));
		var owner = new ApplicationUser { Id = "owner", Email = "owner@test.com" };
		var member = new ApplicationUser { Id = "mem", Email = "mem@test.com" };
		var project = new Project { Id = 1, Name = "P", OwnerId = owner.Id, CreatedAt = DateTime.UtcNow };
		var board = new Board { Id = 10, Name = "B", ProjectId = 1 };
		var col = new Column { Id = 100, BoardId = 10, Name = "To Do", Order = 0 };
		var card = new Card { Id = 1000, BoardId = 10, ColumnId = 100, Title = "Old" };
		db.Users.AddRange(owner, member);
		db.Projects.Add(project);
		db.Boards.Add(board);
		db.Columns.Add(col);
		db.Cards.Add(card);
		db.ProjectMembers.AddRange(
			new ProjectMember { ProjectId = 1, UserId = owner.Id, Role = ProjectRole.Owner },
			new ProjectMember { ProjectId = 1, UserId = member.Id, Role = ProjectRole.Member }
		);
		await db.SaveChangesAsync();

		var svc = new CardService(db);
		await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
			await svc.UpdateAsync(1000, "New", null, Priority.High, member.Id, false));

		var bid = await svc.UpdateAsync(1000, "New", "Desc", Priority.Low, owner.Id, false);
		Assert.Equal(10, bid);
		var updated = await db.Cards.FindAsync(1000);
		Assert.Equal("New", updated!.Title);
		Assert.Equal("Desc", updated.Description);

		var delBoard = await svc.DeleteAsync(1000, owner.Id, false);
		Assert.Equal(10, delBoard);
		Assert.False(await db.Cards.AnyAsync(c => c.Id == 1000));
	}

	[Fact]
	public async Task GetDetails_Returns_Dto_For_ProjectMember_And_Null_For_NonMember()
	{
		using var db = TestHelpers.CreateInMemoryDb(nameof(GetDetails_Returns_Dto_For_ProjectMember_And_Null_For_NonMember));
		var owner = new ApplicationUser { Id = "owner", Email = "owner@test.com" };
		var member = new ApplicationUser { Id = "mem", Email = "mem@test.com" };
		var outsider = new ApplicationUser { Id = "out", Email = "out@test.com" };
		var project = new Project { Id = 1, Name = "P", OwnerId = owner.Id, CreatedAt = DateTime.UtcNow };
		var board = new Board { Id = 10, Name = "B", ProjectId = 1 };
		var col = new Column { Id = 100, BoardId = 10, Name = "To Do", Order = 0 };
		var card = new Card { Id = 1000, BoardId = 10, ColumnId = 100, Title = "T" };
		db.Users.AddRange(owner, member, outsider);
		db.Projects.Add(project);
		db.Boards.Add(board);
		db.Columns.Add(col);
		db.Cards.Add(card);
		db.ProjectMembers.AddRange(
			new ProjectMember { ProjectId = 1, UserId = owner.Id, Role = ProjectRole.Owner },
			new ProjectMember { ProjectId = 1, UserId = member.Id, Role = ProjectRole.Member }
		);
		await db.SaveChangesAsync();

		var svc = new CardService(db);
		var dto = await svc.GetDetailsAsync(1000, member.Id, false);
		Assert.NotNull(dto);
		Assert.Equal(1000, dto!.Id);
		Assert.True(dto.Assignees.Any(u => u.UserId == owner.Id));
		Assert.False((await svc.GetDetailsAsync(1000, outsider.Id, false)) != null);
	}
} 