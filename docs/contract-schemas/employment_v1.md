# Employment Family Schema v1

## Scope and intent

The employment family covers agreements between a tenant entity and an individual who provides services — staff, contractors, advisors, and the satellite IP/PHI agreements that ride alongside an offer. v1 models five sub-types: `offer_letter`, `engagement_letter`, `advisor_agreement`, `ciia`, `phi_agreement`. These are the five shapes in the Employment roster (samples 6–10) and they share enough structure — parties, term, equity, signatures — to justify a single base schema.

`physician_employment` is reserved for v2 (RVU comp, structured restrictive covenants, call-coverage stipends). Out of scope: separation/severance, equity-only grants without an employment relationship (`corporate_v1`), benefit-plan docs.

Signature detection is upstream. Slice 2 resolves signatures and envelope metadata before this extractor runs.

## Sub-type discriminator

`subtype` (string, required).

| Value | Description |
|---|---|
| `offer_letter` | At-will employment offer; title, start date, comp, optional equity. |
| `engagement_letter` | Scoped contractor / consulting engagement; fee-based, finite. |
| `advisor_agreement` | Advisory relationship; equity-heavy, often pilot-gated. |
| `ciia` | Confidentiality, Inventions Assignment & non-solicit; signed alongside offer. |
| `phi_agreement` | HIPAA Business Associate-style agreement governing PHI handling. |

## Common base

Every employment doc carries these fields.

| Field | Type | Required | Notes |
|---|---|:--:|---|
| `parties[]` | `PartyRecord[]` | yes | Min 2 entries. At least one with `role` matching the company side. |
| `effective_date` | date (ISO-8601) | yes | If blank/`____`, set `is_template=true` and leave null. |
| `term` | `Term` | yes | See Term sub-schema. `open_ended` for at-will offers. |
| `governing_law` | string | yes | ISO state code or jurisdiction name. `[Your State]` → template. |
| `venue` | string | no | Free text (e.g. "Travis County, Texas"). |
| `is_template` | bool | yes | Computed; see placeholder rules. |
| `is_executed` | bool | yes | Computed from `signature_block`. |
| `signature_block[]` | `SignatureRecord[]` | yes | May be empty for templates. |
| `notes` | string | no | Extractor free-text capture for items not yet modeled. |

Source of truth: `parties[]`, in-doc signer rows, and `effective_date` come from the body; envelope metadata (provider, envelope_id, signed_at_utc) flows in from Slice 2 and supersedes in-doc text on conflict.

## Sub-type-specific fields

### offer_letter

| Field | Type | Required |
|---|---|:--:|
| `position_title` | string | yes |
| `reports_to` | string | no |
| `start_date` | date | yes |
| `base_salary` | `Money` | yes |
| `bonus_terms` | string | no |
| `equity_grants[]` | `EquityGrant[]` | no |
| `benefits_summary` | string | no |
| `at_will` | bool | yes (default `true`) |
| `notice_period` | string | no |

### engagement_letter

| Field | Type | Required |
|---|---|:--:|
| `engagement_scope` | text | yes |
| `fees` | `{ amount: decimal, currency: ISO 4217, structure: "hourly"\|"fixed"\|"retainer"\|"milestone" }` | yes |
| `term_length` | string | yes |
| `deliverables[]` | string[] | no |

### advisor_agreement

| Field | Type | Required |
|---|---|:--:|
| `pilot` | `{ duration_days: int, success_criteria?: text }` | no |
| `scope_of_services` | text | yes |
| `equity_grants[]` | `EquityGrant[]` | yes |
| `growth_milestones[]` | `GrowthMilestone[]` | no |

`growth_milestones[]` is `EquityGrant[]` filtered to `type=growth` — same shape, just queryable independently. This captures sample 6's "Growth Grant: 1.00% per $1M Net New ARR, capped 3.00%, pro-rata 0.10% per $100K" via `cap_percentage=0.03` plus a `pro_rata_increment` of `0.001` per `"$100K Net New ARR"`.

### ciia

| Field | Type | Required |
|---|---|:--:|
| `ip_assignment_scope` | text | yes |
| `prior_inventions_schedule[]` | `{ title: string, date: date?, description: text }[]` | no |
| `confidentiality_term` | string | no |
| `post_employment_obligations` | text | no |

### phi_agreement

| Field | Type | Required |
|---|---|:--:|
| `covered_entity` | `PartyRecord` | yes |
| `business_associate` | `PartyRecord` | yes |
| `permitted_uses[]` | text[] | yes |
| `prohibited_uses[]` | text[] | no |
| `safeguards` | text | yes |
| `subcontractor_terms` | text | no |
| `breach_notification` | `{ window_days: int, channel: string }` | yes |
| `term_after_phi_destruction` | string | no |

## Sub-schemas

**PartyRecord** — `id` (uuid), `type` (`person`|`organization`), `name` (req), `role` (e.g. "Employee", "Employer", "Advisor", "Covered Entity"), `title` (persons only), `address` (`{ street, city, state, postal_code, country }`), `email`.

**Term** — `type` (`fixed_months`|`fixed_until`|`open_ended`), `months` (when `fixed_months`), `end_date` (when `fixed_until`).

**Money** — `{ amount: decimal, currency: ISO 4217 (default "USD"), period: "annual"|"monthly"|"per_engagement"|... }`.

**EquityGrant** *(reused by Corporate family)* — `type` (`core_advisory`|`growth`|`option`|`rsu`|`restricted_stock`|`other`), `percentage_of_fully_diluted` (decimal, exclusive with `share_count`), `share_count` (int, exclusive), `vesting` (`VestingTerms`, req), `cap_percentage` (decimal, e.g. `0.03` for sample 6's Growth Grant cap), `pro_rata_increment` (`ProRataIncrement`, for milestone accrual).

**VestingTerms** — `duration_months` (req), `cliff_months` (default 0), `schedule_after_cliff` (`monthly`|`quarterly`|`annual`|`milestone`), `milestone_trigger` (text, when milestone).

**ProRataIncrement** — `{ amount_per_unit: decimal, unit_description: text }` — e.g. `0.001` per `"$100K Net New ARR"`.

**SignatureRecord** — `signer_name`, `signer_title`, `signer_role` (e.g. "Employee", "Employer Authorized Representative"), `signed_at_utc` (null when unsigned), `signature_provider` (`docusign`|`adobe`|`native`|`wet_ink`|`unknown`), `envelope_id` (Docusign), `page_number`.

## Placeholder / template detection

- Required base field whose raw value is `____________`, `[Your State]`, `[Date]`, or empty after trim → `is_template = true`; field stays null.
- No `SignatureRecord` has non-null `signed_at_utc` → `is_executed = false`.
- Both true → emit reason code `manual_review_template_detected`; do **not** promote to a canonical contract record. The artifact stays in candidate review.

## Extraction priority

Drives extractor confidence weighting per sub-type.

| Sub-type | Priority fields |
|---|---|
| `offer_letter` | parties, start_date, position_title, base_salary, equity_grants |
| `advisor_agreement` | parties, effective_date, term, equity_grants, signature_block |
| `ciia` | parties, effective_date, ip_assignment_scope, signature_block |
| `phi_agreement` | covered_entity, business_associate, breach_notification, signature_block |
| `engagement_letter` | parties, effective_date, fees, term_length, signature_block |

## Validation against samples

| # | Path | Sub-type | Key fields to validate |
|---|---|---|---|
| 6 | `HR\Dr.Stephen\Complete_with_Docusign_Synexar_Advisor_DrSte.pdf` | advisor_agreement | parties (Synexar Inc + Dr. Stephen Campbell), effective_date 2026-04-11, term 2 years, equity_grants[0]=core_advisory 1% 24mo/6mo cliff, equity_grants[1]=growth pro-rata 0.10%/$100K cap 3%, governing_law Delaware, envelope_id 59AB8CA8-31CD-8498-82AB-CF429363CD9F |
| 7 | `HR\Shubham\Shubham_Offer_Letter_Synexar_revised.docx` | offer_letter | parties, position_title, start_date, base_salary, signature_block |
| 8 | `HR\Shubham\Synexar_CIIA_Shubham.docx` | ciia | parties, effective_date, ip_assignment_scope, signature_block |
| 9 | `HR\Shubham\Synexar_PHI_Agreement_Shubham.docx` | phi_agreement | covered_entity, business_associate, permitted_uses, signature_block |
| 10 | `HR\Shubham\Shubham_Engagement_Letter_Synexar.docx` | engagement_letter | parties, engagement_scope, fees, term_length, signature_block |

## Versioning

- Version constant: `employment_v1`.
- Mirrored in C# as `EmploymentSchemaV1` (sibling slice). Snake_case JSON via property naming, matching `src/PracticeX.Discovery.Contracts/Manifest.cs`.
- `employment_v2` adds `physician_employment` with RVU comp formula, call-coverage stipends, and structured restrictive covenants.

## Open questions

- `at_will` on `offer_letter` — default `true` (US standard) or require explicit so the extractor never silently asserts a legal posture?
- `phi_agreement.breach_notification.window_days` — default `60` (HIPAA ceiling) when silent, or always require explicit extraction?
- `equity_grants[]` — keep sub-type-scoped (advisor vs offer) or hoist to the base array? Hoisting simplifies Corporate-family reuse; scoping keeps offer-letter equity economically distinct.
- `governing_law` — normalize to ISO 3166-2 state codes at extraction time, or keep verbatim with a separate `_normalized` field?
- `ciia.prior_inventions_schedule[]` — when the doc says "None" or the page is blank, emit an empty array or omit the field? Affects "did the employee disclose prior IP?" reporting.
