using Azure.Core;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RecordSigning.Shared;
using RecordSigning.Shared.Entities.Models;
using System;
using System.Security.Policy;
using System.Text;
using System.Text.Json;

namespace SigningService
{
    public class MessageQueueConsumer : BackgroundService
    {
        private readonly ILogger<MessageQueueConsumer> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _hostName;
        private readonly string _exchangeName;
        private readonly string _sourceQueueName;
        private readonly string _destinationQueueName;
        private readonly string _sourceRoutingKey;
        private readonly string _destinationRoutingKey;
        private readonly RecordSignDbService _recordSignDbService;
        public MessageQueueConsumer(ILogger<MessageQueueConsumer> logger, 
            IConfiguration configuration,
            RecordSignDbService recordSignDbService)
        {

            _logger = logger;
            _configuration = configuration;
            _hostName = _configuration["RabbitMQ:Host"];
            _exchangeName = _configuration["RabbitMQ:ExchangeName"];
            _sourceQueueName = _configuration["RabbitMQ:SourceQueueName"];
            _destinationQueueName = _configuration["RabbitMQ:DestinationQueueName"];
            _sourceRoutingKey = _configuration["RabbitMQ:SourceRoutingKey"];
            _destinationRoutingKey = _configuration["RabbitMQ:DestinationRoutingKey"];
            _recordSignDbService = recordSignDbService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var rabbitMQUsername = _configuration["RabbitMQ:Username"];
            var rabbitMQPassword = _configuration["RabbitMQ:Password"];
            string keyUrl = _configuration["KeyService:KeyURI"];
            var factory = new ConnectionFactory
            {
                HostName = _hostName,
                UserName = rabbitMQUsername,
                Password = rabbitMQPassword
            };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                // Declare the source queue
                channel.QueueDeclare(queue: _sourceQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

                // Declare the destination queue
                channel.QueueDeclare(queue: _destinationQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

                // Bind the source queue to the exchange with the source routing key
                channel.QueueBind(queue: _sourceQueueName, exchange: _exchangeName, routingKey: _sourceRoutingKey);

                // Create a consumer to consume messages from the source queue
                var consumer = new EventingBasicConsumer(channel);

                // Set up the event handler for received messages
                consumer.Received += (model, ea) =>
                {
                    try
                    {
                        // Get the message body
                        var messageBytes = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(messageBytes);

                        // Deserialize the JSON message into a KeyValuePair<string, int>
                        UnsignedRecordBatch unsignedRecordBatch = JsonSerializer.Deserialize<UnsignedRecordBatch>(message);

                        // Access the batch size from the payload
                        if(unsignedRecordBatch != null)
                        {
                            // get it signed

                            if (unsignedRecordBatch.records.Count > 0)
                            {
                                
                                if (!string.IsNullOrEmpty(keyUrl))
                                {
                                    KeyRing key = RestHelper.GetResponse($"{keyUrl}/getNextAvailableKey").Result;

                                    if (key != null)
                                    {
                                        KeyPair keyPair = JsonSerializer.Deserialize<KeyPair>(key.key_data);
                                        SignedRecordBatch signedRecordBatch = Cryptography.SignBatchOfUnsignedRecords(unsignedRecordBatch, keyPair);

                                        

                                        string json = JsonSerializer.Serialize(signedRecordBatch);
                                        var signedRecordBatchMessage = Encoding.UTF8.GetBytes(json);

                                        // Publish the message to the destination queue
                                        channel.BasicPublish(exchange: _exchangeName, routingKey: _destinationRoutingKey, basicProperties: null, body: signedRecordBatchMessage);
                                        _logger.LogInformation($"published batch {signedRecordBatch.batch_id} of {signedRecordBatch.records.Count},signed records");
                                    }
                                    // Now unlock the key and mark it as available
                                    string url = $"{keyUrl}?keyId={key.key_id}&isInUse=false";
                                    RestHelper.PutResponse(url);
                                }
                            }
                            
                            // Acknowledge the message to remove it from the source queue only when it signed successfully 
                            channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                        }
                        

                        _logger.LogInformation("Message consumed and published to the destination queue.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error consuming and publishing message");
                    }
                };

                // Start consuming messages from the source queue
                channel.BasicConsume(queue: _sourceQueueName, autoAck: false, consumer: consumer);

                _logger.LogInformation("Consuming messages from the source queue.");

                // Wait until cancellation is requested
                await Task.Delay(Timeout.Infinite, stoppingToken);

                // Stop consuming messages and close the channel and connection
                channel.BasicCancel(consumer.ConsumerTags[0]);
            }
        }
    }
}
