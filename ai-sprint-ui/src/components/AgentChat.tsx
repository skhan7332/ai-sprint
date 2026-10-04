import { useEffect, useRef, useState } from 'react'
import type { ChatMessageDto } from '../api';
import { sendAgentMessage } from '../services/agentApi';
import './Chat.css';
import './AgentChat.css';

type Session = {
    id: string;
    title: string;
    messages: ChatMessageDto[];
};

const STORAGE_KEY = 'agent-chat-sessions';

// The server holds the history the agent uses; this copy is only for display.
function loadSessions(): Session[] {
    try {
        return JSON.parse(localStorage.getItem(STORAGE_KEY) ?? '[]');
    } catch {
        return [];
    }
}

function saveSessions(sessions: Session[]) {
    try {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(sessions));
    } catch {
        // storage unavailable (private window etc.): sessions just won't survive a refresh
    }
}

export default function AgentChat() {

    const [sessions, setSessions] = useState<Session[]>(loadSessions);
    const [activeId, setActiveId] = useState<string | null>(() => sessions[0]?.id ?? null);
    const [input, setInput] = useState("");
    const [pendingId, setPendingId] = useState<string | null>(null);

    const active = sessions.find(s => s.id === activeId) ?? null;

    useEffect(() => {
        saveSessions(sessions);
    }, [sessions]);

    function newChat() {
        const session: Session = { id: crypto.randomUUID(), title: 'New chat', messages: [] };
        setSessions(prev => [session, ...prev]);
        setActiveId(session.id);
        setInput("");
    }

    // update one session by id, so a reply lands in the right chat even if the user switched away
    function updateSession(id: string, update: (s: Session) => Session) {
        setSessions(prev => prev.map(s => (s.id === id ? update(s) : s)));
    }

    async function send() {
        const text = input.trim();
        if (!text || !active || pendingId) return;

        const id = active.id;
        updateSession(id, s => ({
            ...s,
            title: s.messages.length === 0 ? text.slice(0, 40) : s.title,
            messages: [...s.messages, { role: 'user', content: text }],
        }));
        setInput("");
        setPendingId(id);

        try {
            const reply = await sendAgentMessage(id, text);
            updateSession(id, s => ({ ...s, messages: [...s.messages, { role: 'assistant', content: reply }] }));
        } catch (err) {
            const message = err instanceof Error ? err.message : String(err);
            updateSession(id, s => ({ ...s, messages: [...s.messages, { role: 'error', content: `Something went wrong: ${message}` }] }));
        } finally {
            setPendingId(null);
        }
    }

    const bottomRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        bottomRef.current?.scrollIntoView({ behavior: "smooth" });
    }, [active?.messages.length, pendingId]);

    return (
        <div className='agent-chat'>
            <aside className='sessions'>
                <button type='button' className='new-chat' onClick={newChat}>
                    + New chat
                </button>
                <ul>
                    {sessions.map(s => (
                        <li key={s.id}>
                            <button
                                type='button'
                                className={s.id === activeId ? 'session active' : 'session'}
                                onClick={() => setActiveId(s.id)}
                                title={s.id}
                            >
                                {s.title}
                            </button>
                        </li>
                    ))}
                </ul>
            </aside>

            <section className='chat'>
                {active ? (
                    <>
                        <div className='messages'>
                            {active.messages.length === 0 && (
                                <p className='empty'>Ask JobScout about jobs, e.g. "Find me remote React jobs".</p>
                            )}
                            {active.messages.map((m, i) => (
                                <div key={i} className={`message ${m.role}`}>
                                    {m.content}
                                </div>
                            ))}
                            {pendingId === active.id && (
                                <div className='message assistant thinking'>Thinking…</div>
                            )}
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
                                placeholder='Message JobScout...'
                                autoFocus
                            />
                            <button type='submit' disabled={!input.trim() || pendingId !== null}>
                                Send
                            </button>
                        </form>
                    </>
                ) : (
                    <div className='messages'>
                        <p className='empty'>Click "New chat" to start a conversation.</p>
                    </div>
                )}
            </section>
        </div>
    )
}
