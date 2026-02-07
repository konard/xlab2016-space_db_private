using Data.Repository;
using SpaceDb.Data.SpaceDb.Entities;

namespace SpaceDb.Models.Queries.Tenants
{
    public partial class TenantQuery : QueryBase<Tenant, TenantFilter, TenantSort>
    {
    }
}
