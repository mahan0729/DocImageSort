# DocImageSort

AI-powered document organizer for mortgage loan officers. Drop a scanned document or photo into a watch folder — DocImageSort classifies it with Claude AI, converts it to PDF, renames it with a standardized file name, and routes it into the correct borrower folder automatically.

---

## What It Does

1. **Watch** — monitors a drop folder for new PDFs, JPGs, and PNGs
2. **Classify** — sends the file to Claude AI to identify the document type (Pay Stub, W-2, Bank Statement, etc.) and extract the document date
3. **Convert** — converts images (JPG/PNG) to PDF
4. **Rename** — applies a standardized file name: `LastName,FirstName_DocType_YYYY-MM.pdf`
5. **Route** — moves the file into `[FilesFolder]\LastName,FirstName\`

Unassigned documents land in `[FilesFolder]\PENDING\` until you assign a borrower from the Document Review screen.

---

## Prerequisites

| Requirement | Version | Notes |
|---|---|---|
| .NET SDK | 10.0+ | [dotnet.microsoft.com](https://dotnet.microsoft.com/download) |
| Node.js | 18+ | [nodejs.org](https://nodejs.org) |
| Anthropic API Key | — | Required for AI classification |

---

## Setup

### 1. Clone the repository

```
git clone https://mahan0729@dev.azure.com/mahan0729/DocImageSort/_git/DocImageSort
cd DocImageSort
```

### 2. Create the watch folders

```
mkdir C:\DocImageSort\Drop
mkdir C:\DocImageSort\Files
```

You can use different paths — just update them in Settings after the app starts.

### 3. Configure the API

Open `DocImageSort.Api\appsettings.json` and set your Anthropic API key:

```json
{
  "Anthropic": {
    "ApiKey": ""
  }
}
```

You can also set the API key from the **Settings** page inside the app after startup.

### 4. Start the API

```
cd DocImageSort.Api
dotnet run
```

The API starts on `http://localhost:5070`. The SQLite database (`docimagesort.db`) is created automatically on first run.

### 5. Start the web app

Open a second terminal:

```
cd DocImageSort.Web
npm install
npm run dev
```

The app opens at `http://localhost:5173`.

---

## Using DocImageSort

### Drop a document

Copy or save any PDF, JPG, or PNG into your drop folder (`C:\DocImageSort\Drop` by default). The pipeline runs automatically within a few seconds.

### Review documents

Click **Documents** in the top navigation.

- **✏️** — Correct the AI's document type if it misidentified it
- **📁** — Assign the document to a borrower (triggers final rename and route out of PENDING)
- Status filter tabs let you quickly find Pending, Filed, Duplicate, or Error documents

### Manage borrowers

Click **Borrowers** to add, edit, or search borrower profiles. Each borrower needs a last name, first name, and loan number. Their folder name (`LastName,FirstName`) is generated automatically.

### Processing Log

Click **Processing Log** to see the full audit trail — every ingest, classification, conversion, rename, route, and duplicate detection event with timestamps and outcomes.

### Settings

Click **Settings** to:
- Toggle **Use Azure Storage** (off = local-only; on = Phase 2 cloud mode)
- Change the drop folder or files folder paths *(requires API restart to take effect)*
- Update your Anthropic API key

---

## File Naming Convention

| Situation | File Name | Location |
|---|---|---|
| Borrower assigned | `Smith,John_Pay Stub_2026-08.pdf` | `C:\DocImageSort\Files\Smith,John\` |
| Borrower not yet assigned | `PENDING_Pay Stub_2026-08.pdf` | `C:\DocImageSort\Files\PENDING\` |
| Duplicate detected | *(original name, not moved)* | Stays in drop folder |

---

## Supported Document Types

Pay Stub · Bank Statement · W2 · Tax Return (1040) · 1003 Loan Application · 1099-INT · 1099-DIV · 1099-B · 1099-MISC · 1099-NEC · 1099 Composite · Driver License · Social Security Card · Gift Letter · Insurance Declaration · Title Report · Appraisal · Purchase Contract · Credit Report · VOE · VOD · Flood Certification · HOA Documents · Mortgage Statement · Lease Agreement · Award Letter · Retirement Statement · Investment Account Statement · P&L Statement · Schedule C · Schedule E · K-1 · SSA-89 · LOE · Divorce Decree · Bankruptcy (Chapter 7) · Bankruptcy (Chapter 13) · Child Support Order · Alimony Agreement · Business License · Power of Attorney · Death Certificate · Quitclaim Deed · Trust Document · Unknown (or document's printed title)

---

## Tech Stack

| Layer | Technology |
|---|---|
| API | .NET 10 Web API |
| AI Classification | Anthropic Claude (claude-sonnet-4-6) via Anthropic.SDK |
| PDF Conversion | PDFsharp 6.2 |
| Database | SQLite + EF Core |
| Frontend | React 19 + TypeScript (Vite) |
| Project Management | Azure DevOps |

---

## Project Structure

```
DocImageSort/
├── DocImageSort.Api/
│   ├── Controllers/          # BorrowersController, DocumentsController, ProcessingLogsController, SettingsController
│   ├── Data/                 # AppDbContext (SQLite + EF Core)
│   ├── Models/               # Borrower, Document, LoanFile, ProcessingLog, BaseEntity
│   ├── Services/             # Pipeline, Classification, Conversion, Rename, Route, Duplicate Detection, Review
│   └── appsettings.json      # Drop folder, files folder, API key, feature flags
└── DocImageSort.Web/
    └── src/
        ├── api/              # borrowers.ts, documents.ts, processingLogs.ts, settings.ts
        └── pages/            # BorrowersPage, DocumentsPage, ProcessingLogPage, SettingsPage
```

---

DocImageSort &copy; 2026, all rights reserved
