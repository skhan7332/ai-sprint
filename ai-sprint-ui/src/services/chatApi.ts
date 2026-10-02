import type { ChatMessageDto, PostApiChatStreamData } from "../api";

export async function streamChat(
  messages: ChatMessageDto[],
  onToken: (t: string) => void,
  signal?: AbortSignal,
) {
  const streamURL: PostApiChatStreamData["url"] = "/api/chat/stream";
  const res = await fetch(streamURL, {
    method: "Post",
    headers: {"Content-Type": "application/json"},
    body: JSON.stringify({messages}),
    signal,
  });
  if (!res.ok || !res.body) throw new Error(`Chat request failed: ${res.status}`);
  const reader = res.body.pipeThrough(new TextDecoderStream()).getReader();
  let buffer = "";
  for (;;) {
    const { value, done } = await reader.read();
    if (done) return;
    buffer += value;
    const events = buffer.split("\n\n");
    buffer = events.pop()!; // keep the incomplete last part for the next read
    for (const e of events) {
      const data = e.replace(/^data: /, "");
      if (data === "[DONE]") return;
      onToken(JSON.parse(data));
    }
  }

}
