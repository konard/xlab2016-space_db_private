using AI.Client.Configuration;
using AI.OpenAI;
using Api.AspNetCore.Models.Secure;
using Api.AspNetCore.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;

namespace AI.Client
{
    public class QAiClient : HttpClientServiceBase
    {
        private readonly QAiConnection connection;
        private readonly QAiFilesConnection filesConnection;
        private readonly string apiToken;
        private DateTime? tokenExpiresAt;

        public QAiClient(HttpClient httpClient, ILogger<HttpClientServiceBase> logger, QAiConnection connection, 
            QAiFilesConnection filesConnection) : base(httpClient, logger)
        {
            this.connection = connection;
            this.filesConnection = filesConnection;
            this.apiToken = connection.ApiToken;
        }

        public async Task<DownloadAgentFileResponse> DownloadFile(DownloadAgentFileRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureAuthenticate(cancellationToken);

            var apiUrl = $"{filesConnection.Host}/api/v1/agent/files/download";

            logger.LogInformation($"Запрос к API files для файла {request.File?.FilePath}, {request.File?.Size} по адресу {apiUrl}");

            var response = await SendAsAsync<DownloadAgentFileResponse>(apiUrl, request);

            return response;
        }

        public async Task<RecognizeResponse> RecognizeWord(RecognizeRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureAuthenticate(cancellationToken);

            var apiUrl = $"{connection.Host}/api/v1/recognize/word";

            logger.LogInformation($"Запрос к API recognize по адресу {apiUrl}");

            var response = await SendAsAsync<RecognizeResponse>(apiUrl, request);
            return response;
        }

        public async Task<RecognizeResponse> RecognizePdf(RecognizeRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureAuthenticate(cancellationToken);

            var apiUrl = $"{connection.Host}/api/v1/recognize/pdf";

            logger.LogInformation($"Запрос к API recognize по адресу {apiUrl}");

            var response = await SendAsAsync<RecognizeResponse>(apiUrl, request);
            return response;
        }

        public async Task<List<AIEmbedding>> CreateEmbeddings(CreateEmbeddingsRequest request, CancellationToken cancellationToken = default)
        {
            request.ApiToken = apiToken;
            await EnsureAuthenticate(cancellationToken);

            var apiUrl = $"{connection.Host}/api/v1/embeddings/create";

            logger.LogInformation($"Запрос к API embeddings по адресу {apiUrl}");

            var response = await SendAsAsync<List<AIEmbedding>>(apiUrl, request);
            return response;
        }

        public async Task<List<AIEmbedding>> SearchEmbeddings(SearchEmbeddingsRequest request, CancellationToken cancellationToken = default)
        {
            request.ApiToken = apiToken;
            await EnsureAuthenticate(cancellationToken);

            var apiUrl = $"{connection.Host}/api/v1/embeddings/search/cosineSimilarity";

            logger.LogInformation($"Запрос к API embeddings по адресу {apiUrl}");

            var response = await SendAsAsync<List<AIEmbedding>>(apiUrl, request);
            return response;
        }

        public async Task<OpenAIChatCompleteResponse> AIComplete(OpenAIChatCompleteRequest request, CancellationToken cancellationToken = default)
        {
            request.ApiToken = apiToken;
            await EnsureAuthenticate(cancellationToken);

            var apiUrl = $"{connection.Host}/api/v1/ai/chat/complete";

            logger.LogInformation($"Запрос к API AI по адресу {apiUrl}");

            var response = await SendAsAsync<OpenAIChatCompleteResponse>(apiUrl, request);

            return response;
        }

        public async Task<OpenAIStructuredResponse<T>> AIStructured<T>(OpenAIChatCompleteRequest request, CancellationToken cancellationToken = default)
            where T : class
        {
            var result = await AIComplete(request, cancellationToken);

            return new OpenAIStructuredResponse<T>
            {
                Result = result.Result,
            };
        }

        public async Task<AgentChatCompleteResponse> Complete(AgentChatCompleteRequest request, CancellationToken cancellationToken = default)
        {
            request.ApiToken = apiToken;
            await EnsureAuthenticate(cancellationToken);

            var apiUrl = $"{connection.Host}/api/v1/agents/chat/complete";

            logger.LogInformation($"Запрос к API для агента {request.AgentName} по адресу {apiUrl}");

            var response = await SendAsAsync<AgentChatCompleteResponse>(apiUrl, request);

            return response;
        }

        private async Task EnsureAuthenticate(CancellationToken cancellationToken)
        {
            // Проверяем наличие токена и его срок действия
            if (!string.IsNullOrEmpty(AuthToken) && 
                tokenExpiresAt.HasValue && 
                DateTime.UtcNow < tokenExpiresAt.Value.AddMinutes(-5)) // Обновляем за 5 минут до истечения
            {
                return;
            }

            var authUrl = $"{connection.Host}/api/v1/authenticate";

            await Authenticate(authUrl, JsonConvert.SerializeObject(new QAiAuthRequest
            {
                Username = connection.UserName,
                Password = connection.Password
            }), async _ =>
            {
                var token = await _.Content.ReadAsAsync<JwtToken>();
                tokenExpiresAt = token.Expires;
                return token.Key;
            }, cancellationToken);
        }

    }
}
