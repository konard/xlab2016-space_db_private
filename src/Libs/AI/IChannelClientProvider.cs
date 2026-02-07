namespace AI
{
    public interface IChannelClientProvider
    {
        Task SendTypingAsync(long chatId, CancellationToken cancellationToken);
        Task<int> SendTextMessageAsync(long chatId, string text, AIMarkup markup, int? parseMode, bool escape, CancellationToken cancellationToken);
        Task EditTextMessageAsync(long chatId, int messageId, string text, AIMarkup markup, int? parseMode, bool escape, CancellationToken cancellationToken);
        Task<int> SendVideoAsync(long chatId, string base64Content, CancellationToken cancellationToken);
        Task<int> SendVideoNoteAsync(long chatId, string base64Content, int? length = null, CancellationToken cancellationToken = default);
        Task DeleteTextMessageAsync(long chatId, int messageId, CancellationToken cancellationToken);
        Task MonitorConnectionAsync(CancellationToken cancellationToken, Func<Task> ping = null);
        Task StartReceivingAsync(CancellationToken cancellationToken, Func<MessageInfo, CancellationToken, Task> onMessageAsync, 
            Func<Exception, CancellationToken, Task> onErrorAsync);
        Task Stop();
    }
}
