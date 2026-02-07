using Data.Repository;
using SpaceDb.Data.SpaceDb.Entities;

namespace SpaceDb.Models.Queries.Users
{
    public partial class UserQuery : QueryBase<User, UserFilter, UserSort>
    {
    }
}
