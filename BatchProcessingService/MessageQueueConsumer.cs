﻿using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RecordSigning.Shared;
using System.Text;
using System.Text.Json;

namespace BatchProcessingService
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
        private bool prevOps = false;
        public MessageQueueConsumer(ILogger<MessageQueueConsumer> logger,
            IConfiguration configuration,
            RecordSignDbService recordSignDbService)
        {
            _logger = logger;
            _configuration = configuration;

            // Read RabbitMQ configuration values from appsettings.json
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
            // Read RabbitMQ credentials from appsettings.json
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
                // Declare the source and destination queues
                channel.QueueDeclare(queue: _sourceQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
                channel.QueueDeclare(queue: _destinationQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

                // Bind the source queue to the exchange
                channel.QueueBind(queue: _sourceQueueName, exchange: _exchangeName, routingKey: _sourceRoutingKey);

                //var consumer = new AsyncEventingBasicConsumer(channel);
                // Create a consumer to consume messages from the source queue
                var consumer = new EventingBasicConsumer(channel);

                consumer.Received += async (model, ea) =>
                {
                    try
                    {
                        while (prevOps)
                        {
                            await Task.Delay(30, stoppingToken);
                        }
                        var messageBytes = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(messageBytes);
                        var payload = JsonSerializer.Deserialize<KeyValuePair<string, int>>(message);
                        
                        if (payload.Value > 0)
                        {
                            
                            // Get unsigned records from the database
                            List<Record> unsignedRecords = _recordSignDbService.GetRecords(payload.Value);

                            while (unsignedRecords.Count > 0)
                            {
                                prevOps = true;
                                // Create a batch of unsigned records
                                UnsignedRecordBatch batchRecord = new UnsignedRecordBatch(payload.Value, unsignedRecords);
                                string json = JsonSerializer.Serialize(batchRecord);
                                var batchRecordMessage = Encoding.UTF8.GetBytes(json);

                                // Publish the batch of records to the destination queue
                                channel.BasicPublish(exchange: _exchangeName, routingKey: _destinationRoutingKey, basicProperties: null, body: batchRecordMessage);
                                _logger.LogInformation($"Published batch of {unsignedRecords.Count} records");

                                // Retrieve the next batch of unsigned records
                                unsignedRecords = _recordSignDbService.GetRecords(payload.Value);
                                
                            }
                            prevOps = false;
                        }

                        // Acknowledge the message after successful processing
                        channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);

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

                await Task.Delay(Timeout.Infinite, stoppingToken);

                // Cancel the consumer when the service is stopping
                channel.BasicCancel(consumer.ConsumerTags[0]);
            }
        }
    }
}
