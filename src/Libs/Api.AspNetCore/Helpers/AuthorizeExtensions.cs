using Api.AspNetCore.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Api.AspNetCore.Helpers
{
    public static class AuthorizeExtensions
    {
        public static void AddAuthorize<T>(this IServiceCollection collection)
            where T : class, IAuthorizeService
        {
            collection.AddHttpContextAccessor();
            collection.AddScoped<IAuthorizeService, T>();
        }

        public static void AddAuthorize<TService, T>(this IServiceCollection collection)
            where T : class, TService
            where TService : class, IAuthorizeService
        {
            collection.AddHttpContextAccessor();
            collection.AddScoped<TService, T>();
        }
    }
}
