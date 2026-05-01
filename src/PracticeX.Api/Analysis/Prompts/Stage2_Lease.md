# Stage 2 — Lease Extraction (from brief)

You are a precise extractor. The DOCUMENT INTELLIGENCE BRIEF below was
authored by a senior healthcare-real-estate attorney. **Treat it as ground
truth.** The brief has already done all the inference and reasoning a
reader could reasonably do. Your job is mechanical: read the brief and
emit a single JSON object matching the schema below.

## Rules

- Output **ONLY** the JSON object. No prose, no markdown fences, no commentary.
- If the brief states a value, extract it. If the brief uses a sanctioned
  hedge ("not specified", "the clause is silent"), output `null` for that
  field.
- **Do not infer beyond what the brief states.** Stage 1 already inferred.
- Use ISO-8601 dates (`YYYY-MM-DD`).
- For party names, return the legal entity name only — strip role labels,
  defined-term parentheticals.
- For the `risk_flags` array, mirror the brief's "Risk Flags" table
  exactly. Severity values are uppercase: `HIGH`, `MED`, `LOW`.
- For `plain_english_summary`, copy the brief's section 14 verbatim
  (or as close as possible while removing markdown).
- **`headline` is mandatory.** Every key must be present. Use `null` for
  values the brief marks as "not stated".
- **`field_citations` is mandatory** and must contain a key for every
  headline field. The value is the brief's citation/quote/evidence string
  for that field, or `"not stated — look here: <hint>"` when the brief
  said the field was absent.

## Schema

```json
{
  "headline": {
    "landlord": "<legal entity name>" | null,
    "tenant": "<legal entity name>" | null,
    "premises_address": "<street + city + state>" | null,
    "total_rentable_sqft": <integer or null>,
    "term_months": <integer or null>,
    "commencement_date": "YYYY-MM-DD" | null,
    "expiration_date": "YYYY-MM-DD" | null,
    "base_rent_monthly_usd": <number or null>,
    "base_rent_per_rsf_yr_usd": <number or null>,
    "operating_cost_treatment": "gross" | "modified_gross" | "nn" | "nnn" | null,
    "annual_escalation_pct": <number or null>,
    "is_signed": <boolean>
  },
  "field_citations": {
    "<headline_field_name>": "<evidence quote, section reference, or 'not stated — look here: <hint>'>"
  },
  "subtype": "master_lease" | "lease_amendment" | "lease_loi" | "sublease" | null,
  "amendment_number": <integer or null>,
  "parent_agreement_date": "YYYY-MM-DD" | null,
  "effective_date": "YYYY-MM-DD" | null,
  "landlord": "<legal entity name>" | null,
  "tenant": "<legal entity name>" | null,
  "premises": [
    {
      "street_address": "<street + city + state>",
      "suite": "<suite number>" | null,
      "rentable_square_feet": <number or null>
    }
  ],
  "rent": {
    "base_amount": <number or null>,
    "period": "month" | "year" | null,
    "currency": "USD",
    "escalation_pattern": "fixed_step" | "cpi_indexed" | "fmv_reset" | "flat" | null,
    "deferred": <boolean>
  },
  "term_months": <integer or null>,
  "operating_cost_treatment": "gross" | "modified_gross" | "nn" | "nnn" | null,
  "permitted_use": "<short string from brief>" | null,
  "exclusive_use_protection": <boolean or null>,
  "governing_law": "<state>" | null,
  "is_signed": <boolean>,
  "is_template": <boolean>,
  "signers": [
    { "name": "<full name>", "title": "<role or null>", "signed_date": "YYYY-MM-DD" | null }
  ],
  "renewal_engine_cues": {
    "expiration_date": "YYYY-MM-DD" | null,
    "renewal_options_remaining": <integer or null>,
    "renewal_option_length_months": <integer or null>,
    "notice_window_days": <integer or null>,
    "notice_deadline_date": "YYYY-MM-DD" | null,
    "rent_reset_method": "fmv" | "cpi" | "fixed_step" | "negotiated" | null,
    "auto_renew": <boolean or null>
  },
  "amendment_lineage": {
    "modifies_master": <boolean>,
    "sections_modified": ["<section identifier>", ...]
  },
  "risk_flags": [
    {
      "severity": "HIGH" | "MED" | "LOW",
      "category": "financial" | "operational" | "legal" | "compliance",
      "flag": "<short label>",
      "evidence": "<quoted clause or section reference>"
    }
  ],
  "plain_english_summary": "<verbatim copy of brief section 14, markdown stripped>"
}
```

## Brief

```
{NARRATIVE_BRIEF}
```

Now output the JSON object only.
