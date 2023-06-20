using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace KeyManagementService
{
    public class MessageQueueService
    {
        private readonly IConfiguration _configuration;
        private readonly string _exchangeName;
        private readonly string _batchSizeQueueName;

        public MessageQueueService(IConfiguration configuration)
        {
            _configuration = configuration;
            _exchangeName = _configuration["RabbitMQ:ExchangeName"];
            _batchSizeQueueName = _configuration["RabbitMQ:BatchSizeQueueName"];
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
    }
}
