using AiSprint.Api2.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace AiSprint.Api2.Controllers
{
    [ApiController]
    [Route("/api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly IChatClient _chatClient;

        public ChatController(IChatClient chatClient)
        {
            _chatClient = chatClient;
        }

        [HttpPost]
        public async Task<IResult> Index(ChatRequest request, CancellationToken ct)
        {
            if (request is null || request.messages.Count == 0)
            {
                return Results.BadRequest("Send at least one item in 'messages', e.g. { \"messages\": [ { \"role\": \"user\", \"content\": \"Hi\" } ] }");
            }

            var res = await _chatClient.GetResponseAsync(request.messages.Select(m => new ChatMessage(new ChatRole(m.Role), m.content)), cancellationToken: ct);

            return Results.Ok(new { reply = res.Text });
        }

        [HttpPost("stream")]
        public async Task StreamAsync(ChatRequest request, CancellationToken ct)
        {
            if(request is null || request.messages.Count == 0)
            {
                await Response.WriteAsync("Send at least one item in 'messages', e.g. { \"messages\": [ { \"role\": \"user\", \"content\": \"Hi\" } ] }", ct);
                return;
            }

            HttpContext.Response.ContentType = "text/event-stream";
            HttpContext.Response.Headers.CacheControl = "no-cache";

            await foreach (var text in Stream(request, ct))
            {
                await HttpContext.Response.WriteAsync($"data: {JsonSerializer.Serialize(text)}\n\n", ct);
                await HttpContext.Response.Body.FlushAsync(ct);
            }

            await Response.WriteAsync("data: [DONE]\n\n", ct);
        }

        private async IAsyncEnumerable<string> Stream(ChatRequest req, [EnumeratorCancellation] CancellationToken ct)
        {
            await foreach (var u in _chatClient.GetStreamingResponseAsync(ToMessages(req), cancellationToken: ct))
                if (!string.IsNullOrEmpty(u.Text)) yield return u.Text;
        }

        private List<ChatMessage> ToMessages(ChatRequest req) => req.messages.Select(m => new ChatMessage(new ChatRole(m.Role), m.content)).ToList();
    }
}
