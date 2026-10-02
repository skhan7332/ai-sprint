import { useEffect, useRef, useState } from 'react'
import type { ChatMessageDto } from '../api';
import { streamChat } from '../services/chatApi';
import './Chat.css';


export default function Chat() {

    const [messages, setMessages] = useState<ChatMessageDto[]>([]);
    const [input, setInput] = useState("")
    const [isStreaming, setisStreaming] = useState(false)
    const abortRef = useRef<AbortController | null>(null);

    async function send() {
        const text = input.trim();
        if (!text || isStreaming) return;

        const history = [...messages, { role: "user", content: text }];
        setMessages([...history, { role: "assistant", content: "" }]);
        setInput("");
        setisStreaming(true);

        const controller = new AbortController();
        abortRef.current = controller;

        try {
            await streamChat(history, (token) => {
                setMessages((prev) => {
                    const last = prev[prev.length - 1];
                    return [...prev.slice(0, -1), { ...last, content: last.content + token }]
                })
            }, controller.signal)
        } catch (err) {
            // an abort is the user pressing Stop, not a failure
            if (!controller.signal.aborted) throw err;
        } finally {
            abortRef.current = null;
            setisStreaming(false);
        }
    }

    function stop() {
        abortRef.current?.abort();
    }

    const bottomRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        bottomRef.current?.scrollIntoView({ behavior: "smooth" });
    }, [messages]);

    return (
        <div className='chat'>
            <div className='messages'>
                {
                    messages.map((m, i) => (
                        <div key={i} className={`message ${m.role}`}>
                            {m.content}
                        </div>
                    ))
                }
                <div ref={bottomRef} />
            </div>
            <form
                className='composer'
                onSubmit={(e) => {
                    e.preventDefault();
                    send();
                }}
            >
                <input
                    value={input}
                    onChange={(e) => setInput(e.target.value)}
                    placeholder='Type message to send...'
                    autoFocus
                />
                {isStreaming ? (
                    <button type='button' className='stop' onClick={stop}>
                        Stop
                    </button>
                ) : (
                    <button type='submit' disabled={!input.trim()}>
                        Send
                    </button>
                )}
            </form>
        </div>
    )
}
