import { useEffect, useState, useCallback } from 'react'
import { borrowersApi, type Borrower, type BorrowerRequest } from '../api/borrowers'
import './BorrowersPage.css'

// ── Form modal ────────────────────────────────────────────────────────────────

interface FormModalProps {
  borrower?: Borrower
  onSave: () => void
  onClose: () => void
}

function BorrowerFormModal({ borrower, onSave, onClose }: FormModalProps) {
  const [lastName, setLastName]   = useState(borrower?.lastName   ?? '')
  const [firstName, setFirstName] = useState(borrower?.firstName  ?? '')
  const [loanNumber, setLoanNumber] = useState(borrower?.loanNumber ?? '')
  const [isPrimary, setIsPrimary] = useState(borrower?.isPrimaryBorrower ?? true)
  const [saving, setSaving]       = useState(false)
  const [error, setError]         = useState('')

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')

    if (!lastName.trim() || !firstName.trim() || !loanNumber.trim()) {
      setError('All fields are required.')
      return
    }

    const req: BorrowerRequest = {
      lastName:          lastName.trim(),
      firstName:         firstName.trim(),
      loanNumber:        loanNumber.trim(),
      isPrimaryBorrower: isPrimary,
    }

    setSaving(true)
    try {
      if (borrower) {
        await borrowersApi.update(borrower.id, req)
      } else {
        await borrowersApi.create(req)
      }
      onSave()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Save failed.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal" onClick={e => e.stopPropagation()}>
        <h2>{borrower ? 'Edit Borrower' : 'Add Borrower'}</h2>
        <form onSubmit={handleSubmit}>
          {error && <div className="error-msg">{error}</div>}

          <div className="form-group">
            <label htmlFor="lastName">Last Name</label>
            <input
              id="lastName"
              type="text"
              value={lastName}
              onChange={e => setLastName(e.target.value)}
              autoFocus
            />
          </div>

          <div className="form-group">
            <label htmlFor="firstName">First Name</label>
            <input
              id="firstName"
              type="text"
              value={firstName}
              onChange={e => setFirstName(e.target.value)}
            />
          </div>

          <div className="form-group">
            <label htmlFor="loanNumber">Loan Number</label>
            <input
              id="loanNumber"
              type="text"
              value={loanNumber}
              onChange={e => setLoanNumber(e.target.value)}
            />
          </div>

          <div className="form-group checkbox-group">
            <input
              id="isPrimary"
              type="checkbox"
              checked={isPrimary}
              onChange={e => setIsPrimary(e.target.checked)}
            />
            <label htmlFor="isPrimary">Primary borrower</label>
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

// ── Delete confirm modal ──────────────────────────────────────────────────────

interface DeleteModalProps {
  borrower: Borrower
  onConfirm: () => void
  onClose: () => void
}

function DeleteModal({ borrower, onConfirm, onClose }: DeleteModalProps) {
  const [deleting, setDeleting] = useState(false)
  const [error, setError]       = useState('')

  async function handleDelete() {
    setDeleting(true)
    try {
      await borrowersApi.delete(borrower.id)
      onConfirm()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Delete failed.')
      setDeleting(false)
    }
  }

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal confirm-modal" onClick={e => e.stopPropagation()}>
        <h2>Delete Borrower?</h2>
        <p>
          <strong>{borrower.folderName}</strong> — Loan #{borrower.loanNumber}
          <br />This cannot be undone.
        </p>
        {error && <div className="error-msg" style={{ marginTop: 12 }}>{error}</div>}
        <div className="modal-footer">
          <button className="btn btn-outline" onClick={onClose} disabled={deleting}>Cancel</button>
          <button
            className="btn btn-primary"
            style={{ background: '#dc2626' }}
            onClick={handleDelete}
            disabled={deleting}
          >
            {deleting ? 'Deleting…' : 'Delete'}
          </button>
        </div>
      </div>
    </div>
  )
}

// ── Main page ─────────────────────────────────────────────────────────────────

export function BorrowersPage() {
  const [borrowers, setBorrowers]   = useState<Borrower[]>([])
  const [search, setSearch]         = useState('')
  const [loading, setLoading]       = useState(true)
  const [error, setError]           = useState('')
  const [editing, setEditing]       = useState<Borrower | null | 'new'>(null)
  const [deleting, setDeleting]     = useState<Borrower | null>(null)

  const load = useCallback(async (term?: string) => {
    setLoading(true)
    setError('')
    try {
      const data = await borrowersApi.getAll(term)
      setBorrowers(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load borrowers.')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { load() }, [load])

  // Debounce search
  useEffect(() => {
    const id = setTimeout(() => load(search || undefined), 300)
    return () => clearTimeout(id)
  }, [search, load])

  function handleSaved() {
    setEditing(null)
    load(search || undefined)
  }

  function handleDeleted() {
    setDeleting(null)
    load(search || undefined)
  }

  return (
    <div>
      <h1>Borrowers</h1>

      <div className="borrowers-toolbar">
        <input
          className="search-input"
          type="text"
          placeholder="Search by name or loan number…"
          value={search}
          onChange={e => setSearch(e.target.value)}
        />
        <button className="btn btn-primary" onClick={() => setEditing('new')}>
          + Add Borrower
        </button>
      </div>

      {error && <div className="error-msg">{error}</div>}

      {loading ? (
        <div className="empty-state">Loading…</div>
      ) : borrowers.length === 0 ? (
        <div className="empty-state">
          {search ? 'No borrowers match your search.' : 'No borrowers yet — add one to get started.'}
        </div>
      ) : (
        <div className="borrowers-table-wrap">
          <table className="borrowers-table">
            <thead>
              <tr>
                <th>Last Name</th>
                <th>First Name</th>
                <th>Loan Number</th>
                <th>Folder Name</th>
                <th>Primary</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {borrowers.map(b => (
                <tr key={b.id}>
                  <td>{b.lastName}</td>
                  <td>{b.firstName}</td>
                  <td>{b.loanNumber}</td>
                  <td><span className="folder-name">{b.folderName}</span></td>
                  <td>{b.isPrimaryBorrower ? '✓' : ''}</td>
                  <td className="actions-cell">
                    <button
                      className="icon-btn"
                      title="Edit"
                      onClick={() => setEditing(b)}
                    >✏️</button>
                    <button
                      className="icon-btn delete"
                      title="Delete"
                      onClick={() => setDeleting(b)}
                    >🗑️</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {editing !== null && (
        <BorrowerFormModal
          borrower={editing === 'new' ? undefined : editing}
          onSave={handleSaved}
          onClose={() => setEditing(null)}
        />
      )}

      {deleting !== null && (
        <DeleteModal
          borrower={deleting}
          onConfirm={handleDeleted}
          onClose={() => setDeleting(null)}
        />
      )}
    </div>
  )
}
