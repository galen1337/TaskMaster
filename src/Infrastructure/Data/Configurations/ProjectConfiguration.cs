using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Common;

namespace Infrastructure.Data.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");
        
        builder.HasKey(p => p.Id);
        
        		builder.Property(p => p.Name)
			.IsRequired()
			.HasMaxLength(ValidationConstants.ProjectNameMaxLength);
			
		builder.Property(p => p.Description)
			.HasMaxLength(ValidationConstants.ProjectDescriptionMaxLength);
            
        builder.Property(p => p.OwnerId)
            .IsRequired();
            
        builder.Property(p => p.CreatedAt)
            .IsRequired();
        
        // Relationships
        builder.HasOne(p => p.Owner)
            .WithMany()
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasMany(p => p.Boards)
            .WithOne(b => b.Project)
            .HasForeignKey(b => b.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasMany(p => p.Labels)
            .WithOne(l => l.Project)
            .HasForeignKey(l => l.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasMany(p => p.Members)
            .WithOne(pm => pm.Project)
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasMany(p => p.Invites)
            .WithOne(i => i.Project)
            .HasForeignKey(i => i.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
} 