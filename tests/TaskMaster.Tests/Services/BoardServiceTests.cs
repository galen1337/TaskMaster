using Domain.Entities;
using Domain.Enums;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace TaskMaster.Tests.Services;

public class BoardServiceTests
{
	[Fact]
	public async Task CreateBoard_Seeds_Default_Columns_And_Adds_Creator_As_Admin()
	{
		using var db = TestHelpers.CreateInMemoryDb(nameof(CreateBoard_Seeds_Default_Columns_And_Adds_Creator_As_Admin));
		var user = new ApplicationUser { Id = "u1", Email = "u1@test.com" };
		var project = new Project { Id = 1, Name = "P", OwnerId = user.Id, CreatedAt = DateTime.UtcNow };
		db.Users.Add(user);
		db.Projects.Add(project);
		db.ProjectMembers.Add(new ProjectMember { ProjectId = 1, UserId = user.Id, Role = ProjectRole.Owner });
		await db.SaveChangesAsync();

		var svc = new BoardService(db);
		var board = await svc.CreateBoardAsync(1, user.Id, isPlatformAdmin: false, new Application.Services.CreateBoardDto { Name = "Board A" });

		var cols = await db.Columns.Where(c => c.BoardId == board.Id).OrderBy(c => c.Order).ToListAsync();
		Assert.Equal(3, cols.Count);
		Assert.Contains(cols, c => c.Name == "To Do");
		Assert.True(await db.BoardMembers.AnyAsync(bm => bm.BoardId == board.Id && bm.UserId == user.Id && bm.Role == BoardRole.Admin));
	}

	[Fact]
	public async Task GetBoardDetails_Blocks_NonMembers_When_Not_Admin()
	{
		using var db = TestHelpers.CreateInMemoryDb(nameof(GetBoardDetails_Blocks_NonMembers_When_Not_Admin));
		var user = new ApplicationUser { Id = "u1", Email = "u1@test.com" };
		var owner = new ApplicationUser { Id = "owner", Email = "owner@test.com" };
		var project = new Project { Id = 1, Name = "P", OwnerId = owner.Id, CreatedAt = DateTime.UtcNow };
		var board = new Board { Id = 10, Name = "B", ProjectId = 1 };
		db.Users.AddRange(user, owner);
		db.Projects.Add(project);
		db.Boards.Add(board);
		db.ProjectMembers.Add(new ProjectMember { ProjectId = 1, UserId = owner.Id, Role = ProjectRole.Owner });
		await db.SaveChangesAsync();

		var svc = new BoardService(db);
		var result = await svc.GetBoardDetailsAsync(10, user.Id, isPlatformAdmin: false);
		Assert.Null(result);
	}
} 