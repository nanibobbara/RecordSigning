
using Microsoft.EntityFrameworkCore;
using RecordSigning.Shared;

namespace KeyManagementService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddDbContext<RecordSignDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("RecordSignDbConnection"));
            },ServiceLifetime.Singleton);

            builder.Services.AddSingleton<RecordSignDbService>();
            builder.Services.AddSingleton<MessageQueuePublisher>(); 
            //builder.Services.AddSingleton<MessageQueueService>(); 

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}