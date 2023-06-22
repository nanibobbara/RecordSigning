using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RecordSigning.Shared;
using System.Text;
using System.Text.Json;

namespace RecordKeepingService
{
    public class MessageQueueConsumer : BackgroundService
    {
        private readonly ILogger<MessageQueueConsumer> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _hostName;
        private readonly string _exchangeName;
        private readonly string _sourceQueueName;
        private readonly string _sourceRoutingKey;
        private readonly RecordSignDbService _recordSignDbService;
        private IConnection _connection;
        private IModel _channel;

        public MessageQueueConsumer(ILogger<MessageQueueConsumer> logger,
            IConfiguration configuration,
            RecordSignDbService recordSignDbService)
        {
            _logger = logger;
            _configuration = configuration;
            _hostName = _configuration["RabbitMQ:Host"];
            _exchangeName = _configuration["RabbitMQ:ExchangeName"];
            _sourceQueueName = _configuration["RabbitMQ:SourceQueueName"];
            _sourceRoutingKey = _configuration["RabbitMQ:SourceRoutingKey"];
            _recordSignDbService = recordSignDbService;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            var rabbitMQUsername = _configuration["RabbitMQ:Username"];
            var rabbitMQPassword = _configuration["RabbitMQ:Password"];
            var factory = new ConnectionFactory
            {
                HostName = _hostName,
                UserName = rabbitMQUsername,
                Password = rabbitMQPassword
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare the source queue
            _channel.QueueDeclare(queue: _sourceQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
                        
            // Bind the source queue to the exchange with the source routing key
            _channel.QueueBind(queue: _sourceQueueName, exchange: _exchangeName, routingKey: _sourceRoutingKey);

            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _channel?.Close();
            _connection?.Close();
            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    // Get the message body
                    var messageBytes = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(messageBytes);

                    // Deserialize the SignedRecordBatch
                    SignedRecordBatch signedRecordBatch = JsonSerializer.Deserialize<SignedRecordBatch>(message);

                    // Access the batch size from the payload
                    if (signedRecordBatch != null && signedRecordBatch.records.Count > 0)
                    {
                        
                        _recordSignDbService.CreateSignedRecord(signedRecordBatch.records);                        

                        _recordSignDbService.UpdateRecordStatus(signedRecordBatch.batch_id);
                    }

                    // Acknowledge the message to remove it from the source queue only when it signed successfully 
                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);

                    _logger.LogInformation("Message consumed and published to the destination queue.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error consuming and publishing message");
                }
            };

            // Start consuming messages from the source queue
            _channel.BasicConsume(queue: _sourceQueueName, autoAck: false, consumer: consumer);

            _logger.LogInformation("Consuming messages from the source queue.");

            // Wait until cancellation is requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}
