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

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Post-Tag many-to-many
            builder.Entity<BlogPlatform.Models.PostTag>()
                .HasKey(pt => new { pt.PostId, pt.TagId });

            builder.Entity<BlogPlatform.Models.Post>()
                .HasOne(p => p.Author)
                .WithMany()
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
        }
    }
}