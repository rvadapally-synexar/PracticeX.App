# PracticeX — Product Requirements Document

**Product:** Practice Command Center (PCC)
**Company:** PracticeX
**Document type:** Internal — for founders, engineering team, design partner
**Status:** Revision 2, supersedes the original enterprise PRD
**Last updated:** April 2026

---

## 1. Why this document exists

This PRD replaces the original enterprise PRD with a version that reflects the decisions made in our founding strategy discussions. It describes the full product vision, not just the pilot, and it is written for internal use — the founders, the engineering team in India, and the design partner. It assumes the frontend design system and screen specifications live separately in the `FCC_MOCK` handoff bundle, which is treated here as a companion artifact rather than duplicated.

The document is meant to be actionable. It resolves open questions from the original PRD rather than enumerating them. It identifies what the pilot delivers, what the post-pilot product becomes, and what the architectural commitments are that should not be revisited casually. Where decisions are genuinely still open, they are called out explicitly in the final section.

## 2. What PracticeX is

PracticeX is the company. Practice Command Center, or PCC, is the flagship product. The product is an enterprise healthcare contract intelligence platform that turns a practice's disorganized inventory of payer contracts, vendor agreements, leases, employment agreements, and processor agreements into a structured, evidence-backed system of record, with visibility into renewal risk, notice deadlines, and eventually rate benchmarking and negotiation support.

The product begins with independent GI groups, ambulatory surgery centers, and specialty practices, because those are the facilities the founding team has direct access to and domain knowledge in. It is designed to generalize to multi-site specialty practice organizations beyond GI over time, but the early product is deliberately shaped to be excellent for GI-adjacent facilities rather than generic.

The core user is a practice or ASC administrator. The core buyer overlaps significantly with the core user, because these are operator-run businesses where administrators have real budget authority on tools that reduce operational burden. Secondary personas include facility owners who want portfolio visibility, CFOs who want rate intelligence, and counsel who want clause-level evidence and audit trails, but the product's design language is set by the administrator persona first.

The product is positioned as calm, institutional, and expert — the visual language in the handoff bundle uses a clay-court palette and Instrument Serif display type to convey operator seriousness without enterprise software sterility. This is deliberate and affects hiring, sales narrative, and product decisions downstream.

## 3. What problem PCC solves

Healthcare practices of any meaningful size accumulate contracts across inboxes, shared drives, scanned PDFs, vendor portals, local desktops, and counsel folders. At a five-facility group, asking each facility administrator to name the active payer contracts will produce five different answers. Notice windows close quietly. Auto-renewals fire without review. Rate schedules are attached to contracts that no one has opened in three years. When a payer offers a renewal, the practice negotiates without knowing whether its existing rates are above or below market, and without knowing which of its clauses differ from the payer's standard template.

The cost of this problem is operational, financial, and governance-related. Missed notice windows and auto-renewals cost real money. Underpriced payer contracts persist for years because no one surfaces them. Accountability is diffuse because no single person is named as the owner of any given contract. Compliance and audit postures are weak because there is no reliable trail of what was extracted, reviewed, edited, or relied on. And the administrators who would normally fix this are already at capacity running clinical operations.

PCC solves this by becoming the system of record and, eventually, the system of action for practice contracts. In the first product release, that means a trustworthy inventory with extracted fields, evidence links, named owners, and renewal tracking. In later releases, it means rate benchmarking and negotiation support on top of the same contract base.

## 4. Product principles

These principles are not aspirational statements. They are constraints on every feature decision and every architectural choice. If a proposed feature violates one of these, either the feature changes or the principle does — not silently.

**Evidence-first.** Every extracted field in the product must be traceable to a specific location in a specific source document, down to page and bounding box. The UI must expose this traceability through "jump to source clause" affordances. A field without evidence is not shippable.

**Review-first.** AI suggests; humans decide on high-impact facts and outbound actions. The extraction pipeline produces candidate fields with confidence scores. A reviewer accepts, edits, or rejects each candidate. Auto-acceptance is a policy decision that applies only to specific field types above specific confidence thresholds, and that policy must be configurable per tenant.

**Workflow-first, not cabinet-first.** Users manage work — review queues, renewal tasks, acknowledgments, approvals — rather than browsing a file repository. The contracts list exists, but it is not the primary surface. The dashboard and the review queue are.

**Facility-native.** The product assumes multi-facility tenants from day one. Every contract, alert, obligation, and task is scoped to a facility, and every list view can be filtered by facility or rolled up across facilities. Permissions are facility-aware.

**Operator-first.** The product must be usable by a practice administrator without IT sophistication. This means drag-and-drop upload is the default onboarding path. SFTP and enterprise connectors exist for customers that need them but are not the primary experience.

**Canonical and extensible.** PCC maintains its own canonical data model for payers, facilities, contracts, and rates. External identifiers like NPI, NAIC, HIOS, and CPT are linked where they exist but the product does not depend on them for identity. The canonical model is versioned so it can evolve without downstream breakage.

**Typed AI surface.** Every AI operation — classification, extraction, drafting, research — has a typed input, a typed output, a version, an evaluation set, and a trace. Agent-style operations are composed from these typed units, not assembled ad-hoc.

**Audit by default.** Every read of sensitive data, every edit, every export, every review decision, every agent run, and every outbound action is logged in an append-only audit store. Audit data is retained independently of operational data retention rules.

**No silent automation.** Outbound notices, emails to counterparties, e-signature packages, and destructive changes require explicit human approval. The product may prepare, but it does not send on its own.

**Enterprise from day one.** Tenancy, role-based access, encryption at rest and in transit, audit, observability, BAA-compliant infrastructure, and data retention policy are foundational. They are not deferred behind an "enterprise tier" to be built later.

## 5. Users

The product serves several personas. The primary persona drives the product's design language; the secondary personas consume specific surfaces.

The **practice or ASC administrator** is the primary persona, embodied in the design as "Jordan Okafor, ASC Administrator." She runs daily operations for one or more facilities, owns the contract inventory in practice if not in title, receives the renewal alerts, assigns ownership, and is the first person the practice's leadership asks when a contract question surfaces. The product must reduce her cognitive load and give her accountability handles. The dashboard, the review queue, the renewals view, and the contract detail page are primarily for her.

The **facility administrator** at a specific site handles facility-scoped contracts — local vendor agreements, leases, equipment contracts — and participates in review and acknowledgment for contracts that apply to her facility. She sees a facility-scoped version of the same surfaces the practice administrator uses.

The **executive or operator** responsible for multiple facilities uses the portfolio rollups to understand contract exposure across the organization. She does not review individual fields; she looks at aggregated renewal risk, underpriced contracts, and owner coverage.

The **CFO or revenue leader** uses the rate intelligence surfaces to understand payer economics. She looks at benchmarks, variance against median, and annualized opportunity. The rate benchmarking pages are for her, though the administrator will often be the one who brings them to her attention.

The **counsel or legal reviewer** looks at specific clauses and the evidence behind them. She uses the contract detail page to verify that extracted clauses are correctly interpreted, and she uses the audit log to confirm what was done and by whom.

The **implementation specialist** is initially the founding team, and later a PracticeX employee, responsible for onboarding a new practice. She uses bulk upload, source connection configuration, mapping review, and the instrumentation that feeds the implementation cost calculator.

The **PracticeX support or customer success user** monitors ingestion health, helps customers resolve review queue backlogs, and accesses support-safe admin views without seeing contract content she should not see.

## 6. What the pilot delivers

The pilot begins one week from the date this document is finalized. The pilot customer is the design partner facility where the GI physician co-founder practices. The pilot is services-wrapped, which means the customer hands over documents and the founding team does the extraction work inside the product, using the review queue and the extraction pipeline as it exists. The customer experiences value through the deployed product — they log in, upload, and see their contracts appear — but the heavy lifting of initial extraction is done by the team, not by the customer.

This framing matters because it sets the scope of what must exist on day one of the pilot versus what can be built during the pilot. On day one, the customer must be able to log in, create their workspace, add their facilities and users, upload documents, and see those documents appear in a usable organization. Within the first two weeks, the team must be able to run uploaded documents through the extraction pipeline for one or two canonical contract types, review and correct extracted fields in the review queue, and publish accepted contracts to the repository. Within the first four to six weeks, the renewal dashboard must surface upcoming deadlines and assigned owners based on the reviewed contracts. Rate intelligence and negotiation support are explicitly post-pilot, though the data model supports them from the start.

The pilot's deliverables to PracticeX are not just a happy customer. They include:

An **instrumented record of implementation time** across every activity category — upload and organization, classification review, field review by contract type, rate schedule review, counterparty resolution, facility mapping, amendment reconciliation. This instrumentation must be built into the product from day one, not retrofitted, and it becomes the basis for the implementation cost calculator that is itself a commercial deliverable.

A **validated canonical data model** for the first two contract types. The types chosen for the pilot should be payer contracts and vendor contracts, unless the design partner's inventory suggests a better pair. The validation comes from the co-founder and the pilot facility's administrator reviewing the extracted data against their mental models and identifying where the schema is wrong.

A **library of real extraction edge cases** — amendment chains, counterparty aliases, multi-page rate schedules, carve-outs, unusual renewal terms — that feeds both the extraction pipeline improvements and the evaluation dataset that will be used to measure extraction quality going forward.

A **set of reusable playbooks** for implementation: document inventory templates, facility mapping conventions, counterparty normalization rules, extraction review protocols. These become the operational backbone of onboarding the second and third customers.

A **reference customer relationship** with the design partner facility, conditional on the pilot delivering real value. The co-founder's embedded position at the facility de-risks this but does not guarantee it; the product still has to earn the reference.

## 7. What the pilot does not deliver

The pilot is narrow by design. The following are explicitly out of scope for the pilot timeline, though the data model and architecture must not preclude them:

- Rate intelligence and payer benchmarking as user-facing features.
- Negotiation copilot with risk memos, suggested asks, and draft language.
- Email discovery through Gmail or Outlook.
- SFTP ingestion.
- Google Drive ingestion (deferred until after pilot; bulk upload handles the pilot).
- Outbound notice generation.
- E-signature integration.
- EHR or PMS integration.
- Self-service implementation by customers without PracticeX team involvement.

These are not deprioritized forever. They are sequenced after the pilot proves the core loop works.

## 8. The full product vision

Once the pilot validates the core loop, PCC expands along three axes: deeper ingestion, deeper intelligence, and deeper action.

**Deeper ingestion** means moving beyond bulk upload as the primary entry point. Google Drive connection comes first, because most practices already have documents organized in Drive folders and the onboarding friction of "select this folder" is dramatically lower than "upload all of these files." Gmail and Outlook evidence discovery — scanning email for contract attachments and amendments — comes next, but as a candidate-generation tool that feeds the review queue, not as a source of truth that commits to the contract record directly. SFTP ingestion comes later for enterprise customers whose IT teams can drop files on a schedule.

**Deeper intelligence** means moving from "here are your contracts" to "here is what you should do about them." The first layer is rate visibility: extracting rate schedules as structured line items and comparing internal rates across facilities and payers. The second layer is external benchmarking, which requires a data partnership with a source like PayerPrice, MRF/TiC aggregators, or equivalent, and which is a Premium-tier feature. The third layer is opportunity prioritization: sorting renewals not just by date but by economic upside, weighted by contract confidence and renewal risk.

**Deeper action** means supporting the actual work of renewal negotiation. Risk memos synthesize contract terms, benchmark comparisons, and payer-specific context into a document the administrator or CFO can take into a negotiation call. Suggested asks are generated against the contract and benchmark data. Draft notice letters are produced but require explicit approval before sending. Approval routing ensures the right people sign off. Outbound notice delivery, once approved, is logged and tracked. Optional e-signature integration packages approved documents for signature through DocuSign or similar. None of this bypasses human approval for anything that leaves the system.

Across all three axes, the product maintains the same core commitments: evidence behind every claim, human review for high-impact decisions, canonical data that generalizes across tenants, and audit for everything.

## 9. Product surface

The product's surfaces are specified in detail in the `FCC_MOCK` handoff bundle. This section summarizes them for the PRD reader but does not duplicate the specifications; engineering should work from the handoff bundle for UI details.

The product has seven primary views:

The **Command Center dashboard** is the morning-standup surface — active contract count, upcoming renewals, alerts needing attention, obligations this month, recent activity, and a teaser card for Premium rate intelligence when the tenant is on the Basic tier.

The **Contracts repository** is the filterable, sortable list of every contract with columns for ID, counterparty, category, facility, end date, renewal type, annual value, and status.

The **Contract Detail page** is the most-used surface. It presents the document on the left and extracted fields on the right in a split layout, with two-way linking between fields and their source clauses. It includes a negotiation playbook card populated by AI suggestions, and an ownership and activity panel with acknowledgments and a timeline.

The **Extraction Review queue** is where candidate fields from newly ingested contracts are accepted, edited, or rejected. It shows a queue on the left and a review panel on the right with per-field confidence, jump-to-source, and confirm/flag controls.

The **Renewals view** is a calendar and timeline of upcoming deadlines, owner assignments, and notice windows.

The **Rate Visibility / Benchmarks page** is Premium-only and shows payer-level benchmark bars, a variance table, top opportunities, and methodology attribution. On Basic tier, this page renders a locked skeleton with a Premium upgrade prompt.

The **Alerts and Obligations pages** are the full-width versions of the dashboard-embedded lists.

Two additional surfaces complete the product:

The **Upload modal** handles drag-and-drop upload with per-file extraction status and a route to the review queue when a batch is ready.

The **Admin surfaces** for facilities, users, roles, source connections, alert rules, audit log, and security settings are scoped by tenant role and not exposed to the facility-level users.

The product supports theme variants (operator/clay default, clinical, dark) and density variants (comfortable/compact) as first-class UI state, persisted per user. A product tier toggle (basic/premium) gates the Intelligence section of the sidebar and the Rate Visibility page.

Responsive behavior is a first-class requirement. The product must work well on iPad in both portrait and landscape orientations, and must use the full width of any desktop screen without large empty gutters. Tablet portrait transforms the contract detail page's side-by-side layout into a full-width document view with the fields panel accessible as a bottom sheet. This is specified in more detail in the handoff bundle.

Native mobile applications are not on the roadmap. The responsive web application, optionally installed to the iPad home screen as a PWA, is the mobile experience. Capacitor remains an option for future App Store distribution if a specific enterprise customer requires it, but is not part of the current build.

## 10. Data model at a glance

The full PostgreSQL DDL lives in a separate document. This section describes the data model at a narrative level so the PRD reader understands the shape of what the product knows and how.

The data model separates concerns into logical schemas within a single PostgreSQL database. Reference data (payers, contract types, taxonomies) lives in a `ref` schema and is not tenant-scoped. Tenancy, facilities, users, and roles live in an `org` schema. Documents and ingestion artifacts live in a `doc` schema. Contracts, counterparties, clauses, and extraction schemas live in a `contract` schema. Evidence links — the traceability layer from extracted facts back to document locations — live in their own `evidence` schema because they cross-cut other domains and will be referenced by rate lines, clauses, and future entities. Rate schedules and rate lines live in a `rate` schema. Operational workflow entities — renewals, obligations, alerts, tasks — live in a `workflow` schema. Audit, agent runs, tool calls, and evaluation runs live in an `audit` schema.

Key modeling decisions:

**Counterparty** is a first-class entity with aliases. A payer like "Regence BlueShield" may appear under half a dozen variant names across contracts; the model represents this as a canonical counterparty with a list of aliases and a resolution process. External identifiers (NPI, NAIC, HIOS, internal payer codes) are linked when available but are not required for identity. This is intentional and important — there is no universal payer identifier in healthcare comparable to OpenFIGI in finance, and the product must not assume one.

**Contract** is distinct from **ContractVersion**. The contract is the ongoing business relationship with a counterparty; the version is a specific executed document — original, amendment, addendum — that contributes terms. Fields are attached to the contract with provenance linking back to the specific version and evidence that supports them. Amendments that supersede prior terms update the current fields while preserving the prior values in history.

**ExtractionSchema** is versioned. When the schema for payer contracts evolves — fields added, definitions refined, validation rules tightened — old extractions remain valid under their prior schema version, and new extractions use the current version. This allows the schema to evolve without invalidating past work.

**EvidenceLink** supports multi-page evidence. A single field can be backed by evidence that spans several pages — a rate schedule that runs for thirty pages, for example — and the evidence model represents this as a list of page-plus-bbox references rather than a single reference.

**AuditEvent** is append-only. Nothing modifies audit events; corrections appear as compensating events. Retention for audit is managed separately from operational data retention.

**AgentRun** and **ToolCall** capture every AI operation at a fidelity that supports debugging, evaluation, and compliance. Input hashes, output hashes, model version, prompt version, and tool invocations are all recorded.

## 11. Extraction pipeline

The extraction pipeline converts uploaded documents into structured, reviewable contract fields. It runs as a chain of typed operations, each of which is a unit of work with inputs, outputs, version, and evaluation. The pipeline is built in ASP.NET Core, calling LLM APIs directly. Python is not introduced unless a specific capability cannot be achieved in C#, and at the time of this PRD no such capability is identified.

The pipeline stages, in order:

**Ingestion and normalization.** Uploaded files are stored as preserved originals in `DocumentAsset` records. Hashes are computed for deduplication. File validation rejects unsupported or corrupted files with user-readable errors. ZIP archives are expanded while retaining folder path metadata that may hint at facility or counterparty mapping.

**Text extraction and layout.** Documents are processed through Azure Document Intelligence (or equivalent OCR and layout service) to produce page-level text, layout information, and table structures. Scanned PDFs produce OCR confidence scores that are preserved through downstream stages.

**Classification.** A classifier determines the document type — payer contract, vendor contract, lease, employee agreement, processor agreement, amendment, fee schedule, or other — and the facility and counterparty hints. Classification produces a `DocumentCandidate` record, which is the pre-canonical representation of what the document is about to become.

**Field extraction.** For each classified document, the appropriate `ExtractionSchema` is selected, and fields are extracted using structured LLM prompts with schema-conformant output. Each extracted field produces a candidate value, a confidence score, and evidence pointers to the page and bounding box of the source text.

**Rate schedule extraction.** When a document includes or is a rate schedule, rate lines are extracted as structured records with CPT or HCPCS code, modifier, description, allowed amount or percent-of-Medicare, and per-line evidence. This is a harder extraction problem than field extraction and is expected to require iteration on the extraction prompts and post-processing validation over the pilot period.

**Counterparty resolution.** Extracted counterparty names are compared against the existing canonical counterparty list using both exact and fuzzy matching. New counterparties are proposed for tenant approval; existing ones are linked. This is a place where human review is load-bearing and where the UI for merging suspected duplicates must be well-designed.

**Review queue population.** Extracted fields and candidate classifications flow into the review queue with their confidence scores, evidence links, and flag reasons. The review queue is where the pipeline's output becomes canonical contract data.

**Publish to repository.** Approved fields update the canonical `Contract` record, link to `ContractVersion` and `EvidenceLink`, and trigger downstream computation like renewal profile calculation.

The pipeline is instrumented throughout. Every extraction operation writes an `AgentRun` record with the model version, prompt version, input hash, output hash, duration, and any errors. Every tool invocation (document text fetch, schema validation, entity lookup) writes a `ToolCall` record. This instrumentation supports the evaluation harness, which replays past extractions against current pipeline versions to detect regressions.

## 12. Implementation cost calculator

The implementation cost calculator is a deliverable of the pilot, not an afterthought. It exists because healthcare practice administrators and CFOs are accustomed to implementation costs being opaque, and because a vendor that can produce a transparent, instrumented, evidence-based cost estimate wins procurement battles that incumbents with relationships would otherwise win.

The calculator takes inputs like number of facilities, estimated document count, contract type mix, source system complexity, and produces an estimate of implementation hours and cost. It is built against coefficients measured during the pilot and future implementations, not against assumptions.

To produce the coefficients, the pilot must instrument every implementation activity with time tracking by category. The categories include initial upload and organization, first-pass classification review, per-contract-type field extraction review, rate schedule review, counterparty resolution, facility mapping, amendment reconciliation, and edge-case handling. Every `ReviewTask` and `AuditEvent` in the data model captures who did it and how long it took, and by the end of the pilot a query against the audit schema produces aggregate time-per-activity that feeds the calculator.

The calculator itself is a product surface, not just an internal tool. Prospective customers can use it during sales conversations to estimate their own implementation cost. This transparency is a sales advantage and should be treated as one.

## 13. Architecture at a glance

The architecture is specified in detail in the backend architecture document. This section summarizes the commitments.

**Frontend:** React with TypeScript, Next.js App Router or Vite with React Router (final call in backend architecture doc), Tailwind CSS, Radix UI primitives, TanStack Query for server state, TanStack Table for data grids, React Hook Form with Zod for forms, react-pdf for document rendering with custom evidence overlay logic. Component library and design tokens per the `FCC_MOCK` handoff bundle. Monorepo with `apps/command-center` and `packages/design-system` as specified in the handoff.

**Backend:** ASP.NET Core Web API, modular monolith with vertical slice architecture, Entity Framework Core for PostgreSQL access, Hangfire or equivalent for background jobs, MediatR for command and query handlers. Snake case throughout the database, PascalCase in C#, with EF Core naming convention translation.

**Database:** PostgreSQL with schema separation (`ref`, `org`, `doc`, `contract`, `evidence`, `rate`, `workflow`, `audit`). Snake case throughout. Row-level security for tenant isolation. Audit tables are append-only and retained independently.

**AI providers:** Open question. Azure OpenAI under existing Azure BAA is the default if GPT-4 class models are sufficient for extraction quality. If Claude is preferred for long-context contract extraction, AWS Bedrock under a Bedrock BAA becomes a second cloud dependency. Resolution required before extraction pipeline work begins in earnest. See Open Questions section.

**Document AI:** Azure Document Intelligence for OCR and layout, under the existing Azure BAA. Alternatives evaluated only if quality proves insufficient.

**Infrastructure:** Azure for compute, database, and storage, under existing BAA. Cloudflare in front for DNS, WAF, and DDoS protection. GitHub Enterprise for source control and CI/CD. Deployment target is a single region initially with multi-region capability designed into the architecture.

**Identity:** OIDC-based authentication, likely Auth0 or Azure AD B2C. Role-based access with facility-scoped permissions. MFA required for administrative roles.

**Observability:** OpenTelemetry for tracing, Application Insights or equivalent for metrics and logs. Every request traced end-to-end, including into agent runs and tool calls.

**Security:** Encryption at rest via Azure-managed keys. TLS everywhere. Secrets in Azure Key Vault. BAA with all AI providers and subprocessors. HIPAA-eligible configuration across all services. Penetration testing before general availability.

## 14. Team and execution

The team is three engineers: the founder as architect and lead, a senior engineer based in India, and a QA engineer based in India. The co-founder is a practicing GI physician who facilitates the pilot from inside the facility and is resourceful on both clinical and product sides.

The execution model accommodates the timezone split. The founder writes interfaces, initial implementations, and architectural boundaries during his working hours. The senior engineer extends, tests, and completes during his working hours. The QA engineer validates against the pilot's real documents and workflows. Because most code is LLM-generated and reviewed rather than written from scratch, the paradigm shift to React and the maintenance of a new stack alongside Synixar are manageable, but require that the review discipline stay strong.

Code review is not optional. Every PR is reviewed before merge, even when generated by LLM tools. This is not about trusting the tools less; it is about maintaining shared understanding across the team.

A weekly rhythm with the design partner is mandatory and unmovable. A scheduled session, same time each week, where the product is demonstrated, friction points are surfaced, and two concrete changes are committed to for the following week. This rhythm is more important than any individual architectural decision.

## 15. What success looks like

At the end of the pilot — approximately twelve weeks after kickoff — success is defined by several concrete outcomes:

The pilot facility's contract inventory is substantially ingested, extracted, reviewed, and published, with all high-value contracts represented in the canonical repository with owners assigned and renewal profiles calculated.

The facility administrator uses PCC as her canonical reference for contract questions and renewal tracking, not as a side system she remembers to check. This is the single most important signal; without it, nothing else matters.

The co-founder and the facility administrator can articulate, from their own experience, three to five specific ways the product reduced operational burden, and they are willing to say those things to prospective second customers.

The implementation cost calculator is populated with real coefficients from measured pilot activity, and is usable in the first sales conversations with the second and third prospects.

The canonical data model has survived contact with real documents and has been revised where necessary based on what the documents actually contained. The revisions are bounded; the fundamental shape of the model — tenant/facility/contract/version/field/evidence — remains intact. If the fundamental shape had to change, the pre-pilot design work failed.

The extraction pipeline achieves acceptable quality on the primary contract types, defined as: for payer contracts, effective date and termination date and notice window and auto-renewal terms extracted correctly on at least 90% of real pilot documents with evidence links; for vendor contracts, the same core fields at the same threshold. Rate schedule extraction is held to a looser standard during the pilot because it is genuinely harder and is expected to require iteration.

The team's velocity and coordination pattern has stabilized, with a clear working rhythm between the founder and the engineers in India.

PracticeX has a reference customer relationship with the pilot facility, conditional on the above, and has the materials it needs — case study, implementation timeline, cost model, product demo on real data — to start the second customer conversation.

## 16. Open questions

A small number of decisions remain genuinely open and must be resolved in the next one to two weeks. Leaving them open longer will cause rework.

**AI provider for extraction.** Default is Azure OpenAI under existing Azure BAA. Alternative is Claude via AWS Bedrock under a Bedrock BAA, which adds a second cloud dependency but may produce better extraction quality on long contract documents. Resolution required before extraction pipeline work begins. Decision owner: founder, in consultation with a small extraction quality test on representative pilot contracts.

**Next.js vs Vite+Router for the frontend.** Both are viable; the handoff bundle permits either. Next.js App Router gives server rendering, API routes, and deployment conveniences but locks in some Vercel-flavored patterns. Vite + React Router is simpler, more framework-minimal, and maps more directly to a traditional SPA + API backend. Recommendation in the backend architecture document: Vite + React Router, because the API backend is ASP.NET Core and server-side rendering is not needed for an authenticated enterprise app. Decision owner: founder, can defer up to one week.

**Contract types for the pilot.** Default is payer contracts and vendor contracts. The design partner's actual inventory may suggest a different pair, for example payer contracts and leases if the facility has significant real estate commitments. Decision owner: founder and co-founder, within the first few days of pilot kickoff.

**Auto-accept policy for fields.** What confidence threshold and field types are eligible for auto-acceptance without human review. Default is conservative: no auto-acceptance for the pilot, all fields reviewed. This is revisited after sufficient pilot data exists to calibrate thresholds. Decision owner: founder and co-founder, reviewed at week 4 and week 8 of the pilot.

**Rate schedule data partner.** For Premium tier benchmarking, a data partnership with PayerPrice, an MRF/TiC aggregator, or similar is required. Not needed for pilot; needed before Premium tier launch. Decision owner: founder, target resolution within two months post-pilot.

**Support admin access policy.** What level of tenant data a PracticeX support or customer success user can access, and how that access is audited. Must be resolved before any support-tier access is granted. Decision owner: founder and security review, within the first four weeks.

## 17. What this document does not cover

The frontend screen specifications, design tokens, component inventory, and UI behavior are specified in the `FCC_MOCK` handoff bundle and should be worked from directly, not paraphrased here.

The backend architecture — API design, project structure, extraction service internals, background job orchestration, observability, deployment — is specified in the backend architecture document.

The PostgreSQL DDL — full table definitions, indexes, constraints, and migration strategy — is specified in the data model document.

The extraction schema definitions for specific contract types — field lists, validation rules, prompt templates — are specified in the extraction schema document, which is produced collaboratively with the co-founder after the pilot inventory is inspected.

The operational playbook for onboarding the pilot customer — kickoff checklist, week-by-week plan, communication rhythm, success metrics — is specified in the pilot playbook document.

These documents together form the complete specification for PracticeX. This PRD is the anchor from which they descend.
