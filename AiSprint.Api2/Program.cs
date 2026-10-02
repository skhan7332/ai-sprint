
using AiSprint.Api2.Model;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenTelemetry.Trace;

namespace AiSprint.Api2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var ollamaConfig = builder.Configuration.GetSection(OllamaConfig.SectionName).Get<OllamaConfig>();
            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddChatClient(new OllamaApiClient(new Uri(ollamaConfig.Endpoint), ollamaConfig.Model))
                .UseDistributedCache()
                .UseLogging()
                .UseOpenTelemetry(sourceName: "AiSprint");
            builder.Services.AddOpenTelemetry()
                .WithTracing(t => t
                    .AddSource("AiSprint")
                    .SetSampler(new AlwaysOnSampler())
                    .AddConsoleExporter()
                );


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "AiSprint.Api2 v1"));
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
