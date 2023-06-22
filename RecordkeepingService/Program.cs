using Microsoft.EntityFrameworkCore;
using RecordSigning.Shared;

namespace RecordKeepingService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var logger = NLog.Web.NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();

            try
            {
                logger.Info("Record Keeping Service starting up");
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "An error occurred during application startup");
                throw;
            }
            finally
            {
                NLog.LogManager.Shutdown();
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
                    services.AddDbContext<RecordSignDbContext>(options =>
                    {
                        options.UseSqlServer(hostContext.Configuration.GetConnectionString("RecordSignDbConnection"));
                    }, ServiceLifetime.Singleton);


                    services.AddSingleton<RecordSignDbService>();
                    services.AddHostedService<MessageQueueConsumer>();
                });
    }
}

