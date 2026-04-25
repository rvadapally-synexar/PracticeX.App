# PracticeX Enterprise Product Requirements Document

## 1. Executive summary

PracticeX should be designed as an enterprise-grade healthcare contract command center for independent GI groups, ASCs, specialty practices, and multi-site facilities. The public website positions the product around uploading existing documents, extracting contract terms, tracking renewals, surfacing money left on the table, and preparing practices for future negotiations ([PracticeX](https://practicex.ai/)).

This should not begin as a proof of concept, chatbot, or simple document repository. It should begin as a domain-modeled enterprise application with a workflow-first design, canonical data model, governed ingestion pipeline, review queues, audit trails, permission model, and extensible agent framework.

The recommended greenfield sequence is:

1. Product domain design: personas, workflows, states, permissions, data objects, and enterprise constraints.
2. Information architecture and low-fidelity UX flows: upload, ingestion, review, repository, renewal dashboard, search, portfolio intelligence.
3. Canonical data model and event model: contracts, documents, extracted fields, evidence links, review tasks, renewals, obligations, agent runs, audit events.
4. API/tool/agent contracts: deterministic services first, agents behind typed interfaces second.
5. High-fidelity mocks and clickable prototype: validate with 3-5 target practice administrators/operators.
6. Application skeleton: auth, tenancy, data model, ingestion pipeline, review queue, audit logging, and observability.
7. Feature implementation by workflow slice: upload ingestion, extraction review, repository, renewals, portfolio insights, negotiation copilot.

The most important product decision is that every AI output must be evidence-backed, confidence-scored, reviewable, and auditable. The website already promises extracted terms that are scored for confidence and surfaced for human review before going live ([PracticeX](https://practicex.ai/)).

## 2. Product vision

PracticeX should become the operating layer for practice contracts and economics.

In the first release, it should answer:

- What contracts do we have?
- Which facilities and owners are responsible?
- What renews next?
- What notice windows are at risk?
- Which documents and terms still need review?
- Where are our payer contracts, rate schedules, vendor agreements, leases, and processor agreements?

In the second release, it should answer:

- Which contracts are economically weak?
- Which payers or facilities are out of market?
- Which renewals deserve negotiation?
- What should the practice ask for?
- What evidence supports that ask?

In the third release, it should help operators act:

- Generate renewal playbooks.
- Draft notice letters.
- Prepare negotiation call briefs.
- Route approvals.
- Send outbound notices only after explicit human approval.
- Optionally coordinate e-signature and document execution.

The website frames this trajectory as starting narrow on visibility and expanding into reimbursement intelligence and a negotiation copilot on the same contract base ([PracticeX](https://practicex.ai/)).

## 3. Product strategy

### 3.1 What comes first?

For an enterprise greenfield application, do not start by coding screens, and do not start by creating high-fidelity mocks in isolation. Start with product domain design.

The correct first design artifact is a combined product/domain blueprint:

| Artifact | Why it comes early | Output |
| --- | --- | --- |
| Personas and job stories | Clarifies whose operational pain matters | Admin, operator, CFO, counsel, facility owner, implementation user |
| Core workflow maps | Prevents fragmented feature design | Upload, ingest, extract, review, search, renew, negotiate |
| Domain object model | Prevents data model rewrites | Facility, contract, document, evidence, extracted field, renewal, obligation |
| State machines | Makes workflows testable | Document state, extraction state, field review state, contract lifecycle state |
| Permission model | Enterprise requirement, not a later add-on | Tenant, facility, role, owner, reviewer |
| Audit and evidence model | Required for trust and compliance | Who/what/when/why for every field and action |
| Low-fidelity mocks | Validates workflows before UI polish | Clickable wireframes |
| API and agent contracts | Keeps AI modular and testable | Typed services, tool schemas, agent I/O contracts |

High-fidelity design should come after the workflows and domain model are stable enough that the UI is not designing the database by accident. Engineering should start after the team can answer the following:

- What is the lifecycle of a document from upload to accepted contract record?
- What is the lifecycle of an extracted field from candidate to reviewed fact?
- What is the lifecycle of a renewal from detection to closed negotiation?
- What actions require approval?
- What data must be immutable?
- What fields are facility-scoped?
- What does the agent see, and what is never exposed to the agent?

### 3.2 Product principles

1. Evidence-first: every accepted extracted field must be traceable to source evidence.
2. Review-first: AI suggests, humans approve high-impact facts and outbound actions.
3. Workflow-first: users should manage work, not browse a file cabinet.
4. Facility-native: multi-location practices need both facility-level views and portfolio rollups.
5. Operator-first: workflows must work for small practices without IT sophistication.
6. Extensible ingestion: bulk upload and Drive should be easy; SFTP should exist for more sophisticated customers but not define the onboarding experience.
7. Typed agent platform: every agent has structured input, structured output, tests, and versioned prompts.
8. Audit by default: every read, edit, export, review, agent run, and outbound action is logged.
9. No silent automation: notices, emails, e-signature packages, and destructive changes require explicit approval.
10. Enterprise from day one: tenancy, RBAC, encryption, audit, observability, data retention, and compliance evidence are foundational.

## 4. Problem statement

Independent and multi-site healthcare practices often have contracts scattered across inboxes, shared drives, local desktops, scanned PDFs, vendor portals, and counsel folders. PracticeX’s website describes the operational problem clearly: ask a five-location group to name its active payer contracts and each facility admin may give a different answer; notice windows close quietly; auto-renewals fire without review; and practices lack rate visibility across payers and facilities ([PracticeX](https://practicex.ai/)).

The cost of not solving this problem is operational, financial, and governance-related:

- Missed notice windows and auto-renewals.
- Underpriced payer contracts that are not identified before renewal.
- Lack of accountability because no single owner is attached to each contract.
- Manual review burden on practice admins who already have many responsibilities.
- Fragmented knowledge across facilities and counterparties.
- No reliable evidence trail for extracted terms, edits, or negotiation decisions.

PracticeX should solve this by becoming the system of record and system of action for practice contracts.

## 5. Target users

| Persona | Description | Primary needs |
| --- | --- | --- |
| Practice administrator | Runs daily operations for one or more facilities | Upload documents, find contracts, assign owners, track renewals, receive alerts |
| Facility admin | Local operator responsible for facility-specific agreements | Facility-scoped view, task list, document upload, review assigned fields |
| Executive/operator | Leadership across multiple facilities | Portfolio rollups, underpriced contracts, renewal risk, owner accountability |
| CFO/revenue leader | Responsible for payer economics and reimbursement | Rate schedule visibility, benchmark comparisons, opportunity prioritization |
| Counsel/legal reviewer | Reviews clauses and risk, not daily schedules | Clause evidence, risk summaries, review queue, audit trail |
| Implementation specialist | Onboards new practice customers | Bulk upload, Drive connection, mapping facilities, cleaning metadata |
| PracticeX support/success | Helps customers activate and maintain data quality | Ingestion monitoring, error handling, review progress, support-safe admin tools |

## 6. Goals

### 6.1 User goals

| Goal | Target outcome |
| --- | --- |
| Fast onboarding | A practice can bulk upload or connect a document source and see a usable contract inventory quickly |
| Trusted extraction | Users can review extracted effective dates, notice windows, renewal terms, and rate schedule presence with evidence |
| Renewal safety | Users can see upcoming notice deadlines, auto-renewals, and owner assignments in one dashboard |
| Searchability | Users can search across documents and extracted fields by facility, counterparty, type, owner, and status |
| Portfolio visibility | Leadership can view contract and renewal exposure across facilities |

### 6.2 Business goals

| Goal | Target outcome |
| --- | --- |
| Become contract system of record | Customers use PracticeX as the canonical location for active practice contracts |
| Expand into rate intelligence | Contract data supports benchmarking and underpricing detection |
| Enable negotiation copilot | Renewal intelligence becomes a paid Pro tier |
| Reduce onboarding friction | Non-technical practices can activate without SFTP or enterprise IT |
| Build defensible data asset | Reviewed contract fields and rate schedules become proprietary structured data |

## 7. Non-goals

| Non-goal | Rationale |
| --- | --- |
| Full enterprise CLM replacement in V1 | PracticeX should be operator-first for healthcare practices, not a generic legal department CLM |
| Autonomous legal advice | The product can summarize, flag, and draft, but humans approve and counsel remains responsible |
| Autonomous contract execution | E-signature and outbound notices should be approval-gated and likely post-MVP |
| Full reimbursement analytics platform in V1 | Rate intelligence depends on reliable contract and rate schedule extraction first |
| SFTP-first onboarding | SFTP should be supported later, but small practices need drag-and-drop upload and Drive-based ingestion |
| EHR/PMS dependency in V1 | Initial value should come from contracts and documents without requiring deep clinical system integration |

## 8. Feature inventory

The website explicitly emphasizes upload, extraction, confidence scoring, human review, date tracking, dashboards, email alerts, search, filtering, ownership, audit logging, multi-facility workspaces, facility-scoped permissions, portfolio rollups, renewal prioritization, risk summaries, suggested asks, draft language, call prep, payer policy research, sourced memos, approval chains, outbound notice generation, and optional e-signature ([PracticeX](https://practicex.ai/)).

### 8.1 Core modules

| Module | Description | Priority |
| --- | --- | --- |
| Workspace setup | Tenant, facilities, users, roles, contract types, owners | P0 |
| Bulk upload ingestion | Drag-and-drop upload of PDFs, scans, DOCX, spreadsheets, ZIPs | P0 |
| Drive ingestion | Connect/select Google Drive folder or shared drive for ingestion | P0/P1 |
| Document normalization | Preserve originals, convert/OCR text, extract pages/tables | P0 |
| Contract classification | Identify payer, vendor, lease, employee, processor, amendment, fee schedule | P0 |
| Term extraction | Effective date, termination date, notice window, auto-renewal, rate schedule presence, termination terms | P0 |
| Confidence and review | Confidence scoring, human review queue, accepted/rejected/edited fields | P0 |
| Contract repository | Canonical contract record with documents, fields, clauses, versions, evidence | P0 |
| Search and saved views | Search documents and fields; filter by facility, owner, type, status, counterparty | P0 |
| Renewal dashboard | Calendar, dashboard, lead times, notice windows, weekly digest | P0 |
| Ownership and tasks | Exactly one accountable owner per contract; task assignment and review SLAs | P0 |
| Audit trail | Review logs, field edit history, prior value, reviewer, timestamp | P0 |
| Portfolio rollups | Multi-facility dashboards and leadership views | P1 |
| Email ingestion | Gmail/Outlook discovery agents for contract evidence | P1 |
| SFTP ingestion | Scheduled secure file drop for sophisticated customers | P2 |
| Rate schedule extraction | CPT/HCPCS/rate table extraction and structured rates | P1 |
| Rate intelligence | Benchmark comparisons and underpricing detection | P1/P2 |
| Renewal prioritization | Rank renewals by deadline, economics, confidence, and risk | P1 |
| Negotiation copilot | Risk summaries, suggested asks, draft language, call prep | P2 |
| Payer policy research | Related policy updates and sourced memos | P2 |
| Outbound notices | Approval-gated notice generation and delivery record | P2 |
| Optional e-signature | Prepare/send e-sign packages after approval | P3 |

## 9. Product workflows

### 9.1 Recommended onboarding workflow

```text
1. Create tenant workspace.
2. Add facilities.
3. Configure contract types and default extraction schemas.
4. Invite users and assign roles.
5. Select ingestion method:
   a. Bulk upload
   b. Google Drive folder/shared drive
   c. Outlook/Gmail mailbox discovery
   d. SFTP, later and optional
6. Map uploaded documents to facilities/counterparties when possible.
7. Run ingestion and extraction.
8. Present review queue.
9. Publish accepted contract records to repository.
10. Activate renewal dashboard and alerts.
```

The onboarding experience should make bulk upload and Drive connection the default. SFTP should be positioned as an enterprise/IT-supported option, not the primary setup path.

### 9.2 Bulk upload workflow

```text
User selects files or ZIP
        ↓
Upload service stores originals
        ↓
Virus scan / file validation / dedupe hash
        ↓
Document normalization
        ↓
OCR and layout extraction
        ↓
Document classification
        ↓
Facility/counterparty/type suggestions
        ↓
Term and rate schedule extraction
        ↓
Confidence scoring
        ↓
Human review queue
        ↓
Contract repository publish
        ↓
Renewal dashboard and alerts
```

Acceptance criteria:

| Scenario | Given | When | Then |
| --- | --- | --- | --- |
| Upload PDFs | User has upload permission | User uploads multiple PDFs | System creates document assets and starts ingestion jobs |
| Upload ZIP | User uploads ZIP with nested folders | System processes ZIP | Documents are extracted while retaining folder path metadata |
| Duplicate file | File hash already exists | User uploads duplicate | System links evidence without creating duplicate canonical document |
| Bad file | File is unsupported or corrupted | User uploads file | System marks file failed with user-readable error |
| Large upload | Upload exceeds configured limit | User uploads file batch | System rejects or chunks based on policy and reports limit clearly |

### 9.3 Google Drive ingestion workflow

```text
User connects Google account
        ↓
User selects folder/shared drive
        ↓
System scans metadata and permissions
        ↓
User confirms ingestion scope
        ↓
System imports eligible files
        ↓
System records source object IDs and hashes
        ↓
Extraction pipeline runs
        ↓
Review queue and repository update
```

Acceptance criteria:

| Scenario | Given | When | Then |
| --- | --- | --- | --- |
| Folder selected | User authorizes Drive access | User selects a folder | System ingests only files within approved scope |
| Permission change | File is removed from Drive scope | Sync runs | System preserves already-ingested evidence but records source access change |
| Unsupported file | Drive contains unsupported MIME type | Sync runs | System skips file and reports skip reason |
| Incremental sync | Folder has new files | Scheduled sync runs | System imports only new or changed files |
| Revoked auth | User revokes Drive access | Sync runs | System marks connection inactive and does not fail unrelated workflows |

### 9.4 SFTP ingestion workflow

SFTP should be an optional enterprise ingestion path for customers with IT support.

```text
Admin provisions SFTP drop zone
        ↓
Customer uploads files by folder convention
        ↓
Scheduled ingest scans new files
        ↓
Files are validated, hashed, and archived
        ↓
Pipeline runs same normalization and extraction flow
```

Acceptance criteria:

| Scenario | Given | When | Then |
| --- | --- | --- | --- |
| Valid file drop | SFTP folder has new PDFs | Scheduled job runs | Files are imported and moved/marked as processed |
| Duplicate drop | Same file appears twice | Scheduled job runs | Duplicate is skipped by hash |
| Invalid folder | Folder cannot map to facility | Scheduled job runs | File enters mapping review queue |
| Customer lacks SFTP capability | Customer does not use SFTP | Onboarding begins | UI defaults to bulk upload and Drive, not SFTP |

### 9.5 Extraction and review workflow

```text
Document classified
        ↓
Extraction schema selected
        ↓
Field candidates extracted
        ↓
Evidence links attached
        ↓
Confidence score calculated
        ↓
Policy decides:
   high confidence + low risk → auto-accept if allowed
   low confidence or high impact → review required
        ↓
Reviewer accepts, edits, rejects, or requests more evidence
        ↓
Accepted fields update canonical contract record
        ↓
Audit event records prior value, new value, reviewer, timestamp
```

Acceptance criteria:

| Scenario | Given | When | Then |
| --- | --- | --- | --- |
| Extracted date | Contract contains effective date | Extraction runs | Candidate field includes value, confidence, and evidence link |
| Low confidence | Confidence below threshold | Extraction completes | Field enters review queue |
| Reviewer edit | Reviewer changes extracted value | Reviewer saves | System stores prior value, new value, reviewer, timestamp |
| Missing evidence | Field lacks evidence link | Extraction completes | Field cannot be auto-accepted |
| Conflicting docs | Amendment conflicts with original | Extraction completes | Conflict is flagged for review |

### 9.6 Renewal workflow

```text
Contract fields accepted
        ↓
Renewal profile calculated
        ↓
Notice deadline and lead times generated
        ↓
Tasks assigned to owner
        ↓
Dashboard/calendar/email digest updated
        ↓
Renewal risk and opportunity assessed
        ↓
Optional negotiation workflow starts
```

Acceptance criteria:

| Scenario | Given | When | Then |
| --- | --- | --- | --- |
| Auto-renewal clause exists | Contract has accepted renewal terms | Renewal calculation runs | Notice deadline and latest safe action date are created |
| Missing owner | Contract lacks owner | Contract enters repository | System requires owner assignment before active status |
| Upcoming deadline | Deadline is within configured lead time | Scheduler runs | Owner receives task and digest item |
| Date edited | Reviewer changes notice window | Field is saved | Renewal profile recalculates and audit event records change |

## 10. Information architecture

### 10.1 Primary navigation

| Area | Purpose |
| --- | --- |
| Command Center | Executive overview: contracts, deadlines, review queue, renewal risk, portfolio signals |
| Inbox / Ingestion | Uploads, Drive sync, failed files, source connections, ingestion jobs |
| Review Queue | Extracted fields, classifications, conflicts, missing data, low-confidence items |
| Contracts | Canonical contract repository |
| Renewals | Calendar, notice windows, tasks, lead times, auto-renewals |
| Rates | Rate schedules, payer comparisons, facility comparisons, underpricing opportunities |
| Search | Command-K global search across documents and fields |
| Reports | Portfolio dashboards, weekly digests, exportable summaries |
| Admin | Facilities, users, roles, sources, schemas, alerts, audit, security |

### 10.2 Core screens

| Screen | Key elements |
| --- | --- |
| Workspace setup | Facilities, users, roles, contract types |
| Bulk upload | Drag-drop, folder tagging, facility mapping, progress |
| Source connections | Drive/Outlook/Gmail/SFTP setup and sync status |
| Ingestion monitor | Jobs, failures, skipped files, duplicates, extracted documents |
| Review queue | Field cards with evidence viewer, confidence, accept/edit/reject |
| Contract detail | Summary, documents, fields, clauses, versions, rates, renewals, tasks, audit |
| Contract repository | Table/cards with filters, saved views, owner, facility, status |
| Renewal dashboard | Timeline, calendar, notice deadlines, owner tasks, risk |
| Rate intelligence | Rate schedules, benchmark deltas, opportunity score |
| Negotiation brief | Risk summary, suggested asks, evidence, draft language |
| Audit log | Resource-level and tenant-level audit trail |

### 10.3 Mocking plan

Before coding, create low-fidelity mocks for:

1. First-run workspace setup.
2. Bulk upload and progress monitor.
3. Drive folder selection and sync preview.
4. Review queue with evidence side-by-side.
5. Contract detail page.
6. Renewal dashboard.
7. Portfolio command center.
8. Rate intelligence view.
9. Negotiation memo and approval gate.
10. Admin permissions and source connections.

High-fidelity mocks should be created only after the domain model and workflow states are stable.

## 11. Canonical data model

### 11.1 Tenancy and access

| Entity | Fields | Notes |
| --- | --- | --- |
| `Tenant` | `id`, `name`, `status`, `data_region`, `baa_status`, `created_at` | Customer boundary |
| `Facility` | `id`, `tenant_id`, `name`, `npi`, `tax_id`, `address`, `specialty`, `status` | Facility-scoped views |
| `User` | `id`, `tenant_id`, `email`, `name`, `status`, `last_login_at` | Human user |
| `Role` | `id`, `tenant_id`, `name`, `permissions` | Admin, reviewer, owner, viewer |
| `RoleAssignment` | `user_id`, `facility_id`, `role_id` | Facility-scoped permissions |
| `OwnerAssignment` | `contract_id`, `owner_user_id`, `assigned_by`, `assigned_at`, `status` | One accountable owner per active contract |

### 11.2 Source and ingestion

| Entity | Fields | Notes |
| --- | --- | --- |
| `SourceConnection` | `id`, `tenant_id`, `source_type`, `status`, `oauth_subject`, `scope_set`, `last_sync_at` | Drive, Gmail, Outlook, SFTP |
| `SourceObject` | `id`, `connection_id`, `external_id`, `uri`, `name`, `mime_type`, `hash`, `created_at`, `modified_at` | External file/email object |
| `IngestionBatch` | `id`, `tenant_id`, `source_type`, `created_by`, `status`, `file_count`, `started_at`, `completed_at` | Upload batch or sync run |
| `IngestionJob` | `id`, `batch_id`, `source_object_id`, `document_asset_id`, `status`, `error_code`, `attempt_count` | Per-file pipeline job |
| `DocumentAsset` | `id`, `tenant_id`, `source_object_id`, `storage_uri`, `sha256`, `mime_type`, `page_count`, `text_status`, `ocr_status` | Preserved original |
| `DocumentPage` | `id`, `document_asset_id`, `page_number`, `text`, `image_uri`, `ocr_confidence` | Evidence viewer |
| `DocumentCandidate` | `id`, `document_asset_id`, `candidate_type`, `facility_hint`, `counterparty_hint`, `confidence`, `status` | Before canonical contract creation |

### 11.3 Contract intelligence

| Entity | Fields | Notes |
| --- | --- | --- |
| `Counterparty` | `id`, `tenant_id`, `name`, `type`, `aliases`, `payer_identifier` | Payer/vendor/landlord/employee/processor |
| `Contract` | `id`, `tenant_id`, `facility_id`, `counterparty_id`, `contract_type`, `status`, `owner_user_id` | Canonical business record |
| `ContractVersion` | `id`, `contract_id`, `document_asset_id`, `version_type`, `execution_date`, `effective_date`, `supersedes_version_id` | Original, amendment, addendum |
| `ExtractionSchema` | `id`, `contract_type`, `version`, `field_definitions`, `status` | Versioned extraction requirements |
| `ContractField` | `id`, `contract_id`, `schema_version`, `field_key`, `value_json`, `normalized_value`, `confidence`, `review_status` | Structured term |
| `ContractClause` | `id`, `contract_id`, `clause_type`, `text`, `risk_score`, `evidence_link_id` | Renewal, termination, assignment, exclusivity |
| `EvidenceLink` | `id`, `resource_type`, `resource_id`, `document_asset_id`, `page_number`, `bbox`, `quote`, `source_object_id`, `agent_run_id` | Provenance |

### 11.4 Rates and renewals

| Entity | Fields | Notes |
| --- | --- | --- |
| `RateSchedule` | `id`, `contract_id`, `facility_id`, `payer_id`, `effective_date`, `refresh_date`, `basis`, `source_document_id` | Contract economics |
| `RateLine` | `id`, `rate_schedule_id`, `code`, `code_type`, `description`, `allowed_amount`, `percent_of_medicare`, `modifier`, `confidence` | CPT/HCPCS or other codes |
| `RenewalProfile` | `id`, `contract_id`, `renewal_type`, `term_months`, `notice_days`, `notice_deadline`, `auto_renewal_date`, `latest_safe_action_date`, `confidence` | Drives alerts |
| `Obligation` | `id`, `contract_id`, `type`, `description`, `due_date`, `frequency`, `owner_user_id`, `status` | Operational tasks |
| `AlertRule` | `id`, `tenant_id`, `scope`, `event_type`, `lead_time_days`, `channel`, `enabled` | Configurable reminders |
| `Task` | `id`, `tenant_id`, `facility_id`, `contract_id`, `owner_user_id`, `type`, `due_date`, `status` | Work management |

### 11.5 AI, review, and audit

| Entity | Fields | Notes |
| --- | --- | --- |
| `WorkflowRun` | `id`, `tenant_id`, `workflow_type`, `trigger_type`, `thread_id`, `status`, `checkpoint_ref` | Long-running workflow |
| `AgentRun` | `id`, `workflow_run_id`, `agent_name`, `agent_version`, `model`, `input_hash`, `output_hash`, `status`, `trace_id` | Observability |
| `ToolCall` | `id`, `agent_run_id`, `tool_name`, `tool_version`, `arguments_hash`, `result_hash`, `is_error`, `duration_ms` | Tool audit |
| `ReviewTask` | `id`, `tenant_id`, `resource_type`, `resource_id`, `reason`, `priority`, `assigned_to`, `decision`, `resolved_at` | Human review |
| `FieldReviewDecision` | `id`, `contract_field_id`, `reviewer_id`, `prior_value_json`, `new_value_json`, `decision`, `timestamp` | Field-level audit |
| `AuditEvent` | `id`, `tenant_id`, `actor_type`, `actor_id`, `event_type`, `resource_type`, `resource_id`, `prior_value_hash`, `new_value_hash`, `timestamp` | Append-only audit |
| `EvaluationRun` | `id`, `agent_name`, `dataset_id`, `version`, `metrics_json`, `passed`, `created_at` | Regression testing |

## 12. State models

### 12.1 Document lifecycle

```text
uploaded
→ validated
→ normalized
→ ocr_completed
→ classified
→ extraction_completed
→ review_pending
→ accepted
→ linked_to_contract
→ archived
```

Failure states:

```text
validation_failed
normalization_failed
ocr_failed
classification_failed
extraction_failed
duplicate
unsupported
needs_manual_mapping
```

### 12.2 Field review lifecycle

```text
candidate
→ evidence_attached
→ confidence_scored
→ auto_accepted | review_required
→ accepted | edited | rejected | needs_more_evidence
→ published
```

### 12.3 Contract lifecycle

```text
draft
→ active
→ renewal_watch
→ notice_window_open
→ negotiation
→ renewed | terminated | expired | superseded
```

### 12.4 Renewal lifecycle

```text
detected
→ calculated
→ owner_assigned
→ alert_scheduled
→ review_started
→ negotiation_prepared
→ notice_generated
→ notice_approved
→ notice_sent
→ closed
```

## 13. Agent and automation architecture

### 13.1 Architecture pattern

PracticeX should use deterministic services for core system actions and agents for ambiguity-heavy work.

| Layer | Examples | Rule |
| --- | --- | --- |
| Deterministic services | Upload, OCR orchestration, RBAC, date math, audit, database writes, alert scheduling | Must be testable without LLMs |
| Connector tools | Drive search, Gmail search, Outlook search, SFTP import, file export | Typed inputs/outputs and scoped permissions |
| Specialist agents | Classification, extraction, evidence discovery, rate extraction, renewal memo | Narrow scope, structured output |
| Workflow orchestrators | Ingestion, review, renewal prep, negotiation prep | Persisted state and checkpointing |

OpenAI’s Agents SDK supports code-first orchestration, direct control over tools and MCP servers, guardrails, human review, state, sandboxing, and observability for production agent applications ([OpenAI Agents SDK](https://developers.openai.com/api/docs/guides/agents)). LangGraph-style workflows are appropriate for PracticeX because checkpointing supports human-in-the-loop, memory, replay, time travel, and fault-tolerant execution ([LangGraph persistence docs](https://docs.langchain.com/oss/python/langgraph/persistence)).

### 13.2 Initial agent catalog

| Agent | Responsibility | Priority |
| --- | --- | --- |
| `DocumentClassificationAgent` | Classify document type, contract type, counterparty, facility hints | P0 |
| `ContractTermExtractionAgent` | Extract core terms and evidence links | P0 |
| `RenewalClauseAgent` | Extract and calculate renewal/notice profile | P0 |
| `EvidenceAttributionAgent` | Attach quotes/pages/coordinates/source object to fields | P0 |
| `GoogleDriveEvidenceAgent` | Discover likely contracts in Drive | P1 |
| `GmailEvidenceAgent` | Discover contract evidence in Gmail | P1 |
| `OutlookEvidenceAgent` | Discover contract evidence in Outlook | P1 |
| `RateScheduleExtractionAgent` | Extract rate tables and fee schedules | P1 |
| `ContradictionResolutionAgent` | Resolve original/amendment conflicts | P1 |
| `PortfolioBenchmarkAgent` | Compare rates and contract terms across facilities | P2 |
| `RenewalRiskSummaryAgent` | Generate sourced renewal memo | P2 |
| `SuggestedAsksAgent` | Recommend negotiation asks | P2 |
| `DraftLanguageAgent` | Draft notices and negotiation language | P2 |
| `PayerPolicyResearchAgent` | Retrieve and summarize payer policy updates | P2 |

### 13.3 Tool contract standard

Every connector/tool should have:

- Unique tool name.
- Human-readable description.
- JSON Schema input schema.
- JSON Schema output schema where possible.
- Explicit error taxonomy.
- Tenant and actor context.
- Facility scope.
- Idempotency key for writes.
- Timeout and retry behavior.
- Audit event emission.
- Unit tests with mocked provider responses.

MCP tools use unique names, descriptions, JSON Schema input schemas, optional output schemas, structured content, and explicit tool-error behavior, making MCP a strong model for reusable/testable connectors ([MCP tools spec](https://modelcontextprotocol.io/specification/2025-11-25/server/tools)).

### 13.4 Human-in-the-loop

Human approval is required for:

- Publishing low-confidence or high-impact extracted fields.
- Resolving contract/amendment conflicts.
- Generating outbound notices.
- Sending emails or e-signature packages.
- Exporting sensitive contract packages.
- Deleting or archiving canonical records.
- Applying bulk field updates.

LangChain’s human-in-the-loop middleware supports tool-level approval policies with approve, edit, and reject decisions, and requires checkpointing so workflows can pause and resume with the same thread ID ([LangChain human-in-the-loop docs](https://docs.langchain.com/oss/python/langchain/human-in-the-loop)).

## 14. Security, privacy, and compliance requirements

### 14.1 Product commitments

The website states that PracticeX uses encryption at rest and in transit, BAA availability, audit-logged access, row-level tenancy isolation, AWS US-East with HIPAA-eligible services, and no offshore processing of PHI-adjacent contract content ([PracticeX](https://practicex.ai/)).

### 14.2 Required controls

| Control | Requirement | Priority |
| --- | --- | --- |
| Tenant isolation | Strict row-level and object-level tenant boundaries | P0 |
| Facility-scoped permissions | Users only see permitted facilities | P0 |
| RBAC | Admin, owner, reviewer, viewer, implementation support | P0 |
| Audit logging | All reads, writes, exports, reviews, agent actions, and source connections | P0 |
| Encryption | Data encrypted at rest and in transit | P0 |
| Secrets handling | OAuth tokens and SFTP credentials never exposed to LLM context | P0 |
| Access review | Admin can review users, roles, source connections | P1 |
| Data retention | Tenant-level retention and deletion policies | P1 |
| Compliance evidence | SOC 2/HIPAA evidence collection hooks | P1 |
| Incident support | Security event logs and alerting | P1 |

The HIPAA Security Rule establishes national standards for protecting electronic protected health information and includes administrative, physical, and technical safeguards for confidentiality, integrity, and availability ([HHS Security Rule](https://www.hhs.gov/hipaa/for-professionals/security/laws-regulations/index.html)). NIST describes trustworthy AI characteristics as valid and reliable, safe, secure and resilient, accountable and transparent, explainable and interpretable, privacy-enhanced, and fair with harmful bias managed ([NIST AI RMF characteristics](https://airc.nist.gov/airmf-resources/airmf/3-sec-characteristics/)).

### 14.3 Connector authorization

MCP authorization guidance requires OAuth 2.1, PKCE, token audience validation, protected resource metadata, exact redirect URI validation, state verification, confused-deputy prevention, and no token passthrough ([MCP authorization spec](https://modelcontextprotocol.io/specification/2025-11-25/basic/authorization)). PracticeX should adopt these rules internally for Drive, Gmail, Outlook, SharePoint, and future connectors.

## 15. Requirements

### 15.1 P0 requirements

| ID | Requirement | Acceptance criteria |
| --- | --- | --- |
| P0-1 | Tenant and facility setup | Given an admin, when facilities are created, then contracts/documents/tasks can be scoped by facility |
| P0-2 | User roles and permissions | Given a user without facility access, when they search, then restricted contracts never appear |
| P0-3 | Bulk upload | Given permitted user, when they upload PDFs/DOCX/scans/ZIPs, then ingestion jobs are created |
| P0-4 | Original preservation | Given any uploaded document, when normalized, then original file remains immutable |
| P0-5 | OCR/text extraction | Given scanned PDFs, when ingestion runs, then searchable text and page records are created |
| P0-6 | Document classification | Given an ingested document, when classification runs, then type/counterparty/facility hints are produced |
| P0-7 | Core term extraction | Given a contract, when extraction runs, then effective date, termination date, notice window, auto-renewal, and rate schedule presence are attempted |
| P0-8 | Evidence links | Given an extracted field, when shown to reviewer, then source quote/page/document is visible |
| P0-9 | Confidence scoring | Given extraction output, when confidence is below threshold, then review is required |
| P0-10 | Review queue | Given review-required fields, when reviewer accepts/edits/rejects, then decision is logged |
| P0-11 | Contract repository | Given accepted fields, when contract is published, then it appears in repository with status and owner |
| P0-12 | Search and filters | Given repository data, when user filters by facility/type/owner/status/counterparty, then results are permission-scoped |
| P0-13 | Renewal profile | Given accepted renewal fields, when calculation runs, then notice deadline and latest safe action date are created |
| P0-14 | Alerts and digests | Given upcoming deadline, when scheduler runs, then owner receives alert based on lead time |
| P0-15 | Ownership | Given active contract, when no owner exists, then system requires assignment |
| P0-16 | Audit logging | Given any review/edit/export/source action, when action completes, then audit event is written |
| P0-17 | Admin configuration | Given admin, when configuring lead times and contract types, then system applies them to workflows |
| P0-18 | Ingestion monitoring | Given batch ingestion, when files fail, then errors are visible and actionable |

### 15.2 P1 requirements

| ID | Requirement | Acceptance criteria |
| --- | --- | --- |
| P1-1 | Google Drive folder ingestion | User can connect/select folder and ingest eligible files |
| P1-2 | Incremental Drive sync | New/changed files are detected without reprocessing unchanged files |
| P1-3 | Gmail evidence discovery | Authorized user can discover likely contract attachments and emails |
| P1-4 | Outlook evidence discovery | Authorized user can discover likely contract attachments and emails |
| P1-5 | Rate schedule extraction | Fee schedules and rate tables are extracted into structured records |
| P1-6 | Portfolio rollups | Leadership can view contracts, renewals, tasks, and risks across facilities |
| P1-7 | Saved views | Users can save common repository and dashboard filters |
| P1-8 | Conflict resolution | Amendments and conflicting extracted fields are flagged and resolved |
| P1-9 | Weekly digest | Owners and leadership receive configurable summaries |
| P1-10 | Data quality dashboard | Admin can see missing owners, missing rate schedules, low-confidence fields |

### 15.3 P2 requirements

| ID | Requirement | Acceptance criteria |
| --- | --- | --- |
| P2-1 | SFTP ingestion | Enterprise admin can configure SFTP drop zone and scheduled ingestion |
| P2-2 | Renewal prioritization | Renewals are ranked by deadline, risk, economics, and confidence |
| P2-3 | Benchmark comparisons | Rates and terms can be compared across facilities/payers |
| P2-4 | Underpricing detection | System identifies potential economic opportunities |
| P2-5 | Renewal risk memo | System generates sourced memo for upcoming renewal |
| P2-6 | Suggested negotiation asks | System proposes asks with evidence and rationale |
| P2-7 | Draft language | System drafts notice/negotiation language for approval |
| P2-8 | Payer policy research | System retrieves and summarizes relevant payer policy updates |
| P2-9 | Approval chains | Drafts and high-impact outputs route to configured approvers |
| P2-10 | Outbound notice generation | Approved notice packages can be generated and logged |

### 15.4 P3 requirements

| ID | Requirement | Acceptance criteria |
| --- | --- | --- |
| P3-1 | Optional e-signature | Approved documents can be sent for e-signature |
| P3-2 | Advanced portal connectors | Payer/vendor portal download automation where legally and technically feasible |
| P3-3 | EHR/PMS integrations | Future enrichment using practice management/reimbursement data |
| P3-4 | Advanced benchmarking | External benchmark datasets or customer cohort benchmarks |

## 16. Success metrics

### 16.1 Activation metrics

| Metric | Target |
| --- | --- |
| Time to first document ingested | Same day during onboarding |
| Time to first reviewed contract | Within first onboarding session for pilot customer |
| Percent uploaded documents classified | High enough to avoid manual triage becoming primary workflow |
| Percent contracts with owner assigned | Near 100% for active contracts |
| Percent contracts with renewal profile | High for active payer/vendor/lease contracts |

### 16.2 Quality metrics

| Metric | Target |
| --- | --- |
| Evidence coverage | 100% of accepted extracted fields have evidence links |
| Wrong auto-accepted high-impact fields | Zero tolerance policy |
| Extraction precision by field | Measured by golden dataset and review outcomes |
| Extraction recall by field | Measured by golden dataset and review outcomes |
| Review edit rate | Used to identify weak extraction fields |
| Cross-tenant leakage | Zero tolerance |

### 16.3 Business metrics

| Metric | Target |
| --- | --- |
| Renewal windows surfaced | Customers see upcoming notice windows reliably |
| Missed renewal reduction | Track before/after where baseline exists |
| Data completeness | Increase contracts with owner, facility, counterparty, dates, rate schedule status |
| Pro conversion | Customers with rate intelligence/negotiation workflows upgrade |
| Retention | Customers continue using renewal dashboard and repository |

## 17. Testing and evaluation

### 17.1 Test layers

| Layer | Tests |
| --- | --- |
| Unit | Date math, role checks, schema validation, confidence scoring |
| Connector | Drive/Gmail/Outlook/SFTP pagination, auth errors, rate limits, unsupported files |
| Document processing | OCR, PDF conversion, table extraction, duplicate detection |
| Agent golden tests | Known contracts with expected fields and evidence |
| Agent adversarial tests | Prompt injection inside documents/emails, conflicting amendments, fake instructions |
| Workflow tests | Upload → extraction → review → publish → renewal |
| Permission tests | Facility-scoped search and object access |
| Audit tests | Every high-value action emits immutable audit event |
| Regression tests | Agent version changes must pass evaluation datasets |

### 17.2 Evaluation datasets

Create datasets for:

- Payer contracts.
- Vendor contracts.
- Leases.
- Employment agreements.
- Processor agreements.
- Amendments/addenda.
- Fee schedules.
- Poor-quality scans.
- Duplicate and superseded documents.
- Email attachments and Drive folder structures.

Each dataset should include expected classification, expected extracted fields, acceptable evidence spans, and review-risk labels.

## 18. Roadmap

### 18.1 Now / Next / Later

| Horizon | Theme | Deliverables |
| --- | --- | --- |
| Now | Enterprise foundation and core intake | Tenancy, facilities, users, roles, bulk upload, document normalization, OCR, classification, extraction, review queue, repository, audit |
| Next | Operational contract command center | Renewal dashboard, ownership, alerts, saved views, Drive ingestion, Gmail/Outlook discovery, portfolio rollups, data quality dashboard |
| Later | Rate intelligence and negotiation copilot | Rate schedule extraction, benchmark comparisons, underpricing detection, renewal prioritization, risk memos, suggested asks, draft language, approval chains |
| Future | Extended enterprise workflows | SFTP, e-signature, payer/vendor portal connectors, EHR/PMS enrichment, external benchmarks |

### 18.2 Phase roadmap

#### Phase 0: Product and architecture design

Duration: 2-4 weeks.

Deliverables:

- PRD.
- Workflow maps.
- Canonical data model.
- State machines.
- Permission model.
- Ingestion architecture.
- Agent/tool contracts.
- Low-fidelity mocks.
- Evaluation dataset plan.
- Technical architecture decision record.

Exit criteria:

- Team agrees on data objects and workflow states.
- Design has validated core workflows with target users.
- Engineering can implement without inventing domain rules ad hoc.

#### Phase 1: Enterprise platform foundation

Deliverables:

- Auth and tenancy.
- Facility and user admin.
- Role assignments.
- Object storage.
- Database schema.
- Audit event service.
- Background job infrastructure.
- Ingestion batch/job model.
- Observability and error tracking.

Exit criteria:

- Tenant/facility isolation verified.
- Upload jobs and audit events work end-to-end.
- Admin can set up workspace.

#### Phase 2: Bulk upload and extraction MVP

Deliverables:

- Bulk upload.
- File validation and deduplication.
- Document normalization.
- OCR/page text.
- Classification.
- Core term extraction.
- Evidence links.
- Confidence scoring.
- Review queue.
- Contract repository.

Exit criteria:

- Customer can upload documents and review extracted contract fields.
- Accepted contract records appear in repository.
- Every accepted field has evidence.

#### Phase 3: Renewal command center

Deliverables:

- Renewal profile.
- Notice deadline calculation.
- Owner assignment.
- Alerts and digests.
- Renewal dashboard/calendar.
- Search and saved views.
- Facility/portfolio rollups.

Exit criteria:

- Practice can see upcoming renewals and assigned owners.
- Alerting works with configurable lead times.
- Leadership can view portfolio status.

#### Phase 4: Source connectors

Deliverables:

- Google Drive ingestion.
- Gmail evidence discovery.
- Outlook evidence discovery.
- Incremental sync.
- Source connection admin.
- Connector audit.
- Mapping review for ambiguous source files.

Exit criteria:

- Practice can ingest from Drive without IT support.
- Email discovery agents produce document candidates without committing terms directly.
- Source permissions are scoped and auditable.

#### Phase 5: Rate intelligence

Deliverables:

- Rate schedule extraction.
- Rate line model.
- Fee schedule review.
- Internal comparisons across facilities/payers.
- Missing rate schedule dashboard.
- Opportunity scoring.

Exit criteria:

- Practice can identify which contracts have rate schedules.
- Reviewed rates support internal benchmark comparisons.
- Underpricing opportunities are explainable and evidence-backed.

#### Phase 6: Negotiation copilot

Deliverables:

- Renewal prioritization.
- Renewal risk memo.
- Suggested asks.
- Payer policy research.
- Call prep.
- Draft notice/negotiation language.
- Approval routing.
- Outbound notice package generation.

Exit criteria:

- Renewal workflow produces a sourced, reviewable negotiation package.
- No outbound notice can be sent without approval.
- Drafts include evidence and audit trail.

#### Phase 7: Enterprise extensions

Deliverables:

- SFTP ingestion.
- Optional e-signature.
- Advanced admin controls.
- Data retention policies.
- Advanced exports.
- Portal connectors where appropriate.
- EHR/PMS enrichment exploration.

Exit criteria:

- Enterprise customers with IT support can automate ingestion.
- Signing and outbound workflows remain governed.

## 19. Open questions

| Question | Owner | Blocking? |
| --- | --- | --- |
| What contract types must be supported in V1 beyond payer, vendor, lease, employee, processor? | Product | Yes |
| Which ingestion method should be first after bulk upload: Drive or Gmail/Outlook? | Product/Engineering | Yes |
| What fields are auto-acceptable versus always human-reviewed? | Product/Compliance | Yes |
| What is the minimum golden dataset size before extraction ships? | Product/ML | Yes |
| Should each facility have separate default lead times? | Product | No |
| What export formats are required for operators and counsel? | Product | No |
| What level of support admin access is acceptable for customer success? | Security/Product | Yes |
| How should rate benchmarks be calculated before external benchmark data exists? | Product/Data | No |
| Which cloud storage, OCR, and document AI providers meet compliance needs? | Engineering/Security | Yes |
| What is the first design-partner workflow: upload-first, Drive-first, or renewal-first? | Founder/Product | Yes |

## 20. Recommended next steps

1. Approve this PRD as the starting product scope.
2. Create workflow diagrams for upload, Drive ingestion, extraction review, renewal dashboard, and negotiation prep.
3. Create an ERD from the canonical data model.
4. Create low-fidelity mocks for the 10 core screens.
5. Build a field schema matrix for contract types and extraction fields.
6. Assemble a golden dataset of real/synthetic contracts for evaluation.
7. Define V1 auto-accept vs. review-required policy.
8. Decide the V1 connector order: bulk upload first, Drive second, email discovery third, SFTP later.
9. Produce technical architecture document and ADRs.
10. Only then begin application implementation.

## 21. Bottom line

The right way to begin PracticeX is not “mock everything first” or “start coding the data model immediately.” The right way is to spend focused time on product-domain design, workflow design, canonical data modeling, permission modeling, and review/evidence architecture, then validate the workflows through low-fidelity mocks, then implement the enterprise platform foundation.

The first shippable product should be a trustworthy contract command center: bulk upload, normalize, classify, extract, review, publish, search, assign owners, and track renewals. The second product should extend ingestion through Drive and email discovery. The third product should convert reviewed contract data into rate intelligence and negotiation workflows.

This sequence creates a real enterprise application, not a concept demo.
