import { useState } from 'react'
import { BorrowersPage } from './pages/BorrowersPage'
import { DocumentsPage } from './pages/DocumentsPage'
import { MergePage } from './pages/MergePage'
import { SettingsPage } from './pages/SettingsPage'
import { ProcessingLogPage } from './pages/ProcessingLogPage'
import { HelpPage } from './pages/HelpPage'
import './App.css'

type Page = 'dashboard' | 'borrowers' | 'documents' | 'merge' | 'log' | 'settings' | 'help'

const NAV_LINKS: { label: string; page: Page }[] = [
  { label: 'Borrowers',      page: 'borrowers' },
  { label: 'Documents',      page: 'documents' },
  { label: 'Merge',          page: 'merge' },
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
    icon: '🔗',
    title: 'Merge Documents',
    description: 'Combine multiple pages of the same document into one PDF. AI detects page order automatically.',
    action: 'Merge Pages',
    page: 'merge',
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


function Dashboard({ onNavigate }: { onNavigate: (page: Page) => void }) {
  return (
    <>
      <h1>Dashboard</h1>
      <p style={{ color: 'var(--text-muted)' }}>
        Drop a document into your watch folder to start the pipeline — AI classifies, converts, and routes it automatically.
      </p>
      <div className="dashboard-grid">
        {FEATURES.map(f => (
          <div key={f.page} className="card feature-card" style={{ cursor: 'pointer' }} onClick={() => onNavigate(f.page)}>
            <div className="card-icon">{f.icon}</div>
            <h3>{f.title}</h3>
            <p>{f.description}</p>
            <div className="card-footer">
              <button className="btn btn-outline" onClick={e => { e.stopPropagation(); onNavigate(f.page); }}>
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
  const [page, setPage]       = useState<Page>('dashboard')
  const [merging, setMerging] = useState(false)

  function navigate(p: Page) {
    if (merging) return // block navigation while merge is in progress
    setPage(p)
  }

  function renderPage() {
    switch (page) {
      case 'borrowers': return <BorrowersPage />
      case 'documents': return <DocumentsPage />
      case 'merge':     return <MergePage onMergingChange={setMerging} />
      case 'log':       return <ProcessingLogPage />
      case 'settings':  return <SettingsPage />
      case 'help':      return <HelpPage />
      default:          return <Dashboard onNavigate={navigate} />
    }
  }

  return (
    <>
      <header className="app-header">
        <button className="brand-btn" onClick={() => navigate('dashboard')} disabled={merging}>DocImageSort</button>
        <nav>
          {NAV_LINKS.map(link => (
            <button
              key={link.page}
              className={`nav-btn${page === link.page ? ' active' : ''}`}
              onClick={() => navigate(link.page)}
              disabled={merging}
              title={merging ? 'Merge in progress — please wait…' : undefined}
            >
              {link.label}
            </button>
          ))}
        </nav>
        {merging && <span style={{ color: 'rgba(255,255,255,0.85)', fontSize: '13px', marginLeft: '12px' }}>Merging…</span>}
      </header>

      <main className="app-main">
        {renderPage()}
      </main>

      <footer className="app-footer">
        DocImageSort LLC &copy; {new Date().getFullYear()}, all rights reserved
        <div style={{ fontSize: '11px', opacity: 0.6, marginTop: '2px' }}>v0.0.0</div>
      </footer>
    </>
  )
}

export default App
