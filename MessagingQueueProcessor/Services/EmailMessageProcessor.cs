using MessagingQueueProcessor.Models;

namespace MessagingQueueProcessor.Services;

public class EmailMessageProcessor : IMessageProcessor
{
    private readonly ILogger<EmailMessageProcessor> _logger;
    private readonly Random _random = new();

    public MessageType MessageType => MessageType.Email;

    public EmailMessageProcessor(ILogger<EmailMessageProcessor> logger)
    {
        _logger = logger;
    }

    public async Task<bool> ProcessAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message is not EmailMessage emailMessage)
        {
            _logger.LogError("Invalid message type for Email processor: {MessageType}", message.GetType());
            return false;
        }

        try
        {
            _logger.LogInformation("Processing Email message {MessageId} to {To}", 
                message.Id, emailMessage.To);

            // Simulate processing time (emails take longer than SMS)
            await Task.Delay(_random.Next(200, 800), cancellationToken);

            // Simulate occasional failures (5% failure rate)
            if (_random.Next(1, 21) == 1)
            {
                throw new Exception("Email delivery failed - recipient server unavailable");
            }

            _logger.LogInformation("Email message {MessageId} processed successfully", message.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Email message {MessageId}", message.Id);
            return false;
        }
    }
} 