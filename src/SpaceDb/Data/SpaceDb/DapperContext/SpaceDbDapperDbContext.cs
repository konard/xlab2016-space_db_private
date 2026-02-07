using Data.Repository.Dapper;
using Microsoft.Extensions.Configuration;

namespace SpaceDb.Data.SpaceDb.DapperContext
{
    public partial class SpaceDbDapperDbContext : DapperDbContext
    {
        public SpaceDbDapperDbContext(IConfiguration configuration)
            : base(configuration)
        {
        }
    }
}
