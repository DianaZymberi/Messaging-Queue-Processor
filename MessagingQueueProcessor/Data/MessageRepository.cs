using System.Data;
using System.Text.Json;
using Dapper;
using MessagingQueueProcessor.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace MessagingQueueProcessor.Data;

public class MessageRepository
{
    private readonly string _connectionString;

    public MessageRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Postgres")!;
    }

    private IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);

    public async Task InsertAsync(Message message)
    {
        var sql = @"INSERT INTO messages (id, type, status, created_at, processed_at, error_message, retry_count, max_retries, payload)
                    VALUES (@Id, @Type, @Status, @CreatedAt, @ProcessedAt, @ErrorMessage, @RetryCount, @MaxRetries, @Payload)";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new
        {
            message.Id,
            Type = message.Type.ToString(),
            Status = message.Status.ToString(),
            message.CreatedAt,
            message.ProcessedAt,
            message.ErrorMessage,
            message.RetryCount,
            message.MaxRetries,
            Payload = JsonSerializer.Serialize(message)
        });
    }

    public async Task UpdateStatusAsync(Guid id, MessageStatus status, DateTime? processedAt, string? errorMessage, int retryCount)
    {
        var sql = @"UPDATE messages SET status=@Status, processed_at=@ProcessedAt, error_message=@ErrorMessage, retry_count=@RetryCount WHERE id=@Id";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new
        {
            Id = id,
            Status = status.ToString(),
            ProcessedAt = processedAt,
            ErrorMessage = errorMessage,
            RetryCount = retryCount
        });
    }

    public async Task<IEnumerable<Message>> GetPendingOrProcessingAsync()
    {
        var sql = @"SELECT * FROM messages WHERE status = 'Pending' OR status = 'Processing'";
        using var conn = CreateConnection();
        var rows = await conn.QueryAsync(sql);
        var messages = new List<Message>();
        foreach (var row in rows)
        {
            var payload = (string)row.payload;
            var type = (string)row.type;
            Message? msg = type switch
            {
                "SMS" => JsonSerializer.Deserialize<SmsMessage>(payload),
                "Email" => JsonSerializer.Deserialize<EmailMessage>(payload),
                "PushNotification" => JsonSerializer.Deserialize<PushNotificationMessage>(payload),
                _ => null
            };
            if (msg != null)
            {
                msg.Status = Enum.Parse<MessageStatus>((string)row.status);
                msg.Id = (Guid)row.id;
                msg.CreatedAt = (DateTime)row.created_at;
                msg.ProcessedAt = row.processed_at as DateTime?;
                msg.ErrorMessage = row.error_message as string;
                msg.RetryCount = (int)row.retry_count;
                msg.MaxRetries = (int)row.max_retries;
                messages.Add(msg);
            }
        }
        return messages;
    }
} 