using Microsoft.AspNetCore.Mvc;
using MessagingQueueProcessor.Models;
using MessagingQueueProcessor.Services;

namespace MessagingQueueProcessor.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessageQueueController : ControllerBase
{
    private readonly IMessageQueue _messageQueue;
    private readonly ILogger<MessageQueueController> _logger;

    public MessageQueueController(IMessageQueue messageQueue, ILogger<MessageQueueController> logger)
    {
        _messageQueue = messageQueue;
        _logger = logger;
    }

    [HttpPost("sms")]
    public async Task<IActionResult> EnqueueSms([FromBody] SmsRequest request)
    {
        try
        {
            var message = new SmsMessage
            {
                PhoneNumber = request.PhoneNumber,
                Text = request.Text
            };

            await _messageQueue.EnqueueAsync(message);
            
            _logger.LogInformation("SMS message enqueued: {MessageId}", message.Id);
            
            return Ok(new { MessageId = message.Id, Status = "Enqueued" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue SMS message");
            return StatusCode(500, new { Error = "Failed to enqueue SMS message" });
        }
    }

    [HttpPost("email")]
    public async Task<IActionResult> EnqueueEmail([FromBody] EmailRequest request)
    {
        try
        {
            var message = new EmailMessage
            {
                To = request.To,
                Subject = request.Subject,
                Body = request.Body,
                From = request.From
            };

            await _messageQueue.EnqueueAsync(message);
            
            _logger.LogInformation("Email message enqueued: {MessageId}", message.Id);
            
            return Ok(new { MessageId = message.Id, Status = "Enqueued" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue Email message");
            return StatusCode(500, new { Error = "Failed to enqueue Email message" });
        }
    }

    [HttpPost("push")]
    public async Task<IActionResult> EnqueuePushNotification([FromBody] PushNotificationRequest request)
    {
        try
        {
            var message = new PushNotificationMessage
            {
                DeviceToken = request.DeviceToken,
                Title = request.Title,
                Body = request.Body,
                Data = request.Data
            };

            await _messageQueue.EnqueueAsync(message);
            
            _logger.LogInformation("Push notification enqueued: {MessageId}", message.Id);
            
            return Ok(new { MessageId = message.Id, Status = "Enqueued" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue Push notification");
            return StatusCode(500, new { Error = "Failed to enqueue Push notification" });
        }
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetQueueStatus()
    {
        try
        {
            var statistics = await _messageQueue.GetStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get queue status");
            return StatusCode(500, new { Error = "Failed to get queue status" });
        }
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingMessages()
    {
        try
        {
            var messages = await _messageQueue.GetPendingMessagesAsync();
            return Ok(messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pending messages");
            return StatusCode(500, new { Error = "Failed to get pending messages" });
        }
    }

    [HttpGet("failed")]
    public async Task<IActionResult> GetFailedMessages()
    {
        try
        {
            var messages = await _messageQueue.GetFailedMessagesAsync();
            return Ok(messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get failed messages");
            return StatusCode(500, new { Error = "Failed to get failed messages" });
        }
    }

    [HttpPost("retry/{messageId}")]
    public async Task<IActionResult> RetryFailedMessage(Guid messageId)
    {
        try
        {
            await _messageQueue.RetryFailedMessageAsync(messageId);
            return Ok(new { MessageId = messageId, Status = "Retried" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retry message {MessageId}", messageId);
            return StatusCode(500, new { Error = "Failed to retry message" });
        }
    }

    [HttpDelete("failed")]
    public async Task<IActionResult> ClearFailedMessages()
    {
        try
        {
            await _messageQueue.ClearFailedMessagesAsync();
            return Ok(new { Status = "Failed messages cleared" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear failed messages");
            return StatusCode(500, new { Error = "Failed to clear failed messages" });
        }
    }
}

public class SmsRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class EmailRequest
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? From { get; set; }
}

public class PushNotificationRequest
{
    public string DeviceToken { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, object>? Data { get; set; }
} 