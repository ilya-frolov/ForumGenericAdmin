using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Dino.Core.AdminBL.Data;
using DinoGenericAdmin.BL.Models;
using System;

namespace DinoGenericAdmin.BL.Data
{
    public class MainDbContext : BaseDbContext<MainDbContext>
    {
        public DbSet<AdminUser> AdminUsers { get; set; }
        public DbSet<AdminRole> AdminRoles { get; set; }

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
        }
    }
}