using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class BoardMemberConfiguration : IEntityTypeConfiguration<BoardMember>
{
	public void Configure(EntityTypeBuilder<BoardMember> builder)
	{
		builder.HasKey(bm => new { bm.BoardId, bm.UserId });

		// Relationships
		builder.HasOne(bm => bm.Board)
			.WithMany(b => b.Members)
			.HasForeignKey(bm => bm.BoardId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(bm => bm.User)
			.WithMany(u => u.BoardMemberships)
			.HasForeignKey(bm => bm.UserId)
			.OnDelete(DeleteBehavior.Cascade);
	}
} 