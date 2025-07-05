using MessagingQueueProcessor.Models;

namespace MessagingQueueProcessor.Services;

public interface IMessageProcessor
{
    MessageType MessageType { get; }
    Task<bool> ProcessAsync(Message message, CancellationToken cancellationToken = default);
} 