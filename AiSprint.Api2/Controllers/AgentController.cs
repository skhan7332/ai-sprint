using AiSprint.Api2.Model;
using AiSprint.Api2.Services;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Mvc;

namespace AiSprint.Api2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AgentController : ControllerBase
    {
        private readonly ConversationStore _conversationStore;
        private readonly AIAgent _aiAgent;

        public AgentController(ConversationStore conversationStore, AIAgent aiAgent) 
        {
            _aiAgent = aiAgent;
            _conversationStore = conversationStore;
        }

        [HttpPost("{conversationId}")]
        public async Task<ActionResult<AgentChatResponseModel>> Chat(string conversationId, AgentChatRequestModel request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.message))
            {
                return BadRequest("Send a non-empty 'message', e.g. { \"message\": \"Find me React jobs\" }");
            }

            var session = await _conversationStore.GetOrCreateAsync(conversationId);
            var res = await _aiAgent.RunAsync(request.message, session: session, cancellationToken: ct);
            return new AgentChatResponseModel(res.Text);
        }

    }
}
