using AI.OpenAI;
using Api.AspNetCore.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AI
{
    public class AILanguageClient : HttpClientServiceBase
    {
        private readonly AILanguageConnection connection;
        private string apiToken = null;

        public AILanguageClient(HttpClient httpClient,
            AILanguageConnection connection,
            ILogger<HttpClientServiceBase> logger) : base(httpClient, logger)
        {
            this.connection = connection;
        }

        public void SetApiToken(string apiToken)
        {
            this.apiToken = apiToken;
        }

        public async Task<OpenAIChatCompleteResponse> OpenAIChatCompleteStream(OpenAIChatCompleteRequest request, Func<OpenAIMessage, Task> streamFunction, CancellationToken cancellationToken = default)
        {
            var url = $"{connection.Host}/openai/chat/complete/stream";

            var resultMessage = new OpenAIMessage();
            var result = new OpenAIChatCompleteResponse
            {
                Result = resultMessage
            };

            await StreamAsAsync(url, request, async (OpenAIMessage _) =>
            {
                if (_ == null)
                    return;

                if (!string.IsNullOrEmpty(_.Name))
                    resultMessage.Name = _.Name;
                if (!string.IsNullOrEmpty(_.Role))
                    resultMessage.Name = _.Role;
                if (!string.IsNullOrEmpty(_.Content))
                    resultMessage.Content += _.Content;
                if (_.FunctionCall != null)
                    resultMessage.FunctionCall = _.FunctionCall;
                await streamFunction(_);
            }, cancellationToken,
                _ =>
                {
                    if (!string.IsNullOrEmpty(apiToken))
                        _.Headers.Add("API-Token", apiToken);
                });

            return result;
        }

        public async Task<OpenAIChatCompleteResponse> OpenAIChatComplete(OpenAIChatCompleteRequest request, CancellationToken cancellationToken = default)
        {
            var result = await SendAsAsync<OpenAIChatCompleteResponse>($"{connection.Host}/openai/chat/complete", request, cancellationToken,
                _ =>
                {
                    if (!string.IsNullOrEmpty(apiToken))
                        _.Headers.Add("API-Token", apiToken);
                });
            logger.LogWarning($"AILanguageClient.OpenAIChatComplete: {JsonConvert.SerializeObject(result)}");
            return result;
        }

        public async Task<OpenAIEmbeddingResponse> OpenAIEmbeddingCreate(OpenAIEmbeddingRequest request, CancellationToken cancellationToken = default)
        {
            var result = await SendAsAsync<OpenAIEmbeddingResponse>($"{connection.Host}/openai/embeddings", request, cancellationToken,
                _ =>
                {
                    if (!string.IsNullOrEmpty(apiToken))
                        _.Headers.Add("API-Token", apiToken);
                });
            logger.LogWarning($"AILanguageClient.OpenAIEmbeddingCreate: {JsonConvert.SerializeObject(result)}");
            return result;
        }
    }
}
