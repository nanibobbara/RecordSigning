using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace KeyManagementService
{
    public class MessageQueuePublisher
    {
        private readonly ILogger<MessageQueuePublisher> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _hostName;
        private readonly string _exchangeName;
        private readonly string _queueName;
        private readonly string _routingKey;

        public MessageQueuePublisher(ILogger<MessageQueuePublisher> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _hostName = _configuration["RabbitMQ:Host"];
            _exchangeName = _configuration["RabbitMQ:ExchangeName"];
            _routingKey = _configuration["RabbitMQ:RoutingKey"];
        }

        public void PublishBatchSize(int batchSize)
        {
            var rabbitMQUsername = _configuration["RabbitMQ:Username"];
            var rabbitMQPassword = _configuration["RabbitMQ:Password"];

            var factory = new ConnectionFactory
            {
                HostName = _hostName,
                UserName = rabbitMQUsername,
                Password = rabbitMQPassword
            };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                // Declare the exchange
                channel.ExchangeDeclare(exchange: _exchangeName, type: ExchangeType.Direct,durable:true);

                // Create the message payload as a JSON object
                var payload = new KeyValuePair<string, int>("BatchSize", batchSize);
                string json = JsonSerializer.Serialize(payload);

                byte[] messageBytes = Encoding.UTF8.GetBytes(json);


                // Publish the message to the exchange with the specified routing key
                channel.BasicPublish(exchange: _exchangeName, routingKey: _routingKey, basicProperties: null, body: messageBytes);


            }
        }
    }
}
