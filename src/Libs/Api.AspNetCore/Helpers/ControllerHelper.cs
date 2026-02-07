using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System;
using System.Threading.Tasks;

namespace Api.AspNetCore.Helpers
{
    public static class ControllerHelper
    {
        public static async Task<bool> ReadMultipartRequest(this ControllerBase controller,
            Func<ContentDispositionHeaderValue, MultipartSection, Task> readSection,
            ILogger<ControllerBase> logger = null)
        {
            var boundary = MultipartRequestHelper.GetBoundary(
                MediaTypeHeaderValue.Parse(controller.Request.ContentType),
                70);
            var reader = new MultipartReader(boundary, controller.HttpContext.Request.Body);
            var section = await reader.ReadNextSectionAsync();

            logger?.LogInformation("Reading multipart sections...");

            while (section != null)
            {
                var hasContentDispositionHeader =
                    ContentDispositionHeaderValue.TryParse(
                        section.ContentDisposition, out var contentDisposition);

                if (hasContentDispositionHeader)
                {
                    if (!MultipartRequestHelper
                        .HasFileContentDisposition(contentDisposition))
                    {
                        var message = $"The request couldn't be processed (Error 2).";
                        controller.ModelState.AddModelError("File",
                            message);
                        // Log error
                        logger?.LogError(message);

                        return false;
                    }

                    logger?.LogInformation($"{section.ContentType}, {section.ContentDisposition}, {section.BaseStreamOffset}");

                    await readSection(contentDisposition, section);
                }

                section = await reader.ReadNextSectionAsync();
            }

            return true;
        }
    }
}
