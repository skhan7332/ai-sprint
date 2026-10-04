using System.Collections.Concurrent;
using Microsoft.Agents.AI;

namespace AiSprint.Api2.Services
{
    // Keeps one agent session (the conversation history) per conversation id, in memory.
    public class ConversationStore
    {
        private readonly AIAgent _agent;
        private readonly ConcurrentDictionary<string, AgentSession> _sessions = new();

        public ConversationStore(AIAgent agent)
        {
            _agent = agent;
        }

        public async Task<AgentSession> GetOrCreateAsync(string conversationId)
        {
            if (_sessions.TryGetValue(conversationId, out var existing))
                return existing;

            var created = await _agent.CreateSessionAsync();

            // if another request created a session for this id meanwhile, keep theirs
            return _sessions.GetOrAdd(conversationId, created);
        }
    }
}
