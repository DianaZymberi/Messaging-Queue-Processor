using MessagingQueueProcessor.Models;

namespace MessagingQueueProcessor.Services;

public class MessageConsumerService : BackgroundService
{
    private readonly IMessageQueue _messageQueue;
    private readonly IEnumerable<IMessageProcessor> _processors;
    private readonly ILogger<MessageConsumerService> _logger;
    private readonly Dictionary<MessageType, IMessageProcessor> _processorMap;

    public MessageConsumerService(
        IMessageQueue messageQueue,
        IEnumerable<IMessageProcessor> processors,
        ILogger<MessageConsumerService> logger)
    {
        _messageQueue = messageQueue;
        _processors = processors;
        _logger = logger;
        _processorMap = _processors.ToDictionary(p => p.MessageType);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Message Consumer Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = await _messageQueue.DequeueAsync(stoppingToken);
                
                if (message == null)
                {
                    // No messages in queue, wait a bit before checking again
                    await Task.Delay(1000, stoppingToken);
                    continue;
                }

                await ProcessMessageAsync(message, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Message Consumer Service stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in message consumer loop");
                await Task.Delay(5000, stoppingToken); // Wait before retrying
            }
        }

        _logger.LogInformation("Message Consumer Service stopped");
    }

    private async Task ProcessMessageAsync(Message message, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing message {MessageId} of type {MessageType}", 
                message.Id, message.Type);

            if (!_processorMap.TryGetValue(message.Type, out var processor))
            {
                var errorMessage = $"No processor found for message type {message.Type}";
                _logger.LogError(errorMessage);
                await _messageQueue.MarkMessageFailedAsync(message.Id, errorMessage);
                return;
            }

            var success = await processor.ProcessAsync(message, cancellationToken);

            if (success)
            {
                await _messageQueue.MarkMessageCompletedAsync(message.Id);
                _logger.LogInformation("Message {MessageId} processed successfully", message.Id);
            }
            else
            {
                await _messageQueue.MarkMessageFailedAsync(message.Id, "Processing failed");
                _logger.LogWarning("Message {MessageId} processing failed", message.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing message {MessageId}", message.Id);
            await _messageQueue.MarkMessageFailedAsync(message.Id, ex.Message);
        }
    }
} 