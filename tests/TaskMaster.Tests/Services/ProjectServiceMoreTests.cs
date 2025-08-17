using Infrastructure.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace TaskMaster.Tests.Services;

public class ProjectServiceMoreTests
{
	[Fact]
	public async Task GetUserProjects_Returns_Distinct_Sorted_List()
	{
		using var db = TestHelpers.CreateInMemoryDb(nameof(GetUserProjects_Returns_Distinct_Sorted_List));
		var user = new ApplicationUser { Id = "u1", Email = "u1@test.com" };
		var p1 = new Project { Id = 1, Name = "B", OwnerId = user.Id, CreatedAt = DateTime.UtcNow };
		var p2 = new Project { Id = 2, Name = "A", OwnerId = user.Id, CreatedAt = DateTime.UtcNow };
		db.Users.Add(user);
		db.Projects.AddRange(p1, p2);
		db.ProjectMembers.AddRange(
			new ProjectMember { ProjectId = 1, UserId = user.Id, Role = ProjectRole.Member },
			new ProjectMember { ProjectId = 2, UserId = user.Id, Role = ProjectRole.Member }
		);
		await db.SaveChangesAsync();

		var svc = new ProjectService(db);
		var items = await svc.GetUserProjectsAsync(user.Id);
		Assert.Equal(new[] { "A", "B" }, items.Select(i => i.Name).ToArray());
	}
} 