using Data.Repository;
using SpaceDb.Data.SpaceDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace SpaceDb.Data.SpaceDb.DatabaseContext
{
    public partial class SpaceDbContext : DbContext
    {
        public DbSet<User>? Users { get; set; }
        public DbSet<Role>? Roles { get; set; }
        public DbSet<UserRole>? UserRoles { get; set; }
        public DbSet<Tenant>? Tenants { get; set; }
        public DbSet<Singularity>? Singularities { get; set; }
        public DbSet<WorkflowLog>? WorkflowLogs { get; set; }

        public SpaceDbContext(DbContextOptions<SpaceDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new UsersConfiguration { IsInMemoryDb = this.IsInMemoryDb() });
            modelBuilder.ApplyConfiguration(new RolesConfiguration());
            modelBuilder.ApplyConfiguration(new UserRolesConfiguration());
            modelBuilder.ApplyConfiguration(new TenantsConfiguration { IsInMemoryDb = this.IsInMemoryDb() });
            modelBuilder.ApplyConfiguration(new SingularitiesConfiguration());
            modelBuilder.ApplyConfiguration(new WorkflowLogsConfiguration());
        }
    }
}
