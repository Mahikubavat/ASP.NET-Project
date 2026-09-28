using BlogPlatform.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BlogPlatform.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<BlogPlatform.Models.Post>? Posts { get; set; }
        public DbSet<BlogPlatform.Models.Category>? Categories { get; set; }
        public DbSet<BlogPlatform.Models.Tag>? Tags { get; set; }
        public DbSet<BlogPlatform.Models.PostTag>? PostTags { get; set; }
        public DbSet<BlogPlatform.Models.Comment>? Comments { get; set; }
        public DbSet<BlogPlatform.Models.Like>? Likes { get; set; }
        public DbSet<BlogPlatform.Models.Bookmark>? Bookmarks { get; set; }
        public DbSet<BlogPlatform.Models.Notification>? Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Post-Tag many-to-many
            builder.Entity<BlogPlatform.Models.PostTag>()
                .HasKey(pt => new { pt.PostId, pt.TagId });

            // Post - Author (ApplicationUser.Posts)
            builder.Entity<BlogPlatform.Models.Post>()
                .HasOne(p => p.Author)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.AuthorId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<BlogPlatform.Models.Post>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Posts)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<BlogPlatform.Models.Tag>()
                .HasIndex(t => t.Name)
                .IsUnique();

            builder.Entity<BlogPlatform.Models.Category>()
                .HasIndex(c => c.Name)
                .IsUnique();

            // Comment - Author
            builder.Entity<BlogPlatform.Models.Comment>()
                .HasOne(c => c.Author)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Comment - Reply self-reference
            builder.Entity<BlogPlatform.Models.Comment>()
                .HasOne(c => c.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Like composite key and relations
            builder.Entity<BlogPlatform.Models.Like>()
                .HasKey(l => new { l.PostId, l.UserId });

            builder.Entity<BlogPlatform.Models.Like>()
                .HasOne(l => l.Post)
                .WithMany(p => p.Likes)
                .HasForeignKey(l => l.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<BlogPlatform.Models.Like>()
                .HasOne(l => l.User)
                .WithMany(u => u.Likes)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Notification relations
            builder.Entity<BlogPlatform.Models.Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BlogPlatform.Models.Notification>()
                .HasOne(n => n.TriggeredByUser)
                .WithMany()
                .HasForeignKey(n => n.TriggeredByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}