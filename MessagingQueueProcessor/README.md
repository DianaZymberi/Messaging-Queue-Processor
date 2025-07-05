# Messaging Queue Processor

A .NET 8 Web API implementation of an in-memory message queue system with producer/consumer pattern supporting multiple message types.

## Features

- **In-Memory Message Queue**: Thread-safe implementation using `ConcurrentQueue`
- **Producer/Consumer Pattern**: Asynchronous message processing with background service
- **Multiple Message Types**: Support for SMS, Email, and Push Notifications
- **Retry Mechanism**: Automatic retry with configurable max retries
- **Message Status Tracking**: Pending, Processing, Completed, Failed states
- **REST API**: Full CRUD operations for queue management
- **Comprehensive Logging**: Structured logging with Serilog
- **Queue Statistics**: Real-time monitoring of queue performance

## Architecture

### Core Components

1. **Message Models** (`Models/Message.cs`)
   - Abstract `Message` base class
   - `SmsMessage`, `EmailMessage`, `PushNotificationMessage` implementations
   - Message status tracking and retry logic

2. **Message Queue** (`Services/InMemoryMessageQueue.cs`)
   - Thread-safe in-memory queue implementation
   - Concurrent collections for different message states
   - Semaphore-based synchronization

3. **Message Processors** (`Services/*Processor.cs`)
   - Type-specific processors for each message type
   - Simulated processing with configurable failure rates
   - Extensible design for custom processors

4. **Consumer Service** (`Services/MessageConsumerService.cs`)
   - Background service for continuous message processing
   - Automatic message routing to appropriate processors
   - Error handling and retry logic

5. **REST API** (`Controllers/MessageQueueController.cs`)
   - Endpoints for enqueueing messages
   - Queue monitoring and management
   - Failed message retry functionality

## API Endpoints

### Message Production
- `POST /api/messagequeue/sms` - Enqueue SMS message
- `POST /api/messagequeue/email` - Enqueue Email message
- `POST /api/messagequeue/push` - Enqueue Push notification

### Queue Management
- `GET /api/messagequeue/status` - Get queue statistics
- `GET /api/messagequeue/pending` - Get pending messages
- `GET /api/messagequeue/failed` - Get failed messages
- `POST /api/messagequeue/retry/{messageId}` - Retry failed message
- `DELETE /api/messagequeue/failed` - Clear all failed messages

### Health Check
- `GET /health` - Application health status

## Getting Started

### Prerequisites
- .NET 8 SDK
- Visual Studio 2022 or VS Code

### Running the Application

1. **Clone and Navigate**
   ```bash
   cd MessagingQueueProcessor
   ```

2. **Run the Application**
   ```bash
   dotnet run
   ```

3. **Access the API**
   - Swagger UI: https://localhost:7001/swagger
   - Health Check: https://localhost:7001/health

### Testing with HTTP Requests

Use the provided `MessagingQueueProcessor.http` file in VS Code or any HTTP client:

```http
### Enqueue SMS
POST https://localhost:7001/api/messagequeue/sms
Content-Type: application/json

{
  "phoneNumber": "+1234567890",
  "text": "Hello! This is a test SMS message."
}

### Check Queue Status
GET https://localhost:7001/api/messagequeue/status
```

## Message Processing Simulation

The processors include realistic simulation features:

- **Processing Delays**: Random delays to simulate real-world processing times
- **Failure Simulation**: Configurable failure rates for testing retry logic
  - SMS: 10% failure rate
  - Email: 5% failure rate  
  - Push: 8% failure rate
- **Retry Logic**: Automatic retry with exponential backoff (max 3 retries)

## Configuration

### Message Retry Settings
```csharp
public int MaxRetries { get; set; } = 3; // Configurable per message
```

### Processing Delays
- SMS: 100-500ms
- Email: 200-800ms
- Push: 150-600ms

### Failure Rates
- SMS: 10% (1 in 10)
- Email: 5% (1 in 20)
- Push: 8% (1 in 12)

## Logging

The application uses Serilog for structured logging:

- **Console Output**: Real-time logging during development
- **File Logging**: Daily rolling log files in `logs/` directory
- **Log Levels**: Information, Warning, Error with structured data

## Extending the System

### Adding New Message Types

1. Create a new message class inheriting from `Message`
2. Implement the `GetContent()` method
3. Create a corresponding processor implementing `IMessageProcessor`
4. Register the processor in `Program.cs`

### Custom Processors

```csharp
public class CustomMessageProcessor : IMessageProcessor
{
    public MessageType MessageType => MessageType.Custom;
    
    public async Task<bool> ProcessAsync(Message message, CancellationToken cancellationToken = default)
    {
        // Custom processing logic
        return true;
    }
}
```

## Performance Considerations

- **In-Memory Storage**: Fast but not persistent across application restarts
- **Thread Safety**: Uses concurrent collections and semaphores
- **Background Processing**: Non-blocking message consumption
- **Scalability**: Can be extended with multiple consumer instances

## Future Enhancements

- **Persistence**: Database storage for message durability
- **Distributed Queue**: Redis or RabbitMQ integration
- **Priority Queues**: Message prioritization
- **Dead Letter Queue**: Permanent failure handling
- **Metrics**: Prometheus/Grafana integration
- **Message Scheduling**: Delayed message processing
- **Batch Processing**: Bulk message operations

## Troubleshooting

### Common Issues

1. **Messages Not Processing**: Check if the consumer service is running
2. **High Memory Usage**: Monitor queue size and clear failed messages
3. **Processing Delays**: Adjust processing delays in processors
4. **Log Files**: Check `logs/` directory for detailed error information

### Monitoring

- Use `/api/messagequeue/status` for real-time queue metrics
- Monitor log files for processing errors
- Check application health with `/health` endpoint 