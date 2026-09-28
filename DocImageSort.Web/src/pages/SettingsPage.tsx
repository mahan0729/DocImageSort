import { useEffect, useState } from 'react'
import { settingsApi, type AppSettings } from '../api/settings'
import './SettingsPage.css'

export function SettingsPage() {
  const [, setSettings]   = useState<AppSettings | null>(null)
  const [loading, setLoading]     = useState(true)
  const [saving, setSaving]       = useState(false)
  const [saveStatus, setSaveStatus] = useState<'idle' | 'saved' | 'error'>('idle')
  const [saveMsg, setSaveMsg]     = useState('')
  const [showKey, setShowKey]     = useState(false)
  const [pathsChanged, setPathsChanged] = useState(false)

  const [useAzure, setUseAzure]           = useState(false)
  const [dropPath, setDropPath]           = useState('')
  const [filesPath, setFilesPath]         = useState('')
  const [apiKey, setApiKey]               = useState('')

  useEffect(() => {
    settingsApi.get()
      .then(s => {
        setSettings(s)
        setUseAzure(s.useAzure)
        setDropPath(s.dropFolderPath)
        setFilesPath(s.filesFolderPath)
        setApiKey(s.anthropicApiKey)
      })
      .catch(err => {
        setSaveStatus('error')
        setSaveMsg(err instanceof Error ? err.message : 'Failed to load settings.')
      })
      .finally(() => setLoading(false))
  }, [])

  function handlePathChange(setter: (v: string) => void, value: string) {
    setter(value)
    setPathsChanged(true)
  }

  async function handleSave(e: React.FormEvent) {
    e.preventDefault()
    setSaving(true)
    setSaveStatus('idle')
    setSaveMsg('')

    try {
      await settingsApi.update({
        useAzure,
        dropFolderPath:  dropPath,
        filesFolderPath: filesPath,
        anthropicApiKey: apiKey,
      })
      setSaveStatus('saved')
      setSaveMsg('Settings saved.')
      setPathsChanged(false)
      setTimeout(() => setSaveStatus('idle'), 3000)
    } catch (err) {
      setSaveStatus('error')
      setSaveMsg(err instanceof Error ? err.message : 'Save failed.')
    } finally {
      setSaving(false)
    }
  }

  if (loading) return <div style={{ padding: 32, color: 'var(--text-muted)' }}>Loading settings…</div>

  return (
    <div>
      <h1>Settings</h1>
      <p style={{ color: 'var(--text-muted)' }}>
        Configure how DocImageSort processes and stores your documents.
      </p>

      <form onSubmit={handleSave}>
        <div className="settings-grid">

          {/* ── Feature flags ── */}
          <div className="settings-card">
            <h2>Feature Flags</h2>
            <p className="card-desc">Toggle optional features. Changes take effect immediately after saving.</p>

            <div className="toggle-row">
              <div className="toggle-label">
                <strong>Use Azure Storage</strong>
                <span>
                  {useAzure
                    ? 'Documents will be stored in Azure Blob Storage (Phase 2).'
                    : 'Documents are stored on this machine only (local mode — RESPA/GLBA compliant).'}
                </span>
              </div>
              <label className="toggle-switch">
                <input
                  type="checkbox"
                  checked={useAzure}
                  onChange={e => setUseAzure(e.target.checked)}
                />
                <span className="toggle-track" />
              </label>
            </div>
          </div>

          {/* ── Folder paths ── */}
          <div className="settings-card">
            <h2>Folder Paths</h2>
            <p className="card-desc">Where DocImageSort watches for new documents and files processed ones.</p>

            <div className="settings-field">
              <label htmlFor="dropPath">Drop Folder</label>
              <input
                id="dropPath"
                type="text"
                value={dropPath}
                onChange={e => handlePathChange(setDropPath, e.target.value)}
              />
              <span className="field-hint">DocImageSort watches this folder for new files to process.</span>
            </div>

            <div className="settings-field">
              <label htmlFor="filesPath">Files Folder</label>
              <input
                id="filesPath"
                type="text"
                value={filesPath}
                onChange={e => handlePathChange(setFilesPath, e.target.value)}
              />
              <span className="field-hint">Processed documents are routed here into borrower subfolders.</span>
            </div>

            {pathsChanged && (
              <div className="restart-notice">
                ⚠️ Folder path changes require restarting the DocImageSort API to take effect.
              </div>
            )}
          </div>

          {/* ── API keys ── */}
          <div className="settings-card">
            <h2>API Keys</h2>
            <p className="card-desc">Required for AI document classification.</p>

            <div className="settings-field">
              <label htmlFor="apiKey">Anthropic API Key</label>
              <div className="api-key-row">
                <input
                  id="apiKey"
                  type={showKey ? 'text' : 'password'}
                  value={apiKey}
                  onChange={e => setApiKey(e.target.value)}
                  placeholder="sk-ant-…"
                />
                <button
                  type="button"
                  className="btn btn-outline"
                  onClick={() => setShowKey(v => !v)}
                  style={{ flexShrink: 0 }}
                >
                  {showKey ? 'Hide' : 'Show'}
                </button>
              </div>
              <span className="field-hint">Used by the Claude AI to classify documents.</span>
            </div>
          </div>

        </div>

        <div className="settings-save-bar" style={{ marginTop: 24 }}>
          <button type="submit" className="btn btn-primary" disabled={saving}>
            {saving ? 'Saving…' : 'Save Settings'}
          </button>
          {saveStatus !== 'idle' && (
            <span className={`save-status${saveStatus === 'error' ? ' error' : ''}`}>
              {saveMsg}
            </span>
          )}
        </div>
      </form>
    </div>
  )
}
