using MessagingQueueProcessor.Models;

namespace MessagingQueueProcessor.Services;

public class PushNotificationMessageProcessor : IMessageProcessor
{
    private readonly ILogger<PushNotificationMessageProcessor> _logger;
    private readonly Random _random = new();

    public MessageType MessageType => MessageType.PushNotification;

    public PushNotificationMessageProcessor(ILogger<PushNotificationMessageProcessor> logger)
    {
        _logger = logger;
    }

    public async Task<bool> ProcessAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message is not PushNotificationMessage pushMessage)
        {
            _logger.LogError("Invalid message type for Push Notification processor: {MessageType}", message.GetType());
            return false;
        }

        try
        {
            _logger.LogInformation("Processing Push Notification message {MessageId} to device {DeviceToken}", 
                message.Id, pushMessage.DeviceToken);

            // Simulate processing time
            await Task.Delay(_random.Next(150, 600), cancellationToken);

            // Simulate occasional failures (8% failure rate)
            if (_random.Next(1, 13) == 1)
            {
                throw new Exception("Push notification failed - device token invalid");
            }

            _logger.LogInformation("Push Notification message {MessageId} processed successfully", message.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Push Notification message {MessageId}", message.Id);
            return false;
        }
    }
} 