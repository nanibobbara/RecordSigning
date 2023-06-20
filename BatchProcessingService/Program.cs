using Microsoft.EntityFrameworkCore;
using RecordSigning.Shared;

namespace BatchProcessingService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            host.Run();
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
                    });

                    //services.AddScoped<RecordSignDbService>();
                    services.AddSingleton<MessageQueueService>();
                    services.AddHostedService<Worker>();
                });
    }
}

/*
 * 
 * Services.AddDbContext<RecordSignDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("RecordSignDbConnection"));
            });

            builder.Services.AddScoped<RecordSignDbService>();
            builder.Services.AddSingleton<MessageQueueService>();*/