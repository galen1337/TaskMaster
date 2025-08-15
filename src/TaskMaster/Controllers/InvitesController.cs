using System.Security.Claims;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace TaskMaster.Controllers;

[Authorize]
public class InvitesController : Controller
{
	private readonly IInviteService _invites;
	private readonly UserManager<ApplicationUser> _userManager;

	public InvitesController(IInviteService invites, UserManager<ApplicationUser> userManager)
	{
		_invites = invites;
		_userManager = userManager;
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Send(int projectId, string email)
	{
		string? senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (string.IsNullOrEmpty(senderId)) return Challenge();
		try
		{
			bool isPlatformAdmin = User.IsInRole("Admin");
			await _invites.SendInviteAsync(projectId, email, senderId, isPlatformAdmin);
			TempData["Success"] = "Invite created";
		}
		catch (Exception ex)
		{
			TempData["Error"] = ex.Message;
		}
		return RedirectToAction("Details", "Projects", new { id = projectId });
	}

	public async Task<IActionResult> Inbox()
	{
		string? email = _userManager.GetEmailAsync(await _userManager.GetUserAsync(User)).Result;
		var invites = await _invites.GetInboxAsync(email ?? string.Empty);
		return View(invites);
	}

	[AllowAnonymous]
	public async Task<IActionResult> Accept(string token)
	{
		if (string.IsNullOrWhiteSpace(token)) return BadRequest();

		if (!User.Identity?.IsAuthenticated ?? true)
		{
			return Challenge(new AuthenticationProperties { RedirectUri = Url.Action(nameof(Accept), new { token }) }, IdentityConstants.ApplicationScheme);
		}

		string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
		bool ok = await _invites.AcceptAsync(token, userId);
		return View("AcceptResult", ok);
	}
} 