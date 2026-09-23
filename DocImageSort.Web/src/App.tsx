import './App.css'

const NAV_LINKS = [
  { label: 'Borrowers',      href: '#borrowers' },
  { label: 'Documents',      href: '#documents' },
  { label: 'Processing Log', href: '#log' },
  { label: 'Settings',       href: '#settings' },
]

const FEATURES = [
  {
    icon: '📁',
    title: 'Borrowers',
    description: 'Create and manage borrower profiles. Each borrower gets their own folder for filed documents.',
    action: 'Manage Borrowers',
    href: '#borrowers',
  },
  {
    icon: '📄',
    title: 'Documents',
    description: 'Review AI-classified documents, confirm document types, and assign them to borrowers.',
    action: 'Review Documents',
    href: '#documents',
  },
  {
    icon: '📋',
    title: 'Processing Log',
    description: 'View the full audit trail of every file ingested, classified, converted, renamed, and routed.',
    action: 'View Log',
    href: '#log',
  },
  {
    icon: '⚙️',
    title: 'Settings',
    description: 'Configure your drop folder, files folder, and Azure storage feature flag.',
    action: 'Open Settings',
    href: '#settings',
  },
]

function App() {
  return (
    <>
      <header className="app-header">
        <span className="brand">DocImageSort</span>
        <nav>
          {NAV_LINKS.map(link => (
            <a key={link.href} href={link.href}>{link.label}</a>
          ))}
        </nav>
      </header>

      <main className="app-main">
        <h1>Dashboard</h1>
        <p style={{ color: 'var(--text-muted)' }}>
          Drop a document into your watch folder to start the pipeline — AI classifies, converts, and routes it automatically.
        </p>

        <div className="dashboard-grid">
          {FEATURES.map(f => (
            <div key={f.href} className="card feature-card">
              <div className="card-icon">{f.icon}</div>
              <h3>{f.title}</h3>
              <p>{f.description}</p>
              <div className="card-footer">
                <a href={f.href} className="btn btn-outline">{f.action}</a>
              </div>
            </div>
          ))}
        </div>
      </main>

      <footer className="app-footer">
        DocImageSort &copy; {new Date().getFullYear()}, all rights reserved
      </footer>
    </>
  )
}

export default App
