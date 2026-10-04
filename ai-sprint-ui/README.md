# ai-sprint-ui

The React front end for the AI learning sprint. It's a small chat app that talks to the .NET API in [`../AiSprint.Api2`](../AiSprint.Api2), which in turn talks to a local LLM through Ollama.

It has two pages, switched from the bar at the top:

- **Chat:** a plain chat with the model. Replies stream in token by token, and a Stop button cancels a reply part-way through.
- **Agent Chat:** a chat with the JobScout agent, which searches a list of job postings using tools. Start a new conversation with "+ New chat"; your conversations are listed on the left, and the selected one shows on the right.

Built with React 19, TypeScript and Vite.

## How the pieces fit together

```
Browser (this app, port 5173)
   │  fetch("/api/...")
   ▼
Vite dev server ── proxies /api ──▶ AiSprint.Api2 (port 5241) ──▶ Ollama (port 11434, llama3.2)
```

The app only ever calls relative URLs like `/api/agent/...`. The Vite dev server forwards anything under `/api` to the .NET API (see `vite.config.ts`), so you don't need CORS in development.

## Prerequisites

- **Node.js** 20.19 or newer (or 22.12+), as required by Vite. Check with `node --version`.
- **The .NET API running.** The app does nothing on its own; every message goes through the API. That means you also need:
  - the .NET 9 SDK
  - Ollama, with the `llama3.2` model downloaded

## 1. Start the .NET API first

The full setup, including installing Ollama and downloading the model, is in the [API's README](../AiSprint.Api2/README.md). In short:

```powershell
# one time: download the model (Ollama must be installed and running)
ollama pull llama3.2

# start the API
cd AiSprint.Api2
dotnet run --launch-profile http
```

Check it's up by opening `http://localhost:5241/swagger`.

Use the `http` profile. Under the `https` profile, the API redirects requests to port 7237, the browser follows that redirect outside the proxy, and requests fail with a CORS error.

## 2. Start the UI

In a second terminal:

```powershell
cd ai-sprint-ui
npm install      # first time only
npm run dev
```

Open the URL Vite prints, usually `http://localhost:5173`.

## Scripts

| Command | What it does |
|---|---|
| `npm run dev` | Starts the dev server with hot reload |
| `npm run build` | Type-checks, then builds to `dist/` |
| `npm run lint` | Runs ESLint |
| `npm run preview` | Serves the built `dist/` folder locally |
| `npm run gen:api` | Regenerates the TypeScript API types (see below) |

## API types are generated, not written by hand

The types in `src/api/` (`ChatRequest`, `AgentChatRequestModel`, and so on) are generated from the API's OpenAPI document by [`@hey-api/openapi-ts`](https://heyapi.dev). The settings are in `openapi-ts.config.ts`.

When you change a request or response model in the .NET API:

1. Restart the API so it serves the updated OpenAPI document.
2. Run `npm run gen:api`.

Don't edit `src/api/types.gen.ts` by hand; the next generation overwrites it.

## Project structure

```
src/
  api/                  generated API types (do not edit)
  services/
    chatApi.ts          streaming call to /api/chat/stream (Server-Sent Events)
    agentApi.ts         call to /api/agent/{conversationId}
  components/
    Chat.tsx            plain chat page
    AgentChat.tsx       agent page with the conversation list
  App.tsx               top bar that switches between the two pages
vite.config.ts          dev server, including the /api proxy to port 5241
```

## Things worth knowing

- **The two pages handle history differently,** and that's the point of comparing them:
  - **Chat** sends the whole conversation with every request; the server keeps nothing.
  - **Agent Chat** sends only the new message. The API remembers the conversation by its id.
- **Agent Chat keeps a display copy** of your conversations in the browser's `localStorage`, so the list survives a page refresh. The API only keeps its copy in memory. After an API restart, old chats still show in the browser, but the agent no longer remembers them. Start a new chat after restarting the API.
- **Agent replies arrive all at once,** not streamed, because the agent endpoint returns the complete answer.

## Troubleshooting

| Problem | Likely cause |
|---|---|
| "Chat request failed: 500" or "Agent request failed: 500", with a proxy error in the Vite terminal | The API isn't running, or not on port 5241 |
| A CORS error in the browser console | The API was started with the `https` profile. Restart it with `--launch-profile http`. |
| "Agent request failed: 404" | The API is running an old build. Restart it. |
| A reply takes a long time the first time | Ollama is loading the model into memory |
| The agent mentions jobs or links that don't exist | A limitation of the small `llama3.2` model; see the API README for trying a larger one |
| Type errors after changing the API | Regenerate the types with `npm run gen:api` while the API is running |
