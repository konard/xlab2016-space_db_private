
namespace SpaceDb.Mappings
{
    public partial class DbMapContext
    {
        public UserMap UserMap { get; }
        public RoleMap RoleMap { get; }
        public UserRoleMap UserRoleMap { get; }
        public TenantMap TenantMap { get; }
        public SingularityMap SingularityMap { get; }
        public WorkflowLogMap WorkflowLogMap { get; }

        public DbMapContext()
        {
            UserMap = new UserMap(this);
            RoleMap = new RoleMap(this);
            UserRoleMap = new UserRoleMap(this);
            TenantMap = new TenantMap(this);
            SingularityMap = new SingularityMap(this);
            WorkflowLogMap = new WorkflowLogMap(this);
        }
    }
}
