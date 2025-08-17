using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace TaskMaster.Tests.Services;

public static class TestHelpers
{
	public static ApplicationDbContext CreateInMemoryDb(string name)
	{
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase(name)
			.Options;
		return new ApplicationDbContext(options);
	}
} 