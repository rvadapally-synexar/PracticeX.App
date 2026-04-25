# Source Discovery — Module Notes

Source Discovery is the entry point of the ingestion pipeline. It contains the
connector abstractions, the folder/upload connector, the Outlook Microsoft Graph
connector, the rule-based document classifier, and the ingestion orchestrator
that persists discovered items to `doc.source_objects`, `doc.ingestion_batches`,
`doc.ingestion_jobs`, `doc.document_assets`, `doc.document_candidates`,
`workflow.review_tasks`, and `audit.audit_events`.

## Architecture summary

Connectors implement `ISourceConnector` (in `PracticeX.Application`) and emit
`DiscoveredItem` records. They never mutate canonical contract records — that
is the responsibility of the review queue.

```
ISourceConnector ──▶ DiscoveryResult ──▶ IIngestionOrchestrator
                                          ├─▶ doc.source_objects
                                          ├─▶ doc.document_assets
                                          ├─▶ doc.document_candidates
                                          ├─▶ doc.ingestion_jobs
                                          ├─▶ doc.ingestion_batches
                                          ├─▶ workflow.review_tasks
                                          └─▶ audit.audit_events
```

## Configuring Microsoft Graph OAuth

1. **Register an app** in Microsoft Entra (Azure AD) with the redirect URI
   matching `MicrosoftGraph:RedirectUri` (default
   `https://localhost:7100/api/sources/outlook/oauth/callback`).
2. **Add API permissions** (delegated): `offline_access`, `Mail.Read`,
   `Mail.ReadBasic`. Read-only.
3. **Create a client secret** and copy its value.
4. Set environment variables (do not commit secrets):

   ```bash
   export MicrosoftGraph__ClientId="<app-registration-client-id>"
   export MicrosoftGraph__ClientSecret="<app-registration-client-secret>"
   export MicrosoftGraph__TenantId="common"   # or your AAD tenant id
   export MICROSOFT_GRAPH_REDIRECT_URI="https://localhost:7100/api/sources/outlook/oauth/callback"
   ```

5. Run the API. From the Source Discovery UI, click **Connect Outlook**. The
   browser is redirected to Microsoft, the user consents to read-only mailbox
   access, and the callback exchanges the code for tokens. Tokens are stored
   via `IMicrosoftGraphTokenStore` (in-memory in dev — replace with Key Vault
   for production).

If `MicrosoftGraph:ClientId` is unset, the connector reports
`configuration_required` and the start endpoint returns a 400.

## API surface

| Method | Path | Purpose |
|--|--|--|
| `GET` | `/api/sources/connectors` | Describe registered connectors |
| `GET` | `/api/sources/connections` | List tenant source connections |
| `POST` | `/api/sources/connections` | Create a draft connection |
| `DELETE` | `/api/sources/connections/{id}` | Disable a connection |
| `POST` | `/api/sources/connections/{id}/folder/scan` | Upload files (multipart) and scan |
| `GET` | `/api/sources/connections/{id}/outlook/oauth/start` | OAuth authorize URL |
| `GET` | `/api/sources/outlook/oauth/callback` | OAuth code exchange |
| `POST` | `/api/sources/connections/{id}/outlook/scan` | Pull contract-likely messages and attachments |
| `GET` | `/api/sources/batches` | Recent ingestion batches |
| `GET` | `/api/sources/batches/{id}` | Single batch with status counts |
| `GET` | `/api/sources/candidates` | Document candidates with confidence and reason codes |
| `POST` | `/api/sources/candidates/{id}/queue-review` | Push to review queue |
| `POST` | `/api/sources/candidates/{id}/retry` | Reset to candidate |

## Folder upload contract

Multipart form with one or more `file` parts. To preserve relative paths from
folder/zip uploads, the browser sends `paths[i]` form fields in matching order
(falls back to `webkitRelativePath` on the file). The backend records the
relative path on `doc.source_objects.relative_path` and uses the parent folder
as a classification hint.

## Reason codes

The rule-based classifier emits explainable codes such as:

- `unsupported_mime_type`
- `duplicate_content`
- `empty_file`
- `likely_contract`
- `ambiguous_type`
- `filename_contract_keywords`
- `filename_amendment`
- `filename_rate_schedule`
- `folder_hint_payer`
- `folder_hint_lease`
- `outlook_subject_keywords`
- `outlook_sender_domain`

These are persisted as JSON on `doc.document_candidates.reason_codes_json` and
surfaced in the UI as chips on each candidate row.

## Migrations

Run the canonical foundation migration first
(`migrations/practicex_initial_enterprise_foundation.sql`) and then the source
discovery extensions migration
(`migrations/20260425_source_discovery_extensions.sql`). Both are idempotent.
