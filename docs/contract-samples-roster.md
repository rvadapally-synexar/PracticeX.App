# Contract Samples Roster

The schema-design working set. Each contract family (NDA / Employment / Corporate) gets one schema doc + extractor implementation, validated against the samples below. **Sample files live on the developer's local disk and are not stored in the repo** — they contain real PII (signatures, addresses, SSN-like identifiers, equity allocations). The roster references them by absolute path; everything we share publicly is the derived schema, not the source documents.

When you add a contract type, copy the table row template at the bottom and run the schema-discovery cycle described in `docs/desktop-discovery-agent.md`.

---

## NDA family

5 samples. Covers bilateral / mutual / template variants.

| # | Path (under `C:\Users\harek\SYNEXAR INC\`) | Subtype | Format | Signed |
|---|---|---|---|---|
| 1 | `NDAs\Synexar__NDA_Dr. Akerman.docx` | bilateral_individual | DOCX | yes |
| 2 | `NDAs\Synexar_GIQuIC_Mutual_NDA_v2.docx` | mutual_org | DOCX | maybe |
| 3 | `NDAs\templates\Synexar_NDA_Investor.docx` | investor_template | DOCX | unsigned template |
| 4 | `NDAs\templates\Synexar_NDA_Physician_Advisor.docx` | advisor_template | DOCX | unsigned template |
| 5 | `NDAs\templates\Synexar_NDA_Demo_Participant.docx` | demo_participant_template | DOCX | unsigned template |

**Common shape (target schema for `nda_v1`):**
- `parties[]` — disclosing party + receiving party (both can be person or org; need person/org type discriminator on each)
- `effective_date`
- `term_months` or `term_until`
- `permitted_purpose` — short string ("evaluating partnership", "advisor onboarding", etc.)
- `confidential_information_definition` — long text
- `exclusions[]` — typical carve-outs (already in public domain, independently developed, etc.)
- `governing_law` — jurisdiction string
- `is_mutual` — bool (sample 2 is mutual; samples 1, 3, 4, 5 are typically one-way)
- `is_template` — bool (true for samples 3–5; effective_date and parties are blank)
- `signature_block[]`

---

## Employment family

5 samples. Includes the gold Docusign sample for Slice 2 validation.

| # | Path | Subtype | Format | Signed |
|---|---|---|---|---|
| 6 | `HR\Dr.Stephen\Complete_with_Docusign_Synexar_Advisor_DrSte.pdf` ⭐ | advisor_agreement | PDF | **Docusign envelope `59AB8CA8-31CD-8498-82AB-CF429363CD9F`** |
| 7 | `HR\Shubham\Shubham_Offer_Letter_Synexar_revised.docx` | offer_letter | DOCX | likely yes |
| 8 | `HR\Shubham\Synexar_CIIA_Shubham.docx` | ciia (IP assignment) | DOCX | likely yes |
| 9 | `HR\Shubham\Synexar_PHI_Agreement_Shubham.docx` | phi_agreement | DOCX | likely yes |
| 10 | `HR\Shubham\Shubham_Engagement_Letter_Synexar.docx` | engagement_letter | DOCX | likely yes |

**Sample 6 specifics (gold sample, ground truth for Docusign + structured advisor agreements):**
- 12 pages including Docusign certificate page
- Parties: Synexar, INC. (Delaware corp, 5900 Balcones Drive Ste 100, Austin TX 78731) ↔ Dr. Stephen Campbell, M.D. (4060 Beacon Square Blvd Apt 340, Plano TX 75075; sidifen3@gmail.com)
- Effective Date 2026-04-11; Term 2 years
- **Equity grants** (the structurally interesting part):
  - Core Advisory Grant: 1.00% of fully diluted, 24-month vest with 6-month cliff
  - Growth Grant: 1.00% per $1M Net New ARR, capped at 3.00%, pro-rata 0.10% per $100K
- Pilot Period: 60 days
- Governing law: Delaware
- Signers: Raghuram Vadapally (CEO) + Dr. Stephen Campbell
- Section structure: Purpose → Scope → Term → Compensation → Equity Grants → Vesting Mechanics → Pilot → Confidentiality → Termination → Notices → Governing Law → Signatures → Exhibit A (Compliance Guidelines) → Exhibit B (Pilot Completion Certificate template)

**Common shape (target schema for `employment_v1`):**
- `parties[]` — employer + employee/contractor with role/title/address/email
- `subtype` discriminator: `offer_letter` | `engagement_letter` | `advisor_agreement` | `ciia` | `phi_agreement` | `physician_employment`
- `effective_date`, `term`, `end_date_or_open_ended`
- `position_title`, `reports_to`
- `compensation` — base + bonus + comp formula (free text for v1, structured for v2)
- `equity_grants[]` — type (core/growth/option/RSU), percentage_or_count, vesting_terms (years, cliff_months, schedule_after_cliff, milestone_trigger)
- `notice_period`
- `restrictive_covenants` — non-compete / non-solicit / confidentiality (free text v1, structured v2)
- `governing_law`
- `is_at_will` — bool
- `signature_block[]` — esign provider, envelope_id, signer_name + title + date

**Schema-design note:** Equity grants are the structurally rich part — model them as a sub-schema reused by both `employment.equity_grants` and `corporate.share_authorizations` rather than duplicating the vesting object.

---

## Corporate / Foundation family

6 samples. Most varied family — covers governance, equity, IRS filings.

| # | Path | Subtype | Format | Signed |
|---|---|---|---|---|
| 11 | `FoundationDocs\starting_docs\Document Filing  - Synexar - Delaware.pdf` | filing_receipt | PDF | filed (state) |
| 12 | `FoundationDocs\starting_docs\Synexar Equity Plan Adoption Resolutions - 11_25_2025.pdf` | board_consent (equity_plan_adoption) | PDF | electronic, both directors |
| 13 | `FoundationDocs\starting_docs\Synexar_Founder_Agreement.pdf` | founder_agreement | PDF | unsigned (placeholders blank) |
| 14 | `FoundationDocs\starting_docs\Synexar, Inc. — Founders Charter.pdf` | founders_charter (non-binding) | PDF | unsigned |
| 15 | `FoundationDocs\Form 15620 - 83B.pdf` | section_83b_election | PDF (AcroForm) | yes (digital signature timestamp) |
| 16 | `Carta\CapTable\Synexar_Cap_Table.xlsx` | cap_table (data) | XLSX | n/a |

**Sample 11 — Delaware filing receipt:**
- 2 pages. Service Request 20254536349. Submitted 2025-11-12 by `rvadapally@synexar.ai`. Priority "24 Hour Service".
- **Schema note from research:** This is a *transactional receipt*, not a constitutive document. Model as a separate `evidence_of_filing` artifact linked to the corporate-formation record (`service_request_number`, `submitted_at`, `submitter`, `priority`, `payment_method_last4`). Don't merge into the certificate-of-incorporation schema.

**Sample 12 — Equity Plan Adoption Resolutions:**
- 3 pages. Synexar 2025 Equity Incentive Plan, 2,000,000 shares Common reserved (= 20% pool post-plan, 10M fully diluted). 10-year plan term. DGCL §141(f) unanimous written consent.
- Signers: Raghuram Vadapally (Director, 2025-11-25 17:44 PST) + Ashutosh Gupta (Director, same timestamp).
- Referenced exhibits: A (plan), B (RSA template), C (Option template), D (Exercise template).
- **Schema note:** Need `resolutions[]` (each with type+text), `referenced_exhibits[]`, `share_authorizations[]` (class, count, plan), `consent_type='unanimous_written'`, `directors[]` with timestamped electronic signatures. Repeating numeric data → structured `cap_summary` block, not freeform.

**Sample 13 — Founder Agreement:**
- 17 pages. Two co-founders (Raghuram Vadapally CEO/Technical, Dr. Ashutosh Gupta CMO/Medical). 40/40/20 equity split. 4-year vest, 1-year cliff (25%), monthly 1/48 thereafter. References RSPA + PIIA.
- **Critical: `effective_date` is "___________________" (blank) and governing law is "[Your State]" — this is a partially-completed template, not an executed agreement.**
- **Schema note:** Founder schema needs `founders[]` with role/title/equity_pct, normalized `vesting_terms` (years, cliff_months, schedule_after_cliff), `major_decisions[]`, `governance_rules` (deadlock, decision authority by domain). Must support `is_template`/`is_executed` flag and **placeholder detection** — extractor should flag missing required values rather than ingest blanks as data.

**Sample 14 — Founders Charter:**
- 3 pages. Non-binding statement of roles + equity expectations. 4M shares each (8M issued, 2M unissued). 4-year vest, 1-year cliff. Explicitly labeled "non-binding". Signature lines blank.
- **Schema note:** Soft-governance sibling of the founder agreement. Schema needs `binding: false` flag + `document_type` discriminator that distinguishes binding agreements from charters/MOUs. Reuses founder + share-allocation sub-schemas — define those as shared structures, not per-family duplicates.

**Sample 15 — IRS Form 15620 / 83(b) Election:**
- 4 pages (1 form + 3 instructions). Fixed AcroForm with numbered boxes 1–9. Taxpayer Raghuram Vadapally (TIN 049-98-0888). 4,000,000 shares of Synexar Inc. transferred 2025-11-25. FMV $0.00, gross income $0.00. Service recipient Synexar Inc. (EIN 41-2773035). Signature timestamp string "RAGHURAM VADAPALLY 2025.11.26 02:43:14 +0000".
- **Schema note:** Fixed-shape IRS AcroForm. Schema is flat field-keyed (`box1_taxpayer`, `box6_fmv_total`, etc.) with strict types (TIN regex, ISO date). Link to parent equity-grant via `subject_property_ref` (the RSPA / Founder shares). Treat instruction pages as boilerplate to ignore. Includes digital-signature timestamp string that needs parsing into a normalized `signed_at_utc`.

**Sample 16 — Cap Table:**
- XLSX. Tracked as data, not a contract. The corporate family's contracts (RSPAs, option grants, board consents) reference cap-table positions; the cap table itself is an output / system-of-record sibling, not an agreement to be extracted.

**Common shape (target schema for `corporate_v1`):**

The corporate family is heterogeneous. Subtype discriminator drives which fields apply:

| subtype | required fields |
|---|---|
| `certificate_of_incorporation` | entity_name, jurisdiction, registered_agent, formation_date, authorized_shares[], filing_evidence_ref |
| `filing_receipt` | service_request_number, submitted_at, submitter, priority, related_filing_ref |
| `board_consent` | resolutions[], consent_type, signers[], referenced_exhibits[], share_authorizations[]? |
| `founder_agreement` | founders[], equity_split, vesting_terms, governance_rules, binding=true |
| `founders_charter` | founders[], binding=false |
| `stock_purchase_agreement` (RSPA) | parties[], shares, price_per_share, vesting_terms, repurchase_rights |
| `section_83b_election` | taxpayer, tin, property_description, transfer_date, fmv_total, parent_equity_ref |
| `ein_letter` | entity_name, ein, issued_date |

---

## Workflow per family (when we add the schema + extractor)

1. **Read** the relevant samples in this roster + any new ones the user provides.
2. **Author** `docs/contract-schemas/<family>_v1.md` with the field list, types, required/optional, validation rules, placeholder-detection rules.
3. **User reviews** the schema doc — strikes fields, adds fields, corrects types.
4. **Implement** `PracticeX.Discovery/Schemas/<Family>SchemaV1.cs` (the C# typed mirror) and `PracticeX.Discovery/FieldExtraction/<Family>Extractor.cs`.
5. **Validate** — write a sample-validation harness that runs the extractor against each sample and prints a per-sample field grid for manual spot-check.
6. **Iterate** until extraction quality is acceptable for the demo.
7. **Ship.**

This is the same workflow we'll run at every customer site.

---

## Adding a new sample

```
| #  | path-under-SYNEXAR-INC                                 | family   | subtype       | format | signed     |
| 17 | <path>                                                 | <family> | <subtype>     | <fmt>  | <yes|no|template> |
```

Add a notes block below the table for any structurally interesting observations.
