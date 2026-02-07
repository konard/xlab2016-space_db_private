using Api.AspNetCore.Models.Scope;

namespace Api.AspNetCore.Models
{
    public class RequestContext<TRequest>
    {
        public TRequest Request { get; set; }
        public IAuthorizationData Authorization { get; set; }
    }
}
