import { useState } from 'react'
import { BorrowersPage } from './pages/BorrowersPage'
import { DocumentsPage } from './pages/DocumentsPage'
import { SettingsPage } from './pages/SettingsPage'
import { ProcessingLogPage } from './pages/ProcessingLogPage'
import { HelpPage } from './pages/HelpPage'
import './App.css'

type Page = 'dashboard' | 'borrowers' | 'documents' | 'log' | 'settings' | 'help'

const NAV_LINKS: { label: string; page: Page }[] = [
  { label: 'Borrowers',      page: 'borrowers' },
  { label: 'Documents',      page: 'documents' },
  { label: 'Processing Log', page: 'log' },
  { label: 'Settings',       page: 'settings' },
  { label: 'Help',           page: 'help' },
]

const FEATURES: { icon: string; title: string; description: string; action: string; page: Page }[] = [
  {
    icon: '📁',
    title: 'Borrowers',
    description: 'Create and manage borrower profiles. Each borrower gets their own folder for filed documents.',
    action: 'Manage Borrowers',
    page: 'borrowers',
  },
  {
    icon: '📄',
    title: 'Documents',
    description: 'Review AI-classified documents, confirm document types, and assign them to borrowers.',
    action: 'Review Documents',
    page: 'documents',
  },
  {
    icon: '📋',
    title: 'Processing Log',
    description: 'View the full audit trail of every file ingested, classified, converted, renamed, and routed.',
    action: 'View Log',
    page: 'log',
  },
  {
    icon: '⚙️',
    title: 'Settings',
    description: 'Configure your drop folder, files folder, and Azure storage feature flag.',
    action: 'Open Settings',
    page: 'settings',
  },
]

function ComingSoon({ title }: { title: string }) {
  return (
    <div>
      <h1>{title}</h1>
      <p style={{ color: 'var(--text-muted)' }}>This section is coming soon.</p>
    </div>
  )
}

function Dashboard({ onNavigate }: { onNavigate: (page: Page) => void }) {
  return (
    <>
      <h1>Dashboard</h1>
      <p style={{ color: 'var(--text-muted)' }}>
        Drop a document into your watch folder to start the pipeline — AI classifies, converts, and routes it automatically.
      </p>
      <div className="dashboard-grid">
        {FEATURES.map(f => (
          <div key={f.page} className="card feature-card">
            <div className="card-icon">{f.icon}</div>
            <h3>{f.title}</h3>
            <p>{f.description}</p>
            <div className="card-footer">
              <button className="btn btn-outline" onClick={() => onNavigate(f.page)}>
                {f.action}
              </button>
            </div>
          </div>
        ))}
      </div>
    </>
  )
}

function App() {
  const [page, setPage] = useState<Page>('dashboard')

  function renderPage() {
    switch (page) {
      case 'borrowers': return <BorrowersPage />
      case 'documents': return <DocumentsPage />
      case 'log':       return <ProcessingLogPage />
      case 'settings':  return <SettingsPage />
      case 'help':      return <HelpPage />
      default:          return <Dashboard onNavigate={setPage} />
    }
  }

  return (
    <>
      <header className="app-header">
        <button className="brand-btn" onClick={() => setPage('dashboard')}>DocImageSort</button>
        <nav>
          {NAV_LINKS.map(link => (
            <button
              key={link.page}
              className={`nav-btn${page === link.page ? ' active' : ''}`}
              onClick={() => setPage(link.page)}
            >
              {link.label}
            </button>
          ))}
        </nav>
      </header>

      <main className="app-main">
        {renderPage()}
      </main>

      <footer className="app-footer">
        DocImageSort &copy; {new Date().getFullYear()}, all rights reserved
      </footer>
    </>
  )
}

export default App
