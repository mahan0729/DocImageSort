import './HelpPage.css'

const TOC = [
  { id: 'quick-start',    label: 'Quick Start' },
  { id: 'drop-folder',    label: 'Drop Folder' },
  { id: 'merge',          label: 'Merge Documents' },
  { id: 'documents',      label: 'Documents' },
  { id: 'borrowers',      label: 'Borrowers' },
  { id: 'processing-log', label: 'Processing Log' },
  { id: 'settings',       label: 'Settings' },
  { id: 'file-naming',    label: 'File Naming' },
  { id: 'doc-types',      label: 'Document Types' },
  { id: 'troubleshooting',label: 'Troubleshooting' },
]

const DOC_TYPES = [
  'Pay Stub', 'Bank Statement', 'W2', 'Tax Return (1040)', '1003 Loan Application',
  '1099-INT', '1099-DIV', '1099-B', '1099-MISC', '1099-NEC', '1099 Composite',
  'Driver License', 'Social Security Card', 'Gift Letter',
  'Insurance Declaration', 'Title Report', 'Appraisal', 'Purchase Contract',
  'Credit Report', 'VOE (Verification of Employment)', 'VOD (Verification of Deposits)',
  'Flood Certification', 'HOA Documents',
  'Mortgage Statement', 'Lease Agreement', 'Award Letter',
  'Retirement Statement', 'Investment Account Statement', 'P&L Statement',
  'Schedule C', 'Schedule E', 'K-1', 'SSA-89', 'LOE (Letter of Explanation)',
  'Divorce Decree', 'Bankruptcy (Chapter 7)', 'Bankruptcy (Chapter 13)',
  'Child Support Order', 'Alimony Agreement', 'Business License',
  'Power of Attorney', 'Death Certificate', 'Quitclaim Deed', 'Trust Document',
  'Earnest Money Check',
  'Unknown (or document title if printed)',
]

const STATUSES = [
  { status: 'Pending',    color: '#fef9c3', meaning: 'File received; pipeline has not run yet.' },
  { status: 'Classified', color: '#dbeafe', meaning: 'AI identified the document type and date.' },
  { status: 'Converted',  color: '#e0e7ff', meaning: 'Image was converted to PDF.' },
  { status: 'Filed',      color: '#e8f5ee', meaning: 'Renamed and routed to the borrower folder.' },
  { status: 'Duplicate',  color: '#f3f4f6', meaning: 'File matches a document already in the system. Pipeline skipped.' },
  { status: 'Error',      color: '#fee2e2', meaning: 'Something went wrong. Check the Processing Log for details.' },
]

export function HelpPage() {
  return (
    <div>
      <h1>Help</h1>
      <p style={{ color: 'var(--text-muted)' }}>
        Everything you need to know about using DocImageSort.
      </p>

      <div className="help-layout">

        {/* ── Table of contents ── */}
        <aside className="help-toc">
          <h3>On this page</h3>
          <ul>
            {TOC.map(t => (
              <li key={t.id}>
                <a href={`#${t.id}`}>{t.label}</a>
              </li>
            ))}
          </ul>
        </aside>

        <div className="help-content">

          {/* ── Quick Start ── */}
          <section id="quick-start" className="help-section">
            <h2>🚀 Quick Start</h2>
            <div className="quick-steps">
              <div className="quick-step">
                <div className="step-number">1</div>
                <div className="step-body">
                  <strong>Add a borrower</strong>
                  <span>Go to <b>Borrowers</b> and click <b>+ Add Borrower</b>. Enter their last name, first name, and loan number.</span>
                </div>
              </div>
              <div className="quick-step">
                <div className="step-number">2</div>
                <div className="step-body">
                  <strong>Drop a document</strong>
                  <span>Copy or save a PDF, JPG, or PNG into your drop folder (<code>C:\DocImageSort\Drop</code>). The pipeline starts automatically within seconds.</span>
                </div>
              </div>
              <div className="quick-step">
                <div className="step-number">3</div>
                <div className="step-body">
                  <strong>Review and assign</strong>
                  <span>Go to <b>Documents</b>, confirm the AI-identified type, and click 📁 to assign the document to a borrower. It moves out of PENDING and into their folder.</span>
                </div>
              </div>
            </div>
          </section>

          {/* ── Drop Folder ── */}
          <section id="drop-folder" className="help-section">
            <h2>📂 Drop Folder</h2>
            <p>
              DocImageSort watches a folder on your computer for new files. Any PDF, JPG, JPEG, or PNG
              you save or copy there is picked up automatically — no button to click.
            </p>
            <h3>Default path</h3>
            <p><code>C:\DocImageSort\Drop</code></p>
            <p>You can change this path in <b>Settings</b>. A restart of the API is required for path changes to take effect.</p>
            <h3>What happens after you drop a file</h3>
            <ol>
              <li>DocImageSort detects the file and creates a database record.</li>
              <li>It checks whether the file is a duplicate (by SHA-256 hash). Duplicates are flagged and skipped.</li>
              <li>Claude AI reads the document and identifies its type and date.</li>
              <li>If the file is a JPG or PNG, it is converted to PDF.</li>
              <li>The file is renamed with the <code>PENDING_</code> prefix and moved to the PENDING folder.</li>
              <li>The document appears in the <b>Documents</b> screen, ready for your review.</li>
            </ol>
            <div className="help-tip">
              <strong>Tip:</strong> The drop folder is monitored in real time — you do not need to refresh the app.
              Reload the Documents page to see newly processed files.
            </div>
          </section>

          {/* ── Merge Documents ── */}
          <section id="merge" className="help-section">
            <h2>🔗 Merge Documents</h2>
            <p>
              When a document arrives as multiple separate photos or scans (each page saved as its own file),
              use <b>Merge Documents</b> to combine them into a single PDF before processing.
            </p>
            <h3>How to merge</h3>
            <ol>
              <li>Click <b>Merge</b> in the top navigation.</li>
              <li>Drop or browse for the individual page files (PDF, JPG, or PNG). Up to 20 files at once.</li>
              <li>AI automatically detects the page number printed on each page and sorts them in order.</li>
              <li>Use the ▲ ▼ arrows to adjust the order manually if needed.</li>
              <li>Click <b>Merge N Pages</b>. The pages are combined into one PDF and run through the standard pipeline — classified, renamed, and placed in PENDING.</li>
            </ol>
            <div className="help-tip">
              <strong>Tip:</strong> Do not navigate away while a merge is running — navigation is locked until the merge completes.
            </div>
            <h3>Page order</h3>
            <p>
              AI looks for patterns like <code>Page 2 of 4</code>, <code>2 of 4</code>, or a lone number in
              the header or footer. When page numbers are not visible on a page, that page falls back to
              its upload position.
            </p>
          </section>

          {/* ── Documents ── */}
          <section id="documents" className="help-section">
            <h2>📄 Documents</h2>
            <p>
              The Documents screen is where you review what the AI classified and assign documents to borrowers.
            </p>
            <h3>Status filter tabs</h3>
            <p>Use the tabs at the top to filter documents. The <b>Unassigned</b> tab shows all documents that have not yet been filed to a borrower — this is your main working view after each batch. The page refreshes automatically every 10 seconds so new files appear without reloading.</p>

            <table className="help-table">
              <thead><tr><th>Status</th><th>Meaning</th></tr></thead>
              <tbody>
                {STATUSES.map(s => (
                  <tr key={s.status}>
                    <td><span style={{ background: s.color, padding: '2px 8px', borderRadius: 10, fontSize: 12, fontWeight: 700 }}>{s.status.toUpperCase()}</span></td>
                    <td>{s.meaning}</td>
                  </tr>
                ))}
              </tbody>
            </table>

            <h3>Actions</h3>
            <ul>
              <li><b>✏️ Correct type</b> — opens a dropdown to fix the AI's document type if it was wrong.</li>
              <li><b>📁 Assign borrower</b> — search for a borrower and click <b>Assign &amp; File</b>. Renames the file to <code>[LastName]_[FirstName]_[DocType]_[MMDDYYYY].pdf</code> and moves it to the borrower's folder.</li>
            </ul>
            <h3>Bulk assign</h3>
            <p>
              Unassigned documents are automatically checked when the page loads. Use the checkboxes to select
              multiple documents, then click <b>Assign to Borrower</b> in the green bar to file them all at once.
              The select-all checkbox in the header checks or clears all rows.
            </p>
            <div className="help-tip">
              <strong>Tip:</strong> Correct the document type <em>before</em> assigning a borrower — the final file name includes the document type.
            </div>
          </section>

          {/* ── Borrowers ── */}
          <section id="borrowers" className="help-section">
            <h2>👤 Borrowers</h2>
            <p>Borrowers are the people whose documents you are organizing. Each borrower gets their own subfolder under your Files Folder.</p>
            <h3>Folder naming</h3>
            <p>Folders are named <code>LastName,FirstName</code> — comma-separated, no space. Example: <code>Smith,John</code></p>
            <h3>Adding a borrower</h3>
            <ol>
              <li>Click <b>+ Add Borrower</b>.</li>
              <li>Enter last name, first name, and loan number.</li>
              <li>Check <b>Primary borrower</b> if this is the primary on the loan (folder names always use the primary borrower).</li>
              <li>Click <b>Save</b>.</li>
            </ol>
            <h3>Editing or deleting</h3>
            <p>Use the ✏️ and 🗑️ buttons on each row. Deleting a borrower does not delete their files on disk.</p>
          </section>

          {/* ── Processing Log ── */}
          <section id="processing-log" className="help-section">
            <h2>📋 Processing Log</h2>
            <p>
              Every action the pipeline takes is recorded here — ingests, classifications, conversions,
              renames, routes, duplicates, and errors. Entries are newest first.
            </p>
            <h3>Log levels</h3>
            <ul>
              <li><b>Info</b> — normal pipeline activity (green).</li>
              <li><b>Warning</b> — non-fatal issue, such as a duplicate or unsupported file type (amber).</li>
              <li><b>Error</b> — a pipeline step failed (red). The Message column explains what went wrong.</li>
            </ul>
            <h3>Pipeline actions you will see</h3>
            <table className="help-table">
              <thead><tr><th>Action</th><th>What it means</th></tr></thead>
              <tbody>
                {[
                  ['Ingest',          'File detected in drop folder; database record created.'],
                  ['DuplicateCheck',  'SHA-256 hash compared against existing documents.'],
                  ['Classify',        'Claude AI identified document type and date.'],
                  ['Convert',         'JPG/PNG converted to PDF.'],
                  ['Rename',          'File renamed to standardized name.'],
                  ['Route',           'File moved to borrower or PENDING folder.'],
                  ['CorrectType',     'User corrected the AI document type.'],
                  ['AssignBorrower',  'User assigned borrower; file renamed and moved to borrower folder.'],
                ].map(([action, meaning]) => (
                  <tr key={action}><td><code>{action}</code></td><td>{meaning}</td></tr>
                ))}
              </tbody>
            </table>
          </section>

          {/* ── Settings ── */}
          <section id="settings" className="help-section">
            <h2>⚙️ Settings</h2>

            <h3>Use Azure Storage</h3>
            <p>
              When <b>off</b> (default), all documents stay on this machine. This is the recommended setting
              for Phase 1 — it keeps you RESPA and GLBA compliant without any cloud configuration.
            </p>
            <p>
              When <b>on</b>, documents will be stored in Azure Blob Storage (Phase 2 feature — requires additional setup).
            </p>

            <h3>Drop Folder / Files Folder</h3>
            <p>
              Change where DocImageSort watches for new files and where it routes processed documents.
              <br />
              <strong>Important:</strong> path changes require restarting the API to take effect.
            </p>

            <h3>Anthropic API Key</h3>
            <p>
              Your Claude AI key (<code>sk-ant-…</code>) is required for document classification.
              Without it, all documents will be classified as <b>Unknown</b>.
              Get a key at <b>console.anthropic.com</b>.
            </p>
          </section>

          {/* ── File Naming ── */}
          <section id="file-naming" className="help-section">
            <h2>📝 File Naming Convention</h2>

            <p>Format: <code>[LastName]_[FirstName]_[DocumentType]_[MMDDYYYY].pdf</code></p>
            <p>Only letters (A–Z, a–z), numbers (0–9), and underscores are used. Spaces become underscores; all other characters are removed.</p>

            <table className="help-table">
              <thead><tr><th>Situation</th><th>File Name</th><th>Notes</th></tr></thead>
              <tbody>
                <tr>
                  <td>Borrower assigned</td>
                  <td><code>Smith_John_Pay_Stub_09282026.pdf</code></td>
                  <td>Filed to <code>Files\Smith,John\</code></td>
                </tr>
                <tr>
                  <td>Not yet assigned</td>
                  <td><code>PENDING_Pay_Stub_09282026.pdf</code></td>
                  <td>Filed to <code>Files\PENDING\</code></td>
                </tr>
                <tr>
                  <td>W2 document</td>
                  <td><code>Smith_John_W2_2024_09282026.pdf</code></td>
                  <td>Tax year (4-digit) embedded in type</td>
                </tr>
                <tr>
                  <td>1099 document</td>
                  <td><code>Smith_John_1099_Composite_2024_09282026.pdf</code></td>
                  <td>Tax year (4-digit) embedded in type</td>
                </tr>
                <tr>
                  <td>Bank Statement (period)</td>
                  <td><code>Smith_John_Bank_Statement_Chase_Checking_01012026to01312026.pdf</code></td>
                  <td>Start-to-end date range when AI finds both dates</td>
                </tr>
                <tr>
                  <td>Duplicate detected</td>
                  <td><em>Original name, not moved</em></td>
                  <td>Stays in drop folder</td>
                </tr>
                <tr>
                  <td>Name conflict</td>
                  <td><code>Smith_John_Pay_Stub_09282026_1.pdf</code></td>
                  <td>Counter suffix added automatically</td>
                </tr>
              </tbody>
            </table>

            <h3>Date in file name</h3>
            <p>The date (<code>MMDDYYYY</code>) is the document's own date as identified by AI — not today's date. For bank statements and other period documents, the date shows as a range (<code>MMDDYYYYtoMMDDYYYY</code>). For W2s and 1099s, the tax year is also embedded in the document type segment.</p>
          </section>

          {/* ── Document Types ── */}
          <section id="doc-types" className="help-section">
            <h2>🗂️ Document Types</h2>
            <p>Claude AI classifies documents into one of these types. You can correct any misclassification from the Documents screen.</p>
            <ul style={{ columns: 2, columnGap: 24 }}>
              {DOC_TYPES.map(t => <li key={t}>{t}</li>)}
            </ul>
          </section>

          {/* ── Troubleshooting ── */}
          <section id="troubleshooting" className="help-section">
            <h2>🔧 Troubleshooting</h2>

            <h3>File dropped but nothing happened</h3>
            <ul>
              <li>Make sure the API is running (<code>dotnet run</code> in the <code>DocImageSort.Api</code> folder).</li>
              <li>Check that the file is a PDF, JPG, JPEG, or PNG — other types are ignored.</li>
              <li>Check the <b>Processing Log</b> for an Error entry about the file.</li>
            </ul>

            <h3>All documents show type "Unknown"</h3>
            <ul>
              <li>Your Anthropic API key is missing or invalid. Go to <b>Settings</b> and enter your key.</li>
              <li>Verify your key is active at <b>console.anthropic.com</b>.</li>
            </ul>

            <h3>Document shows as Duplicate</h3>
            <ul>
              <li>The exact same file (byte-for-byte) was already processed. Check the Documents screen for the original.</li>
              <li>If you want to re-process it, delete the original document record and drop the file again.</li>
            </ul>

            <h3>Borrower folder not created</h3>
            <ul>
              <li>DocImageSort creates folders automatically when a document is assigned. Make sure the Files Folder path in Settings exists and is writable.</li>
            </ul>

            <h3>Settings saved but path change not working</h3>
            <ul>
              <li>Restart the API after changing folder paths — the drop folder watcher reads the path only on startup.</li>
            </ul>
          </section>

        </div>
      </div>
    </div>
  )
}
