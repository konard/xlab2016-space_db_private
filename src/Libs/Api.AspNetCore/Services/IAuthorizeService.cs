using Api.AspNetCore.Models.Scope;
using System.Threading.Tasks;

namespace Api.AspNetCore.Services
{
    public interface IAuthorizeService
    {
        Task<IAuthorizationData> IsAuthorized();
    }
}
