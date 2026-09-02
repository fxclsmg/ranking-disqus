using DisqusAnalytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DisqusAnalytics.Persistence.Data;

public sealed class DisqusDbContext : DbContext
{
    public DisqusDbContext(
        DbContextOptions<DisqusDbContext> options)
        : base(options)
    {
    }

    public DbSet<Forum> Forums => Set<Forum>();

    public DbSet<Discussion> Discussions => Set<Discussion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureForum(modelBuilder);
        ConfigureDiscussion(modelBuilder);
    }

    private static void ConfigureForum(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Forum>(entity =>
        {
            entity.ToTable("Forums");

            entity.HasKey(forum => forum.Id);

            entity.Property(forum => forum.ShortName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(forum => forum.Name)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(forum => forum.Url)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(forum => forum.CreatedAt)
                .IsRequired();

            entity.Property(forum => forum.LastSyncAt);
        });
    }

    private static void ConfigureDiscussion(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Discussion>(entity =>
        {
            entity.ToTable("Discussions");

            entity.HasKey(discussion => discussion.Id);

            entity.Property(discussion => discussion.Title)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(discussion => discussion.Link)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(discussion => discussion.Slug)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(discussion => discussion.CommentCount)
                .IsRequired();

            entity.Property(discussion => discussion.CreatedAt)
                .IsRequired();

            entity.Property(discussion => discussion.LastPostAt);

            entity.Property(discussion => discussion.IsClosed)
                .IsRequired();

            entity.Property(discussion => discussion.IsDeleted)
                .IsRequired();

            entity.Property(discussion => discussion.IsRelevant)
                .IsRequired();

            entity.HasOne(discussion => discussion.Forum)
                .WithMany(forum => forum.Discussions)
                .HasForeignKey(discussion => discussion.ForumId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
