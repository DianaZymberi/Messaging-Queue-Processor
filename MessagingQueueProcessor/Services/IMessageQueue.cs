using MessagingQueueProcessor.Models;

namespace MessagingQueueProcessor.Services;

public interface IMessageQueue
{
    Task EnqueueAsync(Message message);
    Task<Message?> DequeueAsync(CancellationToken cancellationToken = default);
    Task<Message?> PeekAsync();
    Task<int> GetQueueSizeAsync();
    Task<IEnumerable<Message>> GetPendingMessagesAsync();
    Task<IEnumerable<Message>> GetFailedMessagesAsync();
    Task RetryFailedMessageAsync(Guid messageId);
    Task ClearFailedMessagesAsync();
    Task<QueueStatistics> GetStatisticsAsync();
    Task MarkMessageCompletedAsync(Guid messageId);
    Task MarkMessageFailedAsync(Guid messageId, string errorMessage);
}

public class QueueStatistics
{
    public int TotalMessages { get; set; }
    public int PendingMessages { get; set; }
    public int ProcessingMessages { get; set; }
    public int CompletedMessages { get; set; }
    public int FailedMessages { get; set; }
    public Dictionary<MessageType, int> MessagesByType { get; set; } = new();
} 