using RecordSigning.Shared;

namespace BatchProcessingService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private readonly RecordSignDbService _recordSignDbService;
        private readonly MessageQueueService _messageQueueService;
        private readonly RecordSignDbContext _recordSignDbContext;

        public Worker(ILogger<Worker> logger, 
            IConfiguration configuration, 
            MessageQueueService messageQueueService)
        {
            _logger = logger;
            _configuration = configuration;
            _messageQueueService = messageQueueService;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            while (!stoppingToken.IsCancellationRequested)
            {
                //_logger.LogInformation(_configuration["RabbitMQ:ExchangeName"], DateTimeOffset.Now);
                _messageQueueService.SubscribeAndPublishRecords();
                //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}