using MessagingQueueProcessor.Services;
using Serilog;
using MessagingQueueProcessor.Data;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/messaging-queue-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register message queue services
builder.Services.AddSingleton<IMessageQueue, InMemoryMessageQueue>();

builder.Services.AddSingleton<IMessageProcessor, SmsMessageProcessor>();
builder.Services.AddSingleton<IMessageProcessor, EmailMessageProcessor>();
builder.Services.AddSingleton<IMessageProcessor, PushNotificationMessageProcessor>();

// Register background service for message consumption
builder.Services.AddHostedService<MessageConsumerService>();

builder.Services.AddSingleton<MessageRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Add a simple health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));

Log.Information("Starting Messaging Queue Processor...");
app.Run();
