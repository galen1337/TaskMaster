using System.Security.Claims;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TaskMaster.Controllers;

[Authorize]
public class InvitesController : Controller
{
	private readonly ApplicationDbContext _context;
	private readonly UserManager<ApplicationUser> _userManager;

	public InvitesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
	{
		_context = context;
		_userManager = userManager;
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Send(int projectId, string email)
	{
		string? senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (string.IsNullOrEmpty(senderId)) return Challenge();

		bool canManage = await _context.ProjectMembers
			.AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == senderId && (pm.Role == ProjectRole.Owner || pm.Role == ProjectRole.Admin));
		bool isPlatformAdmin = User.IsInRole("Admin");
		if (!canManage && !isPlatformAdmin) return Forbid();

		if (string.IsNullOrWhiteSpace(email))
		{
			TempData["Error"] = "Email is required";
			return RedirectToAction("Details", "Projects", new { id = projectId });
		}

		var token = Guid.NewGuid().ToString("N");
		var invite = new Invite
		{
			ProjectId = projectId,
			InvitedEmail = email.Trim(),
			InvitedByUserId = senderId,
			Token = token,
			ExpiresAt = DateTime.UtcNow.AddDays(7),
			Status = InviteStatus.Pending,
			CreatedAt = DateTime.UtcNow
		};
		_context.Invites.Add(invite);
		await _context.SaveChangesAsync();

		TempData["Success"] = "Invite created";
		return RedirectToAction("Details", "Projects", new { id = projectId });
	}

	[AllowAnonymous]
	public async Task<IActionResult> Accept(string token)
	{
		if (string.IsNullOrWhiteSpace(token)) return BadRequest();

		var invite = await _context.Invites.FirstOrDefaultAsync(i => i.Token == token);
		if (invite == null || invite.Status != InviteStatus.Pending || invite.ExpiresAt < DateTime.UtcNow)
		{
			return View("AcceptResult", false);
		}

		if (!User.Identity?.IsAuthenticated ?? true)
		{
			// Redirect to login with returnUrl back to this action
			return Challenge(new AuthenticationProperties { RedirectUri = Url.Action(nameof(Accept), new { token }) }, IdentityConstants.ApplicationScheme);
		}

		string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

		bool alreadyMember = await _context.ProjectMembers.AnyAsync(pm => pm.ProjectId == invite.ProjectId && pm.UserId == userId);
		if (!alreadyMember)
		{
			_context.ProjectMembers.Add(new ProjectMember
			{
				ProjectId = invite.ProjectId,
				UserId = userId,
				Role = ProjectRole.Member,
				JoinedAt = DateTime.UtcNow
			});
		}

		invite.Status = InviteStatus.Accepted;
		await _context.SaveChangesAsync();

		return View("AcceptResult", true);
	}
} 