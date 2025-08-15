namespace Application.Services;

using Domain.Entities;

public interface IInviteService
{
	Task<IReadOnlyList<Invite>> GetInboxAsync(string userEmail);
	Task<bool> AcceptAsync(string token, string userId);
} 