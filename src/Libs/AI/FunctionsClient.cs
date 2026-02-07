using AI.OpenAI;
using Api.AspNetCore.Services;
using Microsoft.Extensions.Logging;

namespace AI
{
    public class FunctionsClient : HttpClientServiceBase, IAgentMethodCallService2
    {
        private IChannelClientProvider channelClientProvider;
        private readonly ILogger<FunctionsClient> logger;
        private readonly FunctionsConnection connection;

        public FunctionsClient(HttpClient client, ILogger<FunctionsClient> logger, FunctionsConnection connection) : base(client, logger)
        {
            this.logger = logger;
            this.connection = connection;
        }

        public async Task<List<FunctionSchema>> Search(FunctionQuery request)
        {
            var apiUrl = $"{connection.Host}/api/v1/functions/search";

            logger.LogWarning($"Запрос к API Functions {request.AgentId} по адресу {apiUrl}");

            var response = await SendAsAsync<List<FunctionSchema>>(apiUrl, request);

            //if (!response.IsSuccessStatusCode)
            //{
            //    logger.LogError("Ошибка при запросе к API: {StatusCode} - {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
            //    throw new HttpRequestException($"Ошибка при запросе: {response.StatusCode}");
            //}

            //var result = await response.Content.ReadAsAsync<AgentChatCompleteResponse>();

            return response;
        }

        public async Task<AgentMethodCallResponse> Call(AgentMethodCallRequest request)
        {
            var apiUrl = string.IsNullOrEmpty(request.FunctionsUrl) ? 
                $"{connection.Host}/api/v1/functions/call" : request.FunctionsUrl;

            logger.LogWarning($"Запрос к API Functions {request.AgentName}:{request.FunctionName}({request.Args}) по адресу {apiUrl}");

            var response = await SendAsAsync<AgentMethodCallResponse>(apiUrl, request);

            //if (!response.IsSuccessStatusCode)
            //{
            //    logger.LogError("Ошибка при запросе к API: {StatusCode} - {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
            //    throw new HttpRequestException($"Ошибка при запросе: {response.StatusCode}");
            //}

            //var result = await response.Content.ReadAsAsync<AgentChatCompleteResponse>();

            logger.LogInformation("Ответ функции: {Content}", response?.Result);

            logger.LogInformation("Текущее состояние агента: {State}", response.State);

            return response;
        }
    }
}
