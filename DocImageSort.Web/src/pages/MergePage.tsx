import { useState, useCallback, useRef } from 'react'
import { mergeApi } from '../api/merge'
import type { Document } from '../api/documents'
import './MergePage.css'

const ACCEPTED = '.pdf,.jpg,.jpeg,.png'
const MAX_FILES = 20

function fileIcon(name: string) {
  return name.toLowerCase().endsWith('.pdf') ? '📄' : '🖼️'
}

export function MergePage() {
  const [files, setFiles]       = useState<File[]>([])
  const [dragging, setDragging] = useState(false)
  const [loading, setLoading]   = useState(false)
  const [result, setResult]     = useState<Document | null>(null)
  const [error, setError]       = useState<string | null>(null)
  const inputRef                = useRef<HTMLInputElement>(null)

  const addFiles = useCallback((incoming: FileList | File[]) => {
    const allowed = /\.(pdf|jpg|jpeg|png)$/i
    const valid   = Array.from(incoming).filter(f => allowed.test(f.name))
    if (valid.length < incoming.length) {
      setError('Some files were skipped — only PDF, JPG, and PNG are accepted.')
    }
    setFiles(prev => {
      const combined = [...prev, ...valid]
      return combined.slice(0, MAX_FILES)
    })
  }, [])

  const removeFile = (idx: number) =>
    setFiles(prev => prev.filter((_, i) => i !== idx))

  const moveUp = (idx: number) => {
    if (idx === 0) return
    setFiles(prev => {
      const next = [...prev]
      ;[next[idx - 1], next[idx]] = [next[idx], next[idx - 1]]
      return next
    })
  }

  const moveDown = (idx: number) => {
    setFiles(prev => {
      if (idx >= prev.length - 1) return prev
      const next = [...prev]
      ;[next[idx], next[idx + 1]] = [next[idx + 1], next[idx]]
      return next
    })
  }

  const onDrop = useCallback((e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault()
    setDragging(false)
    addFiles(e.dataTransfer.files)
  }, [addFiles])

  const onDragOver = (e: React.DragEvent) => { e.preventDefault(); setDragging(true) }
  const onDragLeave = () => setDragging(false)

  const handleMerge = async () => {
    if (files.length < 2) return
    setLoading(true)
    setError(null)
    setResult(null)
    try {
      const doc = await mergeApi.merge(files)
      setResult(doc)
      setFiles([])
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Merge failed.')
    } finally {
      setLoading(false)
    }
  }

  const reset = () => { setResult(null); setError(null); setFiles([]) }

  if (result) {
    return (
      <div className="merge-result">
        <div className="merge-result-icon">✅</div>
        <h2>Merge Complete</h2>
        <p className="merge-result-type">{result.documentType}</p>
        <p className="merge-result-file">{result.renamedFileName}</p>
        <p className="merge-result-hint">
          The merged document has been classified and placed in <strong>PENDING</strong>.
          Open the Documents page to assign it to a borrower.
        </p>
        <div className="merge-result-actions">
          <button className="btn btn-primary" onClick={reset}>Merge Another</button>
        </div>
      </div>
    )
  }

  return (
    <div className="merge-page">
      <h1>Merge Documents</h1>
      <p className="merge-subtitle">
        Upload multiple pages of the same document. AI detects page numbers and assembles
        them in order, then classifies and routes the merged PDF automatically.
      </p>

      {/* Drop zone */}
      <div
        className={`merge-dropzone${dragging ? ' dragging' : ''}`}
        onDrop={onDrop}
        onDragOver={onDragOver}
        onDragLeave={onDragLeave}
        onClick={() => inputRef.current?.click()}
        role="button"
        tabIndex={0}
        onKeyDown={e => e.key === 'Enter' && inputRef.current?.click()}
        aria-label="Drop files here or click to browse"
      >
        <input
          ref={inputRef}
          type="file"
          accept={ACCEPTED}
          multiple
          hidden
          onChange={e => { if (e.target.files) addFiles(e.target.files); e.target.value = '' }}
        />
        <div className="merge-dropzone-content">
          <span className="merge-dropzone-icon">📂</span>
          <span className="merge-dropzone-label">
            {dragging ? 'Drop to add' : 'Drop pages here or click to browse'}
          </span>
          <span className="merge-dropzone-hint">PDF, JPG, PNG · up to {MAX_FILES} files</span>
        </div>
      </div>

      {/* File list */}
      {files.length > 0 && (
        <div className="merge-file-list">
          <div className="merge-file-list-header">
            <span>{files.length} page{files.length !== 1 ? 's' : ''} selected</span>
            <span className="merge-file-list-hint">
              AI will sort by detected page number — reorder manually if needed
            </span>
          </div>
          {files.map((file, idx) => (
            <div key={`${file.name}-${idx}`} className="merge-file-row">
              <span className="merge-file-num">{idx + 1}</span>
              <span className="merge-file-icon">{fileIcon(file.name)}</span>
              <span className="merge-file-name" title={file.name}>{file.name}</span>
              <span className="merge-file-size">
                {(file.size / 1024).toFixed(0)} KB
              </span>
              <div className="merge-file-controls">
                <button
                  className="merge-ctrl-btn"
                  onClick={() => moveUp(idx)}
                  disabled={idx === 0}
                  title="Move up"
                  aria-label="Move up"
                >▲</button>
                <button
                  className="merge-ctrl-btn"
                  onClick={() => moveDown(idx)}
                  disabled={idx === files.length - 1}
                  title="Move down"
                  aria-label="Move down"
                >▼</button>
                <button
                  className="merge-ctrl-btn merge-ctrl-remove"
                  onClick={() => removeFile(idx)}
                  title="Remove"
                  aria-label="Remove file"
                >✕</button>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Error */}
      {error && <div className="merge-error">{error}</div>}

      {/* Actions */}
      <div className="merge-actions">
        {files.length > 0 && (
          <button className="btn btn-outline" onClick={reset} disabled={loading}>
            Clear All
          </button>
        )}
        <button
          className="btn btn-primary"
          onClick={handleMerge}
          disabled={files.length < 2 || loading}
        >
          {loading
            ? 'Merging…'
            : files.length < 2
              ? 'Add at least 2 pages'
              : `Merge ${files.length} Pages`}
        </button>
      </div>

      {loading && (
        <div className="merge-loading">
          <div className="merge-spinner" />
          <p>
            Detecting page order and merging — this may take a moment
            while AI reads each page…
          </p>
        </div>
      )}
    </div>
  )
}
