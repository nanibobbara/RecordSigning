using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RecordSigning.Shared;
using System.Configuration;

namespace BatchProcessingService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            var logger = host.Services.GetRequiredService<ILogger<Program>>();

            try
            {
                logger.LogInformation("BatchProcessingService is starting.");
                host.Run();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while running the BatchProcessingService");
            }
            finally
            {
                // Perform any cleanup if needed
                logger.LogInformation("BatchProcessingService is stopping.");
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: true);
                    config.AddEnvironmentVariables();
                })

                .ConfigureServices((hostContext, services) =>
                {
                    services.AddLogging();
                    services.AddDbContext<RecordSignDbContext>(options =>
                    {
                        options.UseSqlServer(hostContext.Configuration.GetConnectionString("RecordSignDbConnection"));
                    }, ServiceLifetime.Singleton);


                    services.AddSingleton<RecordSignDbService>();
                    services.AddSingleton<IConfiguration>(hostContext.Configuration);
                    
                    services.AddHostedService<MessageQueueConsumer>(); // Add the MessageQueueConsumer as a hosted service
                });
    }
}

