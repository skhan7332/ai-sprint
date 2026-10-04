import type {
  PostApiAgentByConversationIdData,
  PostApiAgentByConversationIdResponse,
} from "../api";

// Sends only the new message; the API keeps the conversation history per conversationId.
export async function sendAgentMessage(
  conversationId: string,
  message: string,
  signal?: AbortSignal,
): Promise<string> {
  const url = `/api/agent/${encodeURIComponent(conversationId)}`;
  const body: PostApiAgentByConversationIdData["body"] = { message };

  const res = await fetch(url, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
    signal,
  });
  if (!res.ok) throw new Error(`Agent request failed: ${res.status}`);

  const data: PostApiAgentByConversationIdResponse = await res.json();
  return data.reply;
}
