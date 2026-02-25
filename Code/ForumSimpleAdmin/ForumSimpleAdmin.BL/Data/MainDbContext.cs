using Dino.Core.AdminBL.Data;
using ForumSimpleAdmin.BL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ForumSimpleAdmin.BL.Data
{
    public class MainDbContext : BaseDbContext<MainDbContext>
    {
        public DbSet<AdminUser> AdminUsers { get; set; }
        public DbSet<AdminRole> AdminRoles { get; set; }
        public DbSet<ForumUser> ForumUsers { get; set; }
        public DbSet<ForumSimpleAdmin.BL.Models.Forum> Forums { get; set; }
        public DbSet<ForumPost> ForumPosts { get; set; }
        public DbSet<ForumComment> ForumComments { get; set; }
        public DbSet<ForumSession> ForumSessions { get; set; }
        public DbSet<SiteSettings> SiteSettings { get; set; }

        public MainDbContext() : base()
        {
        }

        public MainDbContext(IConfiguration config) : base(config)
        {
        }

        public MainDbContext(DbContextOptions<MainDbContext> options, IConfiguration config)
            : base(options, config)
        {
        }

        protected override (Type userType, Type roleType) ConfigureAdminEntities(ModelBuilder modelBuilder)
        {
            return (typeof(AdminUser), typeof(AdminRole));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ForumSimpleAdmin.BL.Models.Forum>(entity =>
            {
                entity.ToTable("Forums");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Name).IsRequired().HasMaxLength(100);
                entity.HasIndex(x => x.SortIndex);
                entity.HasIndex(x => x.IsDeleted);
            });

            modelBuilder.Entity<ForumUser>(entity =>
            {
                entity.ToTable("ForumUsers");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Name).IsRequired().HasMaxLength(100);
                entity.Property(x => x.PasswordHash).IsRequired().HasMaxLength(256);
                entity.Property(x => x.ProfilePicturePath).IsRequired(false).HasMaxLength(300);
                entity.HasIndex(x => x.Name).IsUnique();
                entity.HasIndex(x => x.IsDeleted);
            });

            modelBuilder.Entity<ForumPost>(entity =>
            {
                entity.ToTable("ForumPosts");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Title).IsRequired().HasMaxLength(200);
                entity.Property(x => x.Content).IsRequired();
                entity.HasIndex(x => x.ForumId);
                entity.HasIndex(x => x.UserId);
                entity.HasIndex(x => x.IsDeleted);
                entity.HasIndex(x => x.CreateDate);

                entity.HasOne(x => x.Forum)
                    .WithMany(x => x.Posts)
                    .HasForeignKey(x => x.ForumId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.User)
                    .WithMany(x => x.Posts)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ForumComment>(entity =>
            {
                entity.ToTable("ForumComments");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Content).IsRequired().HasMaxLength(1000);
                entity.HasIndex(x => x.PostId);
                entity.HasIndex(x => x.UserId);
                entity.HasIndex(x => x.IsDeleted);
                entity.HasIndex(x => x.CreateDate);

                entity.HasOne(x => x.Post)
                    .WithMany(x => x.Comments)
                    .HasForeignKey(x => x.PostId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.User)
                    .WithMany(x => x.Comments)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ForumSession>(entity =>
            {
                entity.ToTable("ForumSessions");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Token).IsRequired().HasMaxLength(100);
                entity.HasIndex(x => x.Token).IsUnique();
                entity.HasIndex(x => x.ExpirationDate);

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SiteSettings>(entity =>
            {
                entity.ToTable("SiteSettings");
                entity.HasKey(x => x.Id);
            });
        }
    }
}
