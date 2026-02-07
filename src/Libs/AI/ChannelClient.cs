using Api.AspNetCore.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Threading;

namespace AI
{
    public class ChannelClient : HttpClientServiceBase
    {
        private IChannelClientProvider channelClientProvider;
        private readonly ILogger<ChannelClient> logger;
        private readonly AgentConnection agentConnection;        

        public DateTime? UpdatedTime { get; set; }

        public string ApiToken { get; set; }
        public int? ChannelId { get; set; }
        public bool IsStream { get; set; }

        public ChannelClient(HttpClient client, ILogger<ChannelClient> logger, AgentConnection agentConnection) : base(client, logger)
        {
            this.logger = logger;
            this.agentConnection = agentConnection;
        }

        public async Task Start(IChannelClientProvider channelClientProvider, CancellationToken cancellationToken)
        {
            this.channelClientProvider = channelClientProvider;
            await channelClientProvider.StartReceivingAsync(cancellationToken, OnMessageReceivedAsync, OnErrorAsync);
        }

        public async Task Wait(CancellationToken cancellationToken, Func<Task> ping = null)
        {
            await channelClientProvider.MonitorConnectionAsync(cancellationToken, ping);
        }

        public async Task Stop()
        {
            await channelClientProvider.Stop();
        }

        private async Task OnMessageReceivedAsync(MessageInfo messageInfo, CancellationToken cancellationToken)
        {
            if (messageInfo.Text == null) return;

            //var streamSemaphore = new SemaphoreSlim(1, 1);
            var cancellation = new CancellationTokenSource();

            try
            {
                var completeRequest = new ChannelChatCompleteRequest
                {
                    ApiToken = ApiToken,
                    ChannelId = ChannelId.Value,
                    ChatId = messageInfo.ChatId.ToString(),
                    Username = messageInfo.Username,
                    Text = messageInfo.Text,
                    Contact = messageInfo.Contact,
                    File = messageInfo.File,
                    IsCallback = messageInfo.IsCallback
                };

                int? state = null;
                if (messageInfo.Text == "/start")
                {
                    completeRequest.AgentStateId = (int)AgentStateIds.Started;
                }

                var channelState = await State(new ChannelChatStateRequest
                {
                    ApiToken = ApiToken,
                    ChannelId = ChannelId.Value,
                    ChatId = messageInfo.ChatId.ToString(),
                    Username = messageInfo.Username,
                    Text = messageInfo.Text,
                    Contact = messageInfo.Contact,
                    File = messageInfo.File
                });
                var options = channelState.Agent?.Options;

                var thinkText = "...";

                if (options?.UseStartComplete == true && messageInfo.Text != "/start")
                {
                    var startCompleteResponse = await StartComplete(completeRequest);
                    if (startCompleteResponse?.StartThinkingText != null)
                    {
                        thinkText = startCompleteResponse.StartThinkingText;
                    }
                }

                int? thinkMessageId = options == null || options?.ThinkProgress == true ? 
                    await channelClientProvider.SendTextMessageAsync(messageInfo.ChatId, thinkText, null, (int)ParseModeIds.Html, false, cancellationToken) : null;
                await channelClientProvider.SendTypingAsync(messageInfo.ChatId, cancellationToken);                                
                if (IsStream)
                {
                    Task.Run(async () =>
                    {
                        while (!cancellation.IsCancellationRequested)
                        {
                            await Task.Delay(100);

                            if (cancellation.IsCancellationRequested)
                            {
                                break;
                            }

                            var streamResponse = await Stream(new ChannelChatStreamRequest
                            {
                                ChannelId = ChannelId.Value,
                                ChatId = messageInfo.ChatId.ToString()
                            });
                            if (!string.IsNullOrEmpty(streamResponse?.Content))
                            {
                                thinkMessageId = await SendOrEditTextMessage(messageInfo.ChatId, thinkMessageId, streamResponse.Content, null, options?.ParseMode, true, cancellationToken);
                                await channelClientProvider.SendTypingAsync(messageInfo.ChatId, cancellationToken);
                            }

                            //if (streamResponse?.Finished == true)
                            //    break;
                        }
                    }, cancellationToken);
                }

                // animate progress
                //Task.Run(async () =>
                //{
                //    while (!cancellation.IsCancellationRequested)
                //    {
                //        if (streamReceiving)
                //            break;

                //        await Task.Delay(1000);

                //        if (thinkText.Length < 4 + 6)
                //            thinkText += ".";
                //        else
                //            thinkText = "...";

                //        // Ожидаем завершения потока стрима перед обновлением
                //        await streamSemaphore.WaitAsync();

                //        try
                //        {
                //            if (cancellation.IsCancellationRequested)
                //                break;

                //            if (!streamReceiving)
                //            {
                //                await channelClientProvider.EditTextMessageAsync(messageInfo.ChatId, messageId, thinkText, null, cancellationToken);
                //                await channelClientProvider.SendTypingAsync(messageInfo.ChatId, cancellationToken);
                //            }
                //        }
                //        finally
                //        {
                //            streamSemaphore.Release();
                //        }
                //    }
                //}, cancellationToken);

                var response = await Complete(completeRequest);
                cancellation.Cancel();
                if (response.Markup?.Keyboard?.Clear == true)
                {
                    var messageId2 = await channelClientProvider.SendTextMessageAsync(messageInfo.ChatId, thinkText, new AIMarkup
                    {
                        Keyboard = new AIMarkup.AKeyboard
                        {
                            Buttons = new List<AIMarkup.AButton>(),
                            Remove = true
                        }
                    }, (int)ParseModeIds.Html, false, cancellationToken);
                    await channelClientProvider.DeleteTextMessageAsync(messageInfo.ChatId, messageId2, cancellationToken);
                }
                if (response.Markup == null)
                {
                    thinkMessageId = await SendOrEditTextMessage(messageInfo.ChatId, thinkMessageId, response?.Message?.Content, response?.Markup, options?.ParseMode, response.EscapeSpecialCharacters, cancellationToken);
                }
                else
                {
                    if (response.Markup.Keyboard?.Clear == true)
                    {
                        var messageId2 = await channelClientProvider.SendTextMessageAsync(messageInfo.ChatId, thinkText, new AIMarkup
                        {
                            Keyboard = new AIMarkup.AKeyboard
                            {
                                Buttons = new List<AIMarkup.AButton>(),
                                Remove = true
                            }
                        }, (int)ParseModeIds.Html, false, cancellationToken);
                        await channelClientProvider.DeleteTextMessageAsync(messageInfo.ChatId, messageId2, cancellationToken);
                    }

                    if (thinkMessageId != null)
                        await channelClientProvider.DeleteTextMessageAsync(messageInfo.ChatId, thinkMessageId.Value, cancellationToken);

                    var media = response?.Markup.Media;

                    if (media != null)
                    {
                        if (media.Align == AIMarkup.AMediaAlign.Top || media.Align == null)
                            await SendMedia(messageInfo.ChatId, media, cancellationToken);
                    }

                    await channelClientProvider.SendTextMessageAsync(messageInfo.ChatId, response?.Message?.Content, response?.Markup, options?.ParseMode, response.EscapeSpecialCharacters, cancellationToken);

                    if (media != null)
                    {
                        if (media.Align == AIMarkup.AMediaAlign.Bottom)
                            await SendMedia(messageInfo.ChatId, media, cancellationToken);
                    }
                }                

                if (response?.State == "finish")
                {
                    //await channelClientProvider.SendTextMessageAsync(messageInfo.ChatId, "Завершена работа агента.", cancellationToken);
                    completeRequest.AgentStateId = (int)AgentStateIds.Started;
                    await Complete(completeRequest);
                }                
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при обработке сообщения.");
                await channelClientProvider.SendTextMessageAsync(messageInfo.ChatId, $"Произошла ошибка. Попробуйте позже. {ex.Message}", null, (int)ParseModeIds.Html, false, cancellationToken);
            }
            finally
            {
                cancellation.Cancel();
            }
        }

        private async Task<int?> SendMedia(long chatId, AIMarkup.AMedia media, CancellationToken cancellationToken)
        {
            if (media == null)
                return null;
            switch (media.Type)
            {
                case AIMarkup.AMediaType.Video:
                    return await channelClientProvider.SendVideoAsync(chatId, media.Base64Content, cancellationToken);
                case AIMarkup.AMediaType.VideoNote:
                    return await channelClientProvider.SendVideoNoteAsync(chatId, media.Base64Content, media.Length, cancellationToken);
            }

            return null;
        }

        private async Task<int?> SendOrEditTextMessage(long chatId, int? messageId, string text, AIMarkup? markup, int? parseMode, bool escape, CancellationToken cancellationToken = default)
        {
            if (messageId != null)
                await channelClientProvider.EditTextMessageAsync(chatId, messageId.Value, text, markup, parseMode, escape, cancellationToken);
            else
            {
                messageId = await channelClientProvider.SendTextMessageAsync(chatId, text, markup, parseMode, escape, cancellationToken);
            }

            return messageId;
        }

        private async Task<ChannelChatStreamResponse> Stream(ChannelChatStreamRequest request)
        {
            var apiUrl = $"{agentConnection.Host}/api/v1/channels/chat/stream";

            logger.LogInformation($"Запрос к API для канала {request.ChannelId} по адресу {apiUrl}");

            var response = await SendAsAsync<ChannelChatStreamResponse>(apiUrl, request);

            logger.LogInformation("Ответ ассистента: {Content}", response?.Content);

            return response;
        }

        private async Task<ChannelChatStateResponse> State(ChannelChatStateRequest request)
        {
            var apiUrl = $"{agentConnection.Host}/api/v1/channels/chat/state";

            logger.LogInformation($"Запрос к API для канала {request.ChannelId} по адресу {apiUrl}");

            var response = await SendAsAsync<ChannelChatStateResponse>(apiUrl, request);

            //logger.LogInformation("Ответ ассистента: {Content}", response?.Content);

            return response;
        }

        private async Task<AgentChatStartCompleteResponse> StartComplete(ChannelChatCompleteRequest request)
        {
            var apiUrl = $"{agentConnection.Host}/api/v1/channels/chat/startComplete";

            logger.LogInformation($"Запрос start к API для канала {request.ChannelId} по адресу {apiUrl}");

            var response = await SendAsAsync<AgentChatStartCompleteResponse>(apiUrl, request);

            //if (!response.IsSuccessStatusCode)
            //{
            //    logger.LogError("Ошибка при запросе к API: {StatusCode} - {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
            //    throw new HttpRequestException($"Ошибка при запросе: {response.StatusCode}");
            //}

            //var result = await response.Content.ReadAsAsync<AgentChatCompleteResponse>();

            logger.LogInformation("Ответ ассистента: {Content}", response?.StartThinkingText);
            return response;
        }

        private async Task<AgentChatCompleteResponse> Complete(ChannelChatCompleteRequest request)
        {
            var apiUrl = $"{agentConnection.Host}/api/v1/channels/chat/complete";

            logger.LogInformation($"Запрос к API для канала {request.ChannelId} по адресу {apiUrl}");

            var response = await SendAsAsync<AgentChatCompleteResponse>(apiUrl, request);

            //if (!response.IsSuccessStatusCode)
            //{
            //    logger.LogError("Ошибка при запросе к API: {StatusCode} - {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
            //    throw new HttpRequestException($"Ошибка при запросе: {response.StatusCode}");
            //}

            //var result = await response.Content.ReadAsAsync<AgentChatCompleteResponse>();

            logger.LogInformation("Ответ ассистента: {Content}", response?.Message?.Content);

            if (response.FunctionCallMessage != null)
            {
                logger.LogInformation("Вызов функции: {FunctionCall}", JsonConvert.SerializeObject(response.FunctionCallMessage));
            }

            logger.LogInformation("Текущее состояние агента: {State}", response.State);

            return response;
        }

        private Task OnErrorAsync(Exception exception, CancellationToken cancellationToken)
        {
            logger.LogError(exception, $"Ошибка при получении обновлений Telegram: {ChannelId}");
            return Task.CompletedTask;
        }
    }
}
