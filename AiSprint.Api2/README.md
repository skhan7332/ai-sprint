# AiSprint.Api2

A learning project for building AI features in .NET. It's an ASP.NET Core Web API that talks to a local LLM through [Ollama](https://ollama.com), and it grows exercise by exercise, from a plain chat endpoint to an agent with tools.

Everything runs on your own machine. You don't need an API key or a paid model.

## What it covers

| Topic | Where to look |
|---|---|
| `IChatClient` from `Microsoft.Extensions.AI`, with a middleware pipeline (caching, logging, OpenTelemetry) | `Program.cs` |
| Streaming responses to the browser with Server-Sent Events | `Controllers/ChatController.cs` |
| An agent built with Microsoft Agent Framework (`Microsoft.Agents.AI`) | `Program.cs` |
| Tool calling: the agent searches a job list through C# methods | `Model/JobTools.cs` |
| An agent used as a tool by another agent (JobScout delegates to FitReviewer) | `Program.cs` |
| Conversation history kept on the server, per conversation id | `Services/ConversationStore.cs` |
| Tracing model calls, including token counts | console output while the API runs |

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (check with `dotnet --version`)
- Ollama, with the `llama3.2` model downloaded (steps below)
- About 2 GB of free disk space for the model

## 1. Install Ollama

Ollama runs open models locally and serves them on `http://localhost:11434`.

**Windows**, with winget:

```powershell
winget install Ollama.Ollama
```

Or download the installer from [ollama.com/download](https://ollama.com/download). macOS and Linux installers are on the same page.

On Windows and macOS, Ollama starts automatically in the background after installation. If it isn't running, start it yourself:

```powershell
ollama serve
```

Check that it's up:

```powershell
curl http://localhost:11434
```

This should print `Ollama is running`.

## 2. Download the model

```powershell
ollama pull llama3.2
```

This downloads about 2 GB. Confirm it's there, and try it once from the terminal:

```powershell
ollama list
ollama run llama3.2 "Say hello in one sentence."
```

`llama3.2` is a small 3B model. It's fast on an ordinary laptop, but its tool calling is unreliable. It sometimes skips a tool or invents details, such as links that aren't in the data. That's a useful thing to see while learning. To compare, pull a larger model that supports tools, such as `ollama pull qwen2.5:7b`, and change the model name in the settings (next section).

## 3. Configure

The Ollama connection is set in `appsettings.json`:

```json
"OllamaConfig": {
  "endpoint": "http://localhost:11434",
  "model": "llama3.2"
}
```

The model must already be downloaded with `ollama pull`, or every request will fail.

## 4. Run

```powershell
cd AiSprint.Api2
dotnet run --launch-profile http
```

- **The API** is at `http://localhost:5241`.
- **Swagger UI** is at `http://localhost:5241/swagger`, where you can try every endpoint.
- **The OpenAPI document** is at `http://localhost:5241/openapi/v1.json`. The React app in `ai-sprint-ui` generates its TypeScript types from it.

Use the `http` profile. The `https` profile redirects to port 7237, which breaks the React app's dev proxy.

## Endpoints

| Method and path | Body | What it does |
|---|---|---|
| `POST /api/chat` | `{ "messages": [ { "role": "user", "content": "Hi" } ] }` | Returns the whole reply at once. The client sends the full history. |
| `POST /api/chat/stream` | same as above | Streams the reply as Server-Sent Events (`data: "token"` lines, ending with `data: [DONE]`). |
| `POST /api/agent/{conversationId}` | `{ "message": "Find me remote React jobs" }` | Asks the JobScout agent. The client sends only the new message; the server remembers the conversation for that id. |

To try the agent's memory, send two messages to the same id, then the second one to a new id:

```http
POST http://localhost:5241/api/agent/abc
Content-Type: application/json

{ "message": "Find me React jobs" }

###
POST http://localhost:5241/api/agent/abc
Content-Type: application/json

{ "message": "Which of those are remote?" }
```

The second message only makes sense because the server remembered the first one. Send the same follow-up to `/api/agent/xyz` and the agent won't know what "those" means.

## How it's put together

There are two chat clients, registered in `Program.cs`:

- **The default client** (used by `ChatController`) has caching, logging and OpenTelemetry. An identical request returns the cached answer without calling the model.
- **The `"agent"` client** has no cache. With tools, a cached answer could be stale: the tools would never run again, so changes in the data wouldn't show up.

The agent setup:

- **JobScout** is the agent behind `/api/agent`. It has three tools from `JobTools`: `SearchJobs`, `ListAllJobs` and `GetJobDetails`. They read `Resources/jobs.json`, a list of 10 made-up job postings.
- **FitReviewer** is a second agent, given to JobScout as a fourth tool. JobScout passes it a job's details, and it lists the skill gaps.
- **ConversationStore** keeps one `AgentSession` per conversation id, which holds that conversation's history.

## Watching what happens

While the API runs, the console prints:

- **A `[tool] ...` line** each time the agent calls one of the job tools.
- **An OpenTelemetry span** for each model call. Look for `gen_ai.usage.input_tokens` and `gen_ai.usage.output_tokens` to see the token counts.

Send the same `/api/chat` request twice: the second time there's no new span, because the cache answered before the request reached the model.

## Known limitations

These are deliberate simplifications for learning:

- **Everything is kept in memory.** Restarting the API forgets all agent conversations and empties the cache.
- **No authentication or rate limiting.** Anyone who can reach the API can use the model.
- **The job data is fake** and fixed in `Resources/jobs.json`.

## Troubleshooting

| Problem | Likely cause |
|---|---|
| Connection refused to `localhost:11434` | Ollama isn't running. Start it with `ollama serve`. |
| `model "llama3.2" not found` | The model isn't downloaded. Run `ollama pull llama3.2`. |
| The first request is very slow | Ollama is loading the model into memory. Later requests are faster. |
| No spans in the console | The API wasn't fully restarted after a code change. Hot reload doesn't re-run startup code. |
| The agent invents jobs or links | A limitation of the small model. Try a larger one (see step 2). |
