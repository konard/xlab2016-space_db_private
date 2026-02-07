using AI.OpenAI;
using Api.AspNetCore.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;

namespace AI
{
    public class AgentClient : HttpClientServiceBase
    {
        public AgentClient(HttpClient client, ILogger<AgentClient> logger) : 
            base(client, logger)
        {
        }        

        public async Task<AgentChatCompleteResponse> Complete(AgentChatCompleteRequest request)
        {
            var apiUrl = request.CompleteUrl;
            // prevent servers cycle
            request.CompleteUrl = null;

            logger.LogInformation("Запрос к API для агента {AgentName} по адресу {Url}", request.Agent?.AgentName, apiUrl);

            var response = await SendAsAsync<AgentChatCompleteResponse>(apiUrl, request);
                        
            return response;
        }
    }
}
