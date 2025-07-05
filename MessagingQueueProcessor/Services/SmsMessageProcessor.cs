using MessagingQueueProcessor.Models;
using MessagingQueueProcessor.Data;

namespace MessagingQueueProcessor.Services;

public class SmsMessageProcessor : IMessageProcessor
{
    private readonly ILogger<SmsMessageProcessor> _logger;
    private readonly Random _random = new();
    private readonly MessageRepository _repository;

    public MessageType MessageType => MessageType.SMS;

    public SmsMessageProcessor(ILogger<SmsMessageProcessor> logger, MessageRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    public async Task<bool> ProcessAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message is not SmsMessage smsMessage)
        {
            _logger.LogError("Invalid message type for SMS processor: {MessageType}", message.GetType());
            return false;
        }

        try
        {
            _logger.LogInformation("Processing SMS message {MessageId} to {PhoneNumber}", 
                message.Id, smsMessage.PhoneNumber);

            // Simulate processing time
            await Task.Delay(_random.Next(100, 500), cancellationToken);

            // Simulate occasional failures (10% failure rate)
            if (_random.Next(1, 11) == 1)
            {
                throw new Exception("SMS delivery failed - network timeout");
            }

            _logger.LogInformation("SMS message {MessageId} processed successfully", message.Id);
            await _repository.InsertAsync(message);
            await _repository.UpdateStatusAsync(message.Id, MessageStatus.Completed, DateTime.UtcNow, null, message.RetryCount);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process SMS message {MessageId}", message.Id);
            await _repository.UpdateStatusAsync(message.Id, MessageStatus.Failed, null, ex.Message, message.RetryCount);
            return false;
        }
    }
} 