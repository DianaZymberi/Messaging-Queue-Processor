using System.Text.Json.Serialization;

namespace MessagingQueueProcessor.Models;

public enum MessageType
{
    SMS,
    Email,
    PushNotification
}

public enum MessageStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

public abstract class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public MessageType Type { get; set; }
    public MessageStatus Status { get; set; } = MessageStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;
    
    public abstract string GetContent();
}

public class SmsMessage : Message
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    
    public SmsMessage()
    {
        Type = MessageType.SMS;
    }
    
    public override string GetContent() => $"SMS to {PhoneNumber}: {Text}";
}

public class EmailMessage : Message
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? From { get; set; }
    
    public EmailMessage()
    {
        Type = MessageType.Email;
    }
    
    public override string GetContent() => $"Email to {To}: {Subject}";
}

public class PushNotificationMessage : Message
{
    public string DeviceToken { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, object>? Data { get; set; }
    
    public PushNotificationMessage()
    {
        Type = MessageType.PushNotification;
    }
    
    public override string GetContent() => $"Push to {DeviceToken}: {Title} - {Body}";
} 