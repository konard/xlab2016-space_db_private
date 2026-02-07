using SpaceDb.Models;
using Api.AspNetCore.Services;

namespace SpaceDb.Services
{
    public interface IMicroserviceAuthorizeService : IAuthorizeService
    {
        Task<MicroserviceAuthorizationData> AuthorizeData(Action<MicroserviceAuthorizationData> action);
    }
}
