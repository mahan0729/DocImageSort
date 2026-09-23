import { useEffect, useState, useCallback } from 'react'
import { processingLogsApi, type ProcessingLog } from '../api/processingLogs'
import './ProcessingLogPage.css'

const LEVEL_FILTERS = ['All', 'Info', 'Warning', 'Error']

function formatDate(iso: string) {
  const d = new Date(iso)
  return d.toLocaleString('en-US', {
    month: 'short', day: 'numeric',
    hour: 'numeric', minute: '2-digit',
    hour12: true,
  })
}

export function ProcessingLogPage() {
  const [logs, setLogs]           = useState<ProcessingLog[]>([])
  const [levelFilter, setLevel]   = useState('All')
  const [search, setSearch]       = useState('')
  const [loading, setLoading]     = useState(true)
  const [error, setError]         = useState('')

  const load = useCallback(async (level: string, term: string) => {
    setLoading(true)
    setError('')
    try {
      const data = await processingLogsApi.getAll(
        level === 'All' ? undefined : level,
        term || undefined
      )
      setLogs(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load log.')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { load(levelFilter, search) }, [levelFilter, load])

  useEffect(() => {
    const id = setTimeout(() => load(levelFilter, search), 300)
    return () => clearTimeout(id)
  }, [search, levelFilter, load])

  return (
    <div>
      <h1>Processing Log</h1>
      <p style={{ color: 'var(--text-muted)', marginBottom: 20 }}>
        Full audit trail of every pipeline action — newest first.
      </p>

      <div className="log-level-tabs">
        {LEVEL_FILTERS.map(l => (
          <button
            key={l}
            className={`level-tab${levelFilter === l ? ' active' : ''}${l !== 'All' ? ` level-${l.toLowerCase()}` : ''}`}
            onClick={() => setLevel(l)}
          >
            {l}
          </button>
        ))}
      </div>

      <div className="log-toolbar">
        <input
          className="log-search"
          type="text"
          placeholder="Search by file name, action, or message…"
          value={search}
          onChange={e => setSearch(e.target.value)}
        />
        <button
          className="btn btn-outline"
          onClick={() => load(levelFilter, search)}
          title="Refresh"
        >
          ↻ Refresh
        </button>
      </div>

      {error && <div className="log-error">{error}</div>}

      {loading ? (
        <div className="log-empty">Loading…</div>
      ) : logs.length === 0 ? (
        <div className="log-empty">
          {search ? 'No log entries match your search.' : 'No log entries yet.'}
        </div>
      ) : (
        <div className="log-table-wrap">
          <table className="log-table">
            <thead>
              <tr>
                <th>Time</th>
                <th>Level</th>
                <th>File</th>
                <th>Action</th>
                <th>Outcome</th>
                <th>Message</th>
              </tr>
            </thead>
            <tbody>
              {logs.map(l => (
                <tr key={l.id} className={`log-row-${l.level.toLowerCase()}`}>
                  <td className="log-time">{formatDate(l.createdDate)}</td>
                  <td>
                    <span className={`level-badge level-${l.level.toLowerCase()}`}>{l.level}</span>
                  </td>
                  <td className="log-file" title={l.fileName}>{l.fileName}</td>
                  <td className="log-action">{l.action}</td>
                  <td>
                    <span className={`outcome-badge outcome-${l.outcome.toLowerCase()}`}>{l.outcome}</span>
                  </td>
                  <td className="log-message">{l.message}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
