
using AiSprint.Api2.Model;
using AiSprint.Api2.Services;
using Microsoft.Agents.AI;
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

            builder.Services.AddKeyedChatClient("agent", new OllamaApiClient(new Uri(ollamaConfig.Endpoint), ollamaConfig.Model))
                    .UseLogging()
                    .UseOpenTelemetry(sourceName: "AiSprint");

            builder.Services.AddSingleton(new JobTools(Path.Combine(AppContext.BaseDirectory, "Resources", "jobs.json")));

            builder.Services.AddSingleton<AIAgent>(sp =>
            {
                var chatClient = sp.GetRequiredKeyedService<IChatClient>("agent");
                var jobTools = sp.GetRequiredService<JobTools>();

                AIAgent fitReviewer = chatClient.AsAIAgent(
                    name: "FitReviewer",
                    description: "Reviews one job posting and lists the skills a senior .NET + React + Azure engineer would need to learn for it. " +
                    "Pass the full job details from GetJobDetails, including the description, not just the title.",
                    instructions: "Given a job description, list the skill gaps for a senior .NET + React + Azure engineer. " +
                    "Base the gaps only on the posting you are given; do not invent requirements.");

                return chatClient.AsAIAgent(
                    name: "JobScout",
                    instructions: "You help a senior .NET + React engineer find matching jobs. Always use tools. Never invent jobs. " +
                      "Only mention jobs returned by a tool, using their exact id, title and company. " +
                      "If a search finds nothing, say so, and only suggest alternatives from ListAllJobs.",
                    tools: [
                        AIFunctionFactory.Create(jobTools.SearchJobs),
                        AIFunctionFactory.Create(jobTools.ListAllJobs),
                        AIFunctionFactory.Create(jobTools.GetJobDetails),
                        fitReviewer.AsAIFunction()]);
            });

            builder.Services.AddSingleton<ConversationStore>();

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
