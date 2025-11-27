using Microsoft.EntityFrameworkCore;
using YPrompt.Api.Models.Entities;

namespace YPrompt.Api.Data;

public class YPromptDbContext : DbContext
{
    public YPromptDbContext(DbContextOptions<YPromptDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Prompt> Prompts { get; set; }
    public DbSet<PromptVersion> PromptVersions { get; set; }
    public DbSet<PromptTag> PromptTags { get; set; }
    public DbSet<PromptShare> PromptShares { get; set; }
    public DbSet<UserPromptRules> UserPromptRules { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User entity configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.LinuxDoId).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.AuthType);
            entity.HasIndex(e => e.IsActive);
        });

        // Prompt entity configuration
        modelBuilder.Entity<Prompt>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsFavorite);
            entity.HasIndex(e => e.IsPublic);
            entity.HasIndex(e => e.CreateTime);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Prompts)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PromptVersion entity configuration
        modelBuilder.Entity<PromptVersion>(entity =>
        {
            entity.HasIndex(e => new { e.PromptId, e.VersionNumber }).IsUnique();
            entity.HasIndex(e => e.PromptId);
            entity.HasIndex(e => e.CreatedBy);
            entity.HasIndex(e => e.CreateTime);

            entity.HasOne(e => e.Prompt)
                .WithMany(p => p.Versions)
                .HasForeignKey(e => e.PromptId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Creator)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // PromptTag entity configuration
        modelBuilder.Entity<PromptTag>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.TagName }).IsUnique();
            entity.HasIndex(e => e.UserId);

            entity.HasOne(e => e.User)
                .WithMany(u => u.PromptTags)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PromptShare entity configuration
        modelBuilder.Entity<PromptShare>(entity =>
        {
            entity.HasIndex(e => e.ShareCode).IsUnique();
            entity.HasIndex(e => e.PromptId);

            entity.HasOne(e => e.Prompt)
                .WithMany(p => p.Shares)
                .HasForeignKey(e => e.PromptId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserPromptRules entity configuration
        modelBuilder.Entity<UserPromptRules>(entity =>
        {
            entity.HasIndex(e => e.UserId).IsUnique();

            entity.HasOne(e => e.User)
                .WithOne(u => u.UserPromptRules)
                .HasForeignKey<UserPromptRules>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserSession entity configuration
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.TokenHash);
            entity.HasIndex(e => e.ExpireTime);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
