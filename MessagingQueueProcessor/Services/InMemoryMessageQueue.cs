using MessagingQueueProcessor.Models;
using MessagingQueueProcessor.Data;
using System.Collections.Concurrent;

namespace MessagingQueueProcessor.Services;

public class InMemoryMessageQueue : IMessageQueue
{
    private readonly ConcurrentQueue<Message> _queue = new();
    private readonly ConcurrentDictionary<Guid, Message> _processingMessages = new();
    private readonly ConcurrentDictionary<Guid, Message> _completedMessages = new();
    private readonly ConcurrentDictionary<Guid, Message> _failedMessages = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ILogger<InMemoryMessageQueue> _logger;
    private readonly MessageRepository _repository;

    public InMemoryMessageQueue(ILogger<InMemoryMessageQueue> logger, MessageRepository repository)
    {
        _logger = logger;
        _repository = repository;
        Task.Run(LoadPersistedMessagesAsync).Wait();
    }

    private async Task LoadPersistedMessagesAsync()
    {
        var messages = await _repository.GetPendingOrProcessingAsync();
        foreach (var message in messages)
        {
            _queue.Enqueue(message);
        }
        _logger.LogInformation("Loaded {Count} messages from database into queue", messages.Count());
    }

    public async Task EnqueueAsync(Message message)
    {
        await _semaphore.WaitAsync();
        try
        {
            _queue.Enqueue(message);
            await _repository.InsertAsync(message);
            _logger.LogInformation("Message {MessageId} of type {MessageType} enqueued", 
                message.Id, message.Type);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<Message?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (_queue.TryDequeue(out var message))
            {
                message.Status = MessageStatus.Processing;
                _processingMessages.TryAdd(message.Id, message);
                await _repository.UpdateStatusAsync(message.Id, MessageStatus.Processing, null, null, message.RetryCount);
                _logger.LogInformation("Message {MessageId} of type {MessageType} dequeued for processing", 
                    message.Id, message.Type);
                return message;
            }
            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<Message?> PeekAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            return _queue.TryPeek(out var message) ? message : null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<int> GetQueueSizeAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            return _queue.Count;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IEnumerable<Message>> GetPendingMessagesAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            return _queue.ToArray();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IEnumerable<Message>> GetFailedMessagesAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            return _failedMessages.Values.ToArray();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task RetryFailedMessageAsync(Guid messageId)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_failedMessages.TryRemove(messageId, out var message))
            {
                message.Status = MessageStatus.Pending;
                message.RetryCount = 0;
                message.ErrorMessage = null;
                _queue.Enqueue(message);
                await _repository.UpdateStatusAsync(messageId, MessageStatus.Pending, null, null, 0);
                _logger.LogInformation("Message {MessageId} retried", messageId);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task ClearFailedMessagesAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            _failedMessages.Clear();
            // Optionally, delete failed messages from DB if required
            _logger.LogInformation("All failed messages cleared");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<QueueStatistics> GetStatisticsAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            var statistics = new QueueStatistics
            {
                PendingMessages = _queue.Count,
                ProcessingMessages = _processingMessages.Count,
                CompletedMessages = _completedMessages.Count,
                FailedMessages = _failedMessages.Count
            };

            statistics.TotalMessages = statistics.PendingMessages + statistics.ProcessingMessages + 
                                     statistics.CompletedMessages + statistics.FailedMessages;

            // Count messages by type
            var allMessages = _queue.Concat(_processingMessages.Values)
                                  .Concat(_completedMessages.Values)
                                  .Concat(_failedMessages.Values);

            foreach (var message in allMessages)
            {
                if (!statistics.MessagesByType.ContainsKey(message.Type))
                    statistics.MessagesByType[message.Type] = 0;
                statistics.MessagesByType[message.Type]++;
            }

            return statistics;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task MarkMessageCompletedAsync(Guid messageId)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_processingMessages.TryRemove(messageId, out var message))
            {
                message.Status = MessageStatus.Completed;
                message.ProcessedAt = DateTime.UtcNow;
                _completedMessages.TryAdd(messageId, message);
                await _repository.UpdateStatusAsync(messageId, MessageStatus.Completed, message.ProcessedAt, null, message.RetryCount);
                _logger.LogInformation("Message {MessageId} marked as completed", messageId);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task MarkMessageFailedAsync(Guid messageId, string errorMessage)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_processingMessages.TryRemove(messageId, out var message))
            {
                message.RetryCount++;
                message.ErrorMessage = errorMessage;

                if (message.RetryCount >= message.MaxRetries)
                {
                    message.Status = MessageStatus.Failed;
                    _failedMessages.TryAdd(messageId, message);
                    await _repository.UpdateStatusAsync(messageId, MessageStatus.Failed, null, errorMessage, message.RetryCount);
                    _logger.LogError("Message {MessageId} failed permanently after {RetryCount} retries: {ErrorMessage}", 
                        messageId, message.RetryCount, errorMessage);
                }
                else
                {
                    message.Status = MessageStatus.Pending;
                    _queue.Enqueue(message);
                    await _repository.UpdateStatusAsync(messageId, MessageStatus.Pending, null, errorMessage, message.RetryCount);
                    _logger.LogWarning("Message {MessageId} failed, retrying ({RetryCount}/{MaxRetries}): {ErrorMessage}", 
                        messageId, message.RetryCount, message.MaxRetries, errorMessage);
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
} 