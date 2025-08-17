using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace TaskMaster.Tests.Services;

public class ProjectServiceTests
{
	[Fact]
	public async Task ChangeMemberRole_Allows_Owner_To_Demote_Admin()
	{
		using var db = TestHelpers.CreateInMemoryDb(nameof(ChangeMemberRole_Allows_Owner_To_Demote_Admin));
		var owner = new ApplicationUser { Id = "owner", Email = "owner@test.com" };
		var admin = new ApplicationUser { Id = "admin", Email = "admin@test.com" };
		var project = new Project { Id = 1, Name = "P", OwnerId = owner.Id, CreatedAt = DateTime.UtcNow };
		db.Users.AddRange(owner, admin);
		db.Projects.Add(project);
		db.ProjectMembers.AddRange(
			new ProjectMember { ProjectId = 1, UserId = owner.Id, Role = ProjectRole.Owner },
			new ProjectMember { ProjectId = 1, UserId = admin.Id, Role = ProjectRole.Admin }
		);
		await db.SaveChangesAsync();

		var svc = new ProjectService(db);
		await svc.ChangeMemberRoleAsync(1, admin.Id, ProjectRole.Member, owner.Id, isPlatformAdmin: false);

		var updated = await db.ProjectMembers.FirstAsync(pm => pm.UserId == admin.Id && pm.ProjectId == 1);
		Assert.Equal(ProjectRole.Member, updated.Role);
	}

	[Fact]
	public async Task ChangeMemberRole_Prevents_Removing_Last_Owner()
	{
		using var db = TestHelpers.CreateInMemoryDb(nameof(ChangeMemberRole_Prevents_Removing_Last_Owner));
		var owner = new ApplicationUser { Id = "owner", Email = "owner@test.com" };
		var project = new Project { Id = 1, Name = "P", OwnerId = owner.Id, CreatedAt = DateTime.UtcNow };
		db.Users.Add(owner);
		db.Projects.Add(project);
		db.ProjectMembers.Add(new ProjectMember { ProjectId = 1, UserId = owner.Id, Role = ProjectRole.Owner });
		await db.SaveChangesAsync();

		var svc = new ProjectService(db);
		await Assert.ThrowsAsync<InvalidOperationException>(async () =>
			await svc.ChangeMemberRoleAsync(1, owner.Id, ProjectRole.Member, owner.Id, isPlatformAdmin: false));
	}
} 