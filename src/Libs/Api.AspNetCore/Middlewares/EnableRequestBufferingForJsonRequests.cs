using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Api.AspNetCore.Middlewares
{
    public class EnableRequestBufferingForJsonRequests
    {
        // see https://github.com/dotnet/aspnetcore/blob/4ef204e13b88c0734e0e94a1cc4c0ef05f40849e/src/Http/Http/src/Internal/BufferingHelper.cs#L12
        private const long DefaultBufferThreshold = 1024 * 30; //30kb
        private RequestDelegate _next;

        public EnableRequestBufferingForJsonRequests(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public Task Invoke(HttpContext httpContext)
        {
            var request = httpContext.Request;

            // NOTE:  We are enabling request buffering just to read a request body
            //        But to avoid performance/memory issues enable only if body is json
            //       and less then 30kb (the default threshold for buffering data in memory, otherwise asp.net will write requests to disk)
            if (
                request.ContentLength < DefaultBufferThreshold
                && !string.IsNullOrEmpty(request.ContentType)
                && request.ContentType.StartsWith("application/json", StringComparison.Ordinal))
            {
                request.EnableBuffering();
            }

            return _next(httpContext);
        }
    }
}
