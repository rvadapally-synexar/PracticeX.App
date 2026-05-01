# Canonical Headline Fields — per Contract Family

The single source of truth for what every Stage-1 brief and every Stage-2 JSON
**must** populate. The product principle:

> Every contract has a small, opinionated set of fields that the practice
> owner needs to know first. Those fields appear at the **top** of the
> Extracted Fields panel as a "headline" view. They are extracted under
> a hard contract: every field is either a real value or an explicit
> `null` with a `field_citation` saying where to look. They are NEVER
> silently omitted.

Risk flags, narrative scaffolding, and family-specific cues live below the
headline. Headline is the dense, decision-grade summary.

---

## Cross-family rules

1. **Every headline field is either a real value or `null` with a citation.**
   Stage-1 prompt tells the LLM: *"If absent from the source, write `not
   stated in this document` AND a `look here` hint."* Stage-2 mirrors this
   with `null` value + non-null `field_citations[field]`.

2. **Computed/derived values are allowed** when the source provides
   the inputs. E.g., `base_rent_monthly_usd` may be computed from
   `$/RSF × RSF / 12` if the source only states `$/RSF`. The citation
   string flags the computation: *"computed from $16.83/RSF × 16,806 RSF / 12"*.

3. **Stage-2 JSON shape:**
   ```json
   {
     "headline": { "field_a": <value or null>, ... },
     "field_citations": { "field_a": "evidence string or 'not stated'", ... },
     ... rest of family-specific fields, risk_flags, etc.
   }
   ```

4. **UI contract:** The Extracted Fields tab leads with a `headline-grid`
   of labeled cards. Null fields render as `— not stated` with the
   citation as a tooltip / italic subline. Risk flags collapse to a count
   badge by default.

---

## Lease family (master, amendment, LOI, sublease)

12 canonical fields:

| # | Field | Type | Notes |
|---|---|---|---|
| 1 | `landlord` | string | full legal entity name |
| 2 | `tenant` | string | full legal entity name |
| 3 | `premises_address` | string | street + city + state, full |
| 4 | `total_rentable_sqft` | integer | sum across listed suites |
| 5 | `term_months` | integer | initial term length |
| 6 | `commencement_date` | YYYY-MM-DD | when term begins |
| 7 | `expiration_date` | YYYY-MM-DD | when term ends |
| 8 | `base_rent_monthly_usd` | number | computed from $/RSF if needed |
| 9 | `base_rent_per_rsf_yr_usd` | number | annual $/RSF |
| 10 | `operating_cost_treatment` | enum | `gross` / `modified_gross` / `nn` / `nnn` |
| 11 | `annual_escalation_pct` | number | e.g., 2.5 for 2.5%/year |
| 12 | `is_signed` | boolean | true if signature block executed |

---

## NDA family

12 canonical fields:

| # | Field | Type | Notes |
|---|---|---|---|
| 1 | `counterparty_name` | string | other party's full legal name |
| 2 | `counterparty_class` | enum | `health_system` / `private_equity` / `physician_group` / `vendor` / `payor` / `investor` / `individual` / `unknown` |
| 3 | `is_mutual` | boolean | both parties disclose? |
| 4 | `effective_date` | YYYY-MM-DD | when obligations begin |
| 5 | `discussion_term_months` | integer | how long the discussion period runs |
| 6 | `confidentiality_survival_months` | integer | how long obligations survive after termination |
| 7 | `trade_secret_perpetual` | boolean | trade-secret obligations forever? |
| 8 | `permitted_purpose_quote` | string | verbatim quote of operative clause |
| 9 | `acquirer_signal` | boolean | derived: true if M&A subtype + acquirer-language purpose |
| 10 | `has_standstill` | boolean | restricts share acquisition? |
| 11 | `has_non_solicitation` | boolean | can't poach employees? |
| 12 | `is_signed` | boolean | true if signature block executed |

---

## Employment family (physician, advisor, engagement, CIIA)

12 canonical fields:

| # | Field | Type | Notes |
|---|---|---|---|
| 1 | `employer` | string | legal entity name |
| 2 | `physician_name` | string | individual's full name |
| 3 | `position_title` | string | e.g., "Gastroenterologist" |
| 4 | `fte` | number | 1.0 = full-time, 0.5 = half-time |
| 5 | `effective_date` | YYYY-MM-DD | start of employment |
| 6 | `initial_term_months` | integer | e.g., 36 for 3-year term |
| 7 | `base_compensation_annual_usd` | number | annualized base salary |
| 8 | `productivity_model` | string | one-line description (e.g., "$50/wRVU above 6,500 threshold") or `"none"` |
| 9 | `without_cause_notice_days` | integer | days either party must give |
| 10 | `non_compete_radius_miles` | integer | physician restriction radius |
| 11 | `non_compete_duration_months` | integer | post-termination duration |
| 12 | `tail_insurance_paid_by` | enum | `practice` / `physician` / `shared` / `silent` |

---

## Call Coverage family

12 canonical fields:

| # | Field | Type | Notes |
|---|---|---|---|
| 1 | `covering_group` | string | medical group providing coverage |
| 2 | `covered_facility` | string | hospital / facility receiving coverage |
| 3 | `coverage_specialty` | string | e.g., "gastroenterology" |
| 4 | `effective_date` | YYYY-MM-DD | when coverage begins |
| 5 | `initial_term_months` | integer | e.g., 24 |
| 6 | `stipend_basis` | enum | `per_shift` / `per_day` / `per_week` / `monthly_retainer` / `hourly` |
| 7 | `stipend_amount_usd` | number | dollar amount per the basis |
| 8 | `coverage_schedule_summary` | string | one-line summary (e.g., "weekend GI call, Fri 5pm – Mon 7am") |
| 9 | `response_time_phone_minutes` | integer | required phone response |
| 10 | `without_cause_notice_days` | integer | termination notice |
| 11 | `malpractice_provided_by` | enum | `covering_group` / `covered_facility` / `shared` / `silent` |
| 12 | `fmv_certified` | boolean | explicit FMV certification language present |

---

## Generic family (vendor, processor, bylaws, JV, miscellaneous)

11 canonical fields:

| # | Field | Type | Notes |
|---|---|---|---|
| 1 | `document_type` | string | plain-language type, e.g., "vendor services agreement" |
| 2 | `counterparty_name` | string | other party's legal entity |
| 3 | `effective_date` | YYYY-MM-DD | when in force |
| 4 | `initial_term_months` | integer | length of obligation |
| 5 | `without_cause_notice_days` | integer | termination notice required |
| 6 | `annual_money_flow_usd` | number | typical annual payment if any |
| 7 | `payment_direction` | enum | `practice_pays` / `practice_receives` / `none` |
| 8 | `subject_matter_summary` | string | one-sentence what-this-governs |
| 9 | `is_baa` | boolean | is it (also) a HIPAA Business Associate Agreement? |
| 10 | `liability_cap_usd` | number | total liability cap if stated |
| 11 | `is_signed` | boolean | execution status |

---

## Implementation notes

- **Stage-1 prompts**: insert `## 0 — CANONICAL HEADLINE` as the very
  first output section, BEFORE `## 01 — Document Header`. The narrative
  scaffold below remains unchanged.
- **Stage-2 prompts**: schema gets `headline` and `field_citations` as
  the first two top-level keys. Existing fields stay below for back-compat.
- **UI**: `HeadlineGrid` component renders each field as a labeled card,
  with `— not stated` + citation tooltip for nulls. Currency/area/% values
  get formatting (`$23,570/mo`, `16,806 RSF`, `2.5%`).
