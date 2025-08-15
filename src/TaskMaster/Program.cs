using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<Application.Services.IBoardService, Infrastructure.Services.BoardService>();

var app = builder.Build();

// Seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    
    await context.Database.MigrateAsync();
    await SeedData(context, userManager, roleManager);
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// Add specific route for root to redirect to Projects
app.MapGet("/", async context =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        context.Response.Redirect("/Projects");
    }
    else
    {
        context.Response.Redirect("/Identity/Account/Login");
    }
});

app.Run();

async Task SeedData(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
{
    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    if (!await roleManager.RoleExistsAsync("User"))
        await roleManager.CreateAsync(new IdentityRole("User"));

    ApplicationUser? adminUser = await userManager.FindByEmailAsync("admin@taskmaster.com");
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = "admin@taskmaster.com",
            Email = "admin@taskmaster.com",
            FirstName = "Admin",
            LastName = "User",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(adminUser, "Admin123!");
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }

    ApplicationUser? user1 = await userManager.FindByEmailAsync("john@taskmaster.com");
    if (user1 == null)
    {
        user1 = new ApplicationUser
        {
            UserName = "john@taskmaster.com",
            Email = "john@taskmaster.com",
            FirstName = "John",
            LastName = "Doe",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(user1, "User123!");
        await userManager.AddToRoleAsync(user1, "User");
    }

    ApplicationUser? user2 = await userManager.FindByEmailAsync("jane@taskmaster.com");
    if (user2 == null)
    {
        user2 = new ApplicationUser
        {
            UserName = "jane@taskmaster.com",
            Email = "jane@taskmaster.com",
            FirstName = "Jane",
            LastName = "Smith",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(user2, "User123!");
        await userManager.AddToRoleAsync(user2, "User");
    }

    if (!context.Projects.Any())
    {
        var project = new Project
        {
            Name = "TaskMaster Demo",
            Key = "DEMO",
            Description = "Demo project for TaskMaster Kanban application",
            OwnerId = adminUser.Id
        };
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        // Create labels
        var bugLabel = new Label { ProjectId = project.Id, Name = "Bug", Color = "#FF5733" };
        var featureLabel = new Label { ProjectId = project.Id, Name = "Feature", Color = "#33FF57" };
        var highPriorityLabel = new Label { ProjectId = project.Id, Name = "High Priority", Color = "#FF3357" };
        context.Labels.AddRange(bugLabel, featureLabel, highPriorityLabel);
        await context.SaveChangesAsync();

        var board = new Board
        {
            ProjectId = project.Id,
            Name = "Main Board",
            IsPrivate = false
        };
        context.Boards.Add(board);
        await context.SaveChangesAsync();

        var todoColumn = new Column { BoardId = board.Id, Name = "To Do", Order = 1 };
        var inProgressColumn = new Column { BoardId = board.Id, Name = "In Progress", Order = 2 };
        var reviewColumn = new Column { BoardId = board.Id, Name = "Review", Order = 3 };
        var doneColumn = new Column { BoardId = board.Id, Name = "Done", Order = 4 };
        
        context.Columns.AddRange(todoColumn, inProgressColumn, reviewColumn, doneColumn);
        await context.SaveChangesAsync();

        var card1 = new Card
        {
            BoardId = board.Id,
            ColumnId = todoColumn.Id,
            Title = "Implement user authentication",
            Description = "Add login and registration functionality using ASP.NET Core Identity",
            Priority = Priority.High,
            AssigneeId = user1.Id,
            DueDate = DateTime.UtcNow.AddDays(7)
        };

        var card2 = new Card
        {
            BoardId = board.Id,
            ColumnId = todoColumn.Id,
            Title = "Design database schema",
            Description = "Create entity models and relationships for the Kanban board",
            Priority = Priority.Medium,
            AssigneeId = user2.Id,
            DueDate = DateTime.UtcNow.AddDays(5)
        };

        var card3 = new Card
        {
            BoardId = board.Id,
            ColumnId = inProgressColumn.Id,
            Title = "Create Kanban board UI",
            Description = "Build drag-and-drop interface for task management",
            Priority = Priority.High,
            AssigneeId = adminUser.Id,
            DueDate = DateTime.UtcNow.AddDays(10)
        };

        var card4 = new Card
        {
            BoardId = board.Id,
            ColumnId = reviewColumn.Id,
            Title = "Write unit tests",
            Description = "Add comprehensive test coverage for core functionality",
            Priority = Priority.Low,
            AssigneeId = user1.Id
        };

        var card5 = new Card
        {
            BoardId = board.Id,
            ColumnId = doneColumn.Id,
            Title = "Setup project structure",
            Description = "Initialize ASP.NET Core project with Clean Architecture",
            Priority = Priority.Medium,
            AssigneeId = adminUser.Id
        };

        context.Cards.AddRange(card1, card2, card3, card4, card5);
        await context.SaveChangesAsync();

        // Add labels to cards
        context.CardLabels.AddRange(
            new CardLabel { CardId = card1.Id, LabelId = featureLabel.Id },
            new CardLabel { CardId = card1.Id, LabelId = highPriorityLabel.Id },
            new CardLabel { CardId = card2.Id, LabelId = featureLabel.Id },
            new CardLabel { CardId = card3.Id, LabelId = featureLabel.Id },
            new CardLabel { CardId = card3.Id, LabelId = highPriorityLabel.Id }
        );

        // Add comments
        context.Comments.AddRange(
            new Comment
            {
                CardId = card1.Id,
                AuthorId = adminUser.Id,
                Body = "Let's use ASP.NET Core Identity for this. Make sure to configure proper password policies."
            },
            new Comment
            {
                CardId = card3.Id,
                AuthorId = user1.Id,
                Body = "I suggest using SortableJS for the drag-and-drop functionality. It's lightweight and works well with ASP.NET Core."
            },
            new Comment
            {
                CardId = card3.Id,
                AuthorId = user2.Id,
                Body = "Great idea! We should also consider adding keyboard shortcuts for accessibility."
            }
        );

        await context.SaveChangesAsync();
    }
} 