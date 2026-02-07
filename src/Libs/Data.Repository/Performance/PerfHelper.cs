using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repository.Performance
{
    public static class PerfHelper
    {
        public static async Task<T> RunAsync<T>(string message, Func<Task<T>> target, ILogger logger, Guid correlationId)
        {
            try
            {
                var correlationIdString = correlationId.ToString();
                logger.LogInformation("Will " + message + ": " + correlationIdString);

                var stopWatch = Stopwatch.StartNew();

                var result = await target();

                logger.LogInformation($"Did " + message + $" in  {stopWatch.Elapsed.TotalSeconds.ToString("0.0###")}sec: " + correlationIdString);

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, message);
                throw;
            }
        }

        public static T Run<T>(string message, Func<T> target, ILogger logger, Guid correlationId)
        {
            try
            {
                var correlationIdString = correlationId.ToString();
                logger.LogInformation("Will: " + message + ": " + correlationIdString);

                var stopWatch = Stopwatch.StartNew();

                var result = target();

                logger.LogInformation($"Did " + message + $" in  {stopWatch.Elapsed.TotalSeconds.ToString("0.0###")}sec: " + correlationIdString);

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, message);
                throw;
            }
        }
    }
}
