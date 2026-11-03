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

    public DbSet<Author> Authors => Set<Author>();

    public DbSet<Comment> Comments => Set<Comment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureForum(modelBuilder);
        ConfigureDiscussion(modelBuilder);
        ConfigureAuthor(modelBuilder);
        ConfigureComment(modelBuilder);
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

            entity.Property(discussion => discussion.CommentsSyncedAt);

            entity.Property(discussion => discussion.SyncedCommentCount)
                .IsRequired();

            entity.HasOne(discussion => discussion.Forum)
                .WithMany(forum => forum.Discussions)
                .HasForeignKey(discussion => discussion.ForumId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureAuthor(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Author>(entity =>
        {
            entity.ToTable("Author");

            // Os identificadores vêm do Disqus, portanto nunca são
            // gerados pelo banco. Isso permite também utilizar o
            // identificador de autor anônimo (-1).
            entity.HasKey(author => author.Id);

            entity.Property(author => author.Id)
                .ValueGeneratedNever();

            entity.Property(author => author.Name)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(author => author.Username)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(author => author.ProfileUrl)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(author => author.AvatarUrl)
                .IsRequired()
                .HasMaxLength(1000);
        });
    }

    private static void ConfigureComment(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.ToTable("Comment");

            entity.HasKey(comment => comment.Id);

            entity.Property(comment => comment.Id)
                .ValueGeneratedNever();

            entity.Property(comment => comment.Message)
                .IsRequired();

            entity.Property(comment => comment.CreatedAt)
                .IsRequired();

            entity.Property(comment => comment.CharacterCount)
                .IsRequired();

            entity.Property(comment => comment.IsDeleted)
                .IsRequired();

            entity.Property(comment => comment.IsSpam)
                .IsRequired();

            entity.HasIndex(comment => comment.DiscussionId);

            entity.HasIndex(comment => comment.AuthorId);

            entity.HasOne(comment => comment.Discussion)
                .WithMany(discussion => discussion.Comments)
                .HasForeignKey(comment => comment.DiscussionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(comment => comment.Author)
                .WithMany(author => author.Comments)
                .HasForeignKey(comment => comment.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
