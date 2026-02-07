using SpaceDb.Data.SpaceDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SpaceDb.Data.SpaceDb.DatabaseContext
{
    public class UsersConfiguration : IEntityTypeConfiguration<User>
    {
        public bool IsInMemoryDb { get; set; }

        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(x => x.Id);

            if (!IsInMemoryDb)
            {
                builder.Property(_ => _.RefreshToken).HasColumnType("jsonb");
            }
            else
            {
                builder.Ignore(_ => _.RefreshToken);
            }
        }
    }

    public class RolesConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.HasKey(x => x.Id);
        }
    }

    public class UserRolesConfiguration : IEntityTypeConfiguration<UserRole>
    {
        public void Configure(EntityTypeBuilder<UserRole> builder)
        {
            builder.HasKey(x => x.Id);
        }
    }

    public class TenantsConfiguration : IEntityTypeConfiguration<Tenant>
    {
        public bool IsInMemoryDb { get; set; }

        public void Configure(EntityTypeBuilder<Tenant> builder)
        {
            builder.HasKey(x => x.Id);

            if (!IsInMemoryDb)
            {
                builder.Property(_ => _.Logo).HasColumnType("jsonb");
            }
            else
            {
                builder.Ignore(_ => _.Logo);
            }
        }
    }

    public class SingularitiesConfiguration : IEntityTypeConfiguration<Singularity>
    {
        public void Configure(EntityTypeBuilder<Singularity> builder)
        {
            builder.HasKey(x => x.Id);
        }
    }

    public class WorkflowLogsConfiguration : IEntityTypeConfiguration<WorkflowLog>
    {
        public void Configure(EntityTypeBuilder<WorkflowLog> builder)
        {
            builder.HasKey(x => x.Id);
        }
    }
}
