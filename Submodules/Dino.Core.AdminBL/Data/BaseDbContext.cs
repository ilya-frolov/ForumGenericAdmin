using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Dino.Core.AdminBL.Models;
using System;

namespace Dino.Core.AdminBL.Data
{
    public abstract class BaseAdminDbContext : DbContext
    {
        internal IConfiguration _config;

        // Make settings publicly accessible
        public DbSet<Setting> Settings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure Settings table
            modelBuilder.Entity<Setting>(entity =>
            {
                entity.ToTable("Settings");
                entity.HasKey(e => e.ClassName);

                entity.Property(e => e.ClassName).IsRequired().HasMaxLength(100).HasColumnType("VARCHAR(100)");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100).HasColumnType("NVARCHAR(100)");
                entity.Property(e => e.Data).IsRequired().HasColumnType("NVARCHAR(MAX)");
                entity.Property(e => e.CreateDate).IsRequired().HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdateDate).IsRequired().HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdateBy).IsRequired().HasDefaultValue(1);
            });

            // Let derived classes configure concrete entity types first
            (Type userType, Type roleType) = ConfigureAdminEntities(modelBuilder);
            
            // Then configure base admin entity configurations with the concrete types
            ConfigureBaseAdminEntities(modelBuilder, userType, roleType);
        }
        
        // Configure base model classes with concrete types
        protected virtual void ConfigureBaseAdminEntities(ModelBuilder modelBuilder, Type userType, Type roleType)
        {
            // Default to base types if no concrete types provided
            Type effectiveUserType = userType;
            Type effectiveRoleType = roleType;
            
            // Configure AdminRole entity - using either concrete or base type
            var roleBuilder = modelBuilder.Entity(effectiveRoleType);

            // Only configure the table if we're using the base type
            // For derived types, this is handled in ConfigureAdminEntities
            roleBuilder.ToTable("AdminRoles");
            roleBuilder.HasKey("Id");
        
            // Common properties for all role types
            roleBuilder.Property("Name").IsRequired().HasMaxLength(100);
            roleBuilder.Property("Description").HasMaxLength(255).IsRequired(false);
            roleBuilder.Property("RoleType").IsRequired();
            roleBuilder.Property("IsVisible").IsRequired().HasDefaultValue(true);
            roleBuilder.Property("IsSystemDefined").IsRequired().HasDefaultValue(false);

            // Configure AdminUser entity - using either concrete or base type
            var userBuilder = modelBuilder.Entity(effectiveUserType);

            // Only configure the table if we're using the base type
            // For derived types, this is handled in ConfigureAdminEntities
            userBuilder.ToTable("AdminUsers");
            userBuilder.HasKey("Id");
            
            // Common properties for all user types
            userBuilder.Property("Email").IsRequired().HasMaxLength(256);
            userBuilder.Property("FullName").IsRequired().HasMaxLength(100);
            userBuilder.Property("Phone").HasMaxLength(50).IsRequired(false);
            userBuilder.Property("PasswordHash").IsRequired();
            userBuilder.Property("PasswordSalt").IsRequired();
            userBuilder.Property("EmailVerificationCode").HasMaxLength(100).IsRequired(false);
            userBuilder.Property("VerificationCodeDate").IsRequired(false);
            userBuilder.Property("PictureUrl").HasMaxLength(255).IsRequired(false);
            userBuilder.Property("Active").IsRequired().HasDefaultValue(true);
            userBuilder.Property("CreateDate").IsRequired().HasDefaultValueSql("GETDATE()");
            userBuilder.Property("UpdateDate").IsRequired().HasDefaultValueSql("GETDATE()");
            userBuilder.Property("UpdateBy").IsRequired().HasDefaultValue(1);
            userBuilder.Property("LastLoginDate").IsRequired(false);
            userBuilder.Property("LastIpAddress").HasMaxLength(50).IsRequired(false);
            userBuilder.Property("Archived").IsRequired().HasDefaultValue(false);

            // Add unique index for email - works for both base and derived types
            userBuilder.HasIndex("Email").IsUnique();
        }
        
        // Method for derived classes to override and configure concrete admin entities
        protected abstract (Type userType, Type roleType) ConfigureAdminEntities(ModelBuilder modelBuilder);
    }

    public abstract class BaseDbContext<T> : BaseAdminDbContext where T : BaseDbContext<T>
    {
        public BaseDbContext()
        {
        }

        protected BaseDbContext(IConfiguration config)
        {
            _config = config;
        }

        public BaseDbContext(DbContextOptions<T> options, IConfiguration config)
        {
            _config = config;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseLazyLoadingProxies()
                    .UseSqlServer(_config.GetConnectionString("MainDbContext"));
            }
        }
    }
} 