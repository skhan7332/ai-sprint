import { useState } from 'react'
import './App.css'
import Chat from './components/Chat'
import AgentChat from './components/AgentChat'

type Page = 'chat' | 'agent';

function App() {
  const [page, setPage] = useState<Page>('agent')

  return (
    <div className='app'>
      <nav className='app-nav'>
        <button type='button' className={page === 'chat' ? 'active' : ''} onClick={() => setPage('chat')}>
          Chat
        </button>
        <button type='button' className={page === 'agent' ? 'active' : ''} onClick={() => setPage('agent')}>
          Agent Chat
        </button>
      </nav>
      <main className='app-main'>
        {page === 'chat' ? <Chat /> : <AgentChat />}
      </main>
    </div>
  )
}

export default App
