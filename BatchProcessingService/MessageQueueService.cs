using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RecordSigning.Shared;
using System.Text;
using System.Text.Json;

namespace BatchProcessingService
{
    public class MessageQueueService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _exchangeName;
        private readonly string _batchSizeQueueName;
        private readonly string _unsignedRecordsQueueName;


        public MessageQueueService(ILogger<Worker> logger,IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _exchangeName = _configuration["RabbitMQ:ExchangeName"];
            _batchSizeQueueName = _configuration["RabbitMQ:BatchSizeQueueName"];
            _unsignedRecordsQueueName = _configuration["RabbitMQ:UnsignedQueueName"];
        }

        public void PublishBatchSize(int batchSize)
        {
            var rabbitMQHost = _configuration["RabbitMQ:Host"];
            var rabbitMQUsername = _configuration["RabbitMQ:Username"];
            var rabbitMQPassword = _configuration["RabbitMQ:Password"];

            var factory = new ConnectionFactory
            {
                HostName = rabbitMQHost,
                UserName = rabbitMQUsername,
                Password = rabbitMQPassword
            };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                // Declare the exchange
                channel.ExchangeDeclare(_exchangeName, ExchangeType.Topic);

                // Declare the queue
                channel.QueueDeclare(_batchSizeQueueName, durable: true, exclusive: false, autoDelete: false);

                // Bind the queue to the exchange
                channel.QueueBind(_batchSizeQueueName, _exchangeName, routingKey: "");



                // Create the message payload as a JSON object
                var payload = new KeyValuePair<string, int>("BatchSize", batchSize);

                string json = JsonSerializer.Serialize(payload);
                var body = Encoding.UTF8.GetBytes(json);

                // Publish the message to the queue
                var properties = channel.CreateBasicProperties();
                properties.Persistent = true; // Ensure the message is persisted to disk
                //channel.BasicPublish(_exchangeName, _batchSizeQueueName, properties, body);
                channel.BasicPublish(_exchangeName, routingKey: "", properties, body);

            }
        }

        public void SubscribeAndPublishRecords()
        {
            var rabbitMQHost = _configuration["RabbitMQ:Host"];
            var rabbitMQUsername = _configuration["RabbitMQ:Username"];
            var rabbitMQPassword = _configuration["RabbitMQ:Password"];

            var factory = new ConnectionFactory
            {
                HostName = rabbitMQHost,
                UserName = rabbitMQUsername,
                Password = rabbitMQPassword
            };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                // Declare the exchange
                channel.ExchangeDeclare(_exchangeName, ExchangeType.Topic);

                // Declare the queue
                channel.QueueDeclare(_batchSizeQueueName, durable: true, exclusive: false, autoDelete: false);

                // Bind the queue to the exchange
                channel.QueueBind(_batchSizeQueueName, _exchangeName, routingKey: "");

                // Create a consumer for the queue
                var consumer = new EventingBasicConsumer(channel);

                // Set up the event handler for received messages
                consumer.Received += (model, args) =>
                {
                    var message = Encoding.UTF8.GetString(args.Body.ToArray());
                    var batchSize = JsonSerializer.Deserialize<KeyValuePair<string, int>>(message);
                    // Process the received message, e.g., insert it into the database
                    //ProcessReceivedMessage(message);
                    _logger.LogInformation(batchSize.Value.ToString(), DateTimeOffset.Now);
                    // Publish the received message to another queue (UnsignedRecords) on the same exchange
                    //channel.BasicPublish(_exchangeName, _unsignedRecordsQueueName, null, args.Body.ToArray());
                };

                // Start consuming messages from the queue
                channel.BasicConsume(_batchSizeQueueName, autoAck: true, consumer);
            }
        }

    }
}
