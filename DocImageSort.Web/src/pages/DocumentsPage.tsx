import { useEffect, useState, useCallback } from 'react'
import { documentsApi, type Document } from '../api/documents'
import { borrowersApi, type Borrower } from '../api/borrowers'
import './DocumentsPage.css'

const STATUS_FILTERS = ['All', 'Pending', 'Classified', 'Converted', 'Filed', 'Error']

// ── Correct-type modal ────────────────────────────────────────────────────────

interface CorrectTypeModalProps {
  doc: Document
  docTypes: string[]
  onSave: (updated: Document) => void
  onClose: () => void
}

function CorrectTypeModal({ doc, docTypes, onSave, onClose }: CorrectTypeModalProps) {
  const [docType, setDocType] = useState(doc.documentType)
  const [saving, setSaving]   = useState(false)
  const [error, setError]     = useState('')

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError('')
    try {
      const updated = await documentsApi.correctType(doc.id, docType)
      onSave(updated)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Save failed.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal" onClick={e => e.stopPropagation()}>
        <h2>Correct Document Type</h2>
        <p className="modal-subtitle">{doc.originalFileName}</p>

        {doc.aiClassificationNotes && (
          <div className="ai-notes">
            <strong>AI notes:</strong> {doc.aiClassificationNotes}
          </div>
        )}

        <form onSubmit={handleSubmit}>
          {error && <div className="error-msg">{error}</div>}

          <div className="form-group">
            <label htmlFor="docType">Document Type</label>
            <select
              id="docType"
              value={docType}
              onChange={e => setDocType(e.target.value)}
              autoFocus
            >
              {docTypes.map(t => (
                <option key={t} value={t}>{t}</option>
              ))}
            </select>
          </div>

          <div className="modal-footer">
            <button type="button" className="btn btn-outline" onClick={onClose} disabled={saving}>
              Cancel
            </button>
            <button type="submit" className="btn btn-primary" disabled={saving}>
              {saving ? 'Saving…' : 'Save'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

// ── Assign-borrower modal ─────────────────────────────────────────────────────

interface AssignModalProps {
  doc: Document
  onSave: (updated: Document) => void
  onClose: () => void
}

function AssignModal({ doc, onSave, onClose }: AssignModalProps) {
  const [borrowers, setBorrowers]   = useState<Borrower[]>([])
  const [search, setSearch]         = useState('')
  const [selectedId, setSelectedId] = useState<number | ''>('')
  const [saving, setSaving]         = useState(false)
  const [error, setError]           = useState('')

  // Debounced borrower search
  useEffect(() => {
    const id = setTimeout(async () => {
      try {
        const results = await borrowersApi.getAll(search || undefined)
        setBorrowers(results)
        if (results.length === 1) setSelectedId(results[0].id)
        else setSelectedId('')
      } catch {
        // ignore transient errors
      }
    }, 300)
    return () => clearTimeout(id)
  }, [search])

  // Load all on mount
  useEffect(() => {
    borrowersApi.getAll().then(setBorrowers).catch(() => {})
  }, [])

  async function handleAssign(e: React.FormEvent) {
    e.preventDefault()
    if (!selectedId) { setError('Select a borrower.'); return }
    setSaving(true)
    setError('')
    try {
      const updated = await documentsApi.assign(doc.id, selectedId as number)
      onSave(updated)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Assign failed.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal" onClick={e => e.stopPropagation()}>
        <h2>Assign Borrower</h2>
        <p className="modal-subtitle">{doc.originalFileName} — {doc.documentType}</p>

        <form onSubmit={handleAssign}>
          {error && <div className="error-msg">{error}</div>}

          <div className="form-group">
            <label htmlFor="borrowerSearch">Search Borrower</label>
            <input
              id="borrowerSearch"
              type="text"
              placeholder="Last name, first name, or loan #…"
              value={search}
              onChange={e => setSearch(e.target.value)}
              autoFocus
            />
          </div>

          <div className="form-group">
            <label htmlFor="borrowerSelect">Borrower</label>
            <select
              id="borrowerSelect"
              value={selectedId}
              onChange={e => setSelectedId(Number(e.target.value))}
            >
              <option value="">— select —</option>
              {borrowers.map(b => (
                <option key={b.id} value={b.id}>
                  {b.folderName} — Loan #{b.loanNumber}
                </option>
              ))}
            </select>
          </div>

          <div className="modal-footer">
            <button type="button" className="btn btn-outline" onClick={onClose} disabled={saving}>
              Cancel
            </button>
            <button
              type="submit"
              className="btn btn-primary"
              disabled={saving || !selectedId}
            >
              {saving ? 'Filing…' : 'Assign & File'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

// ── Main page ─────────────────────────────────────────────────────────────────

export function DocumentsPage() {
  const [docs, setDocs]           = useState<Document[]>([])
  const [docTypes, setDocTypes]   = useState<string[]>([])
  const [statusFilter, setStatus] = useState('All')
  const [search, setSearch]       = useState('')
  const [loading, setLoading]     = useState(true)
  const [error, setError]         = useState('')
  const [correcting, setCorrecting] = useState<Document | null>(null)
  const [assigning, setAssigning]   = useState<Document | null>(null)

  const load = useCallback(async (status: string, term: string) => {
    setLoading(true)
    setError('')
    try {
      const data = await documentsApi.getAll(
        status === 'All' ? undefined : status,
        term || undefined
      )
      setDocs(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load documents.')
    } finally {
      setLoading(false)
    }
  }, [])

  // Load doc types once
  useEffect(() => {
    documentsApi.getTypes().then(setDocTypes).catch(() => {})
  }, [])

  useEffect(() => { load(statusFilter, search) }, [statusFilter, load])

  // Debounce search
  useEffect(() => {
    const id = setTimeout(() => load(statusFilter, search), 300)
    return () => clearTimeout(id)
  }, [search, statusFilter, load])

  function handleUpdated(updated: Document) {
    setDocs(prev => prev.map(d => d.id === updated.id ? updated : d))
    setCorrecting(null)
    setAssigning(null)
  }

  return (
    <div>
      <h1>Documents</h1>

      <div className="status-tabs">
        {STATUS_FILTERS.map(s => (
          <button
            key={s}
            className={`status-tab${statusFilter === s ? ' active' : ''}`}
            onClick={() => setStatus(s)}
          >
            {s}
          </button>
        ))}
      </div>

      <div className="docs-toolbar">
        <input
          className="search-input"
          type="text"
          placeholder="Search by file name or document type…"
          value={search}
          onChange={e => setSearch(e.target.value)}
        />
      </div>

      {error && <div className="error-msg">{error}</div>}

      {loading ? (
        <div className="empty-state">Loading…</div>
      ) : docs.length === 0 ? (
        <div className="empty-state">
          {search
            ? 'No documents match your search.'
            : statusFilter === 'All'
            ? 'No documents yet — drop a file into the watch folder to start the pipeline.'
            : `No ${statusFilter} documents.`}
        </div>
      ) : (
        <div className="docs-table-wrap">
          <table className="docs-table">
            <thead>
              <tr>
                <th>Original File</th>
                <th>Document Type</th>
                <th>Status</th>
                <th>Borrower</th>
                <th>Document Date</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {docs.map(d => (
                <tr key={d.id}>
                  <td><span className="file-name" title={d.originalFileName}>{d.originalFileName}</span></td>
                  <td>{d.documentType || '—'}</td>
                  <td>
                    <span className={`status-badge status-${d.status}`}>{d.status}</span>
                  </td>
                  <td>{d.borrowerName ?? <span style={{ color: 'var(--text-muted)' }}>Unassigned</span>}</td>
                  <td>{d.documentDate ?? '—'}</td>
                  <td className="actions-cell">
                    {/* Correct type — always available */}
                    <button
                      className="icon-btn"
                      title="Correct document type"
                      onClick={() => setCorrecting(d)}
                    >
                      ✏️
                    </button>
                    {/* Assign borrower — only for unassigned docs */}
                    {!d.borrowerId && (
                      <button
                        className="icon-btn"
                        title="Assign to borrower"
                        onClick={() => setAssigning(d)}
                      >
                        📁
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {correcting && (
        <CorrectTypeModal
          doc={correcting}
          docTypes={docTypes}
          onSave={handleUpdated}
          onClose={() => setCorrecting(null)}
        />
      )}

      {assigning && (
        <AssignModal
          doc={assigning}
          onSave={handleUpdated}
          onClose={() => setAssigning(null)}
        />
      )}
    </div>
  )
}
