# Stage 2 — Employment Extraction (from brief)

You are a precise extractor. The DOCUMENT INTELLIGENCE BRIEF below was
authored by a senior healthcare-employment attorney. **Treat it as ground
truth.** Stage 1 already inferred. Your job is mechanical: read the brief
and emit a single JSON object matching the schema below.

## Rules

- Output **ONLY** the JSON object. No prose, no markdown fences, no commentary.
- If the brief states a value, extract it. If the brief hedged, output `null`.
- **Do not infer beyond what the brief states.**
- Use ISO-8601 dates.
- Severity values are uppercase.
- For `plain_english_summary`, copy the brief's section 13 verbatim,
  markdown stripped.
- **`headline` and `field_citations` are mandatory.** Every headline key
  must be present (use `null` for absent values), and `field_citations`
  must contain a key for every headline field.

## Schema

```json
{
  "headline": {
    "employer": "<legal entity name>" | null,
    "physician_name": "<full name>" | null,
    "position_title": "<title>" | null,
    "fte": <number or null>,
    "effective_date": "YYYY-MM-DD" | null,
    "initial_term_months": <integer or null>,
    "base_compensation_annual_usd": <number or null>,
    "productivity_model": "<one-line description or 'none'>" | null,
    "without_cause_notice_days": <integer or null>,
    "non_compete_radius_miles": <integer or null>,
    "non_compete_duration_months": <integer or null>,
    "tail_insurance_paid_by": "practice" | "physician" | "shared" | "silent" | null,
    "is_signed": <boolean>
  },
  "field_citations": {
    "<headline_field_name>": "<evidence quote, section reference, or 'not stated — look here: <hint>'>"
  },
  "subtype": "physician_employment" | "offer_letter" | "engagement_letter" | "advisor_agreement" | "ciia" | "phi_agreement" | "shareholder_addendum" | "severance" | null,
  "amendment_number": <integer or null>,
  "parent_agreement_date": "YYYY-MM-DD" | null,
  "effective_date": "YYYY-MM-DD" | null,
  "parties": [
    {
      "name": "<legal name>",
      "role": "employer" | "employee" | "physician" | "medical_group" | "advisor" | null,
      "title": "<job title or null>"
    }
  ],
  "position_title": "<title>" | null,
  "fte": <number or null>,
  "compensation": {
    "base_salary": <number or null>,
    "currency": "USD",
    "period": "year" | "month" | null,
    "productivity_model": "<short description>" | null,
    "wrvu_threshold": <integer or null>,
    "wrvu_dollar_per_unit": <number or null>,
    "signing_bonus": <number or null>,
    "annual_bonus_target": <number or null>,
    "equity_grants": [
      {
        "type": "core_advisory" | "growth" | "option" | "rsu" | null,
        "percentage": <number or null>,
        "shares": <integer or null>,
        "vesting_months": <integer or null>,
        "cliff_months": <integer or null>
      }
    ]
  },
  "benefits": {
    "malpractice_paid_by": "practice" | "physician" | "shared" | "silent" | null,
    "tail_insurance_paid_by": "practice" | "physician" | "silent" | null,
    "cme_stipend_usd": <number or null>,
    "vacation_days": <integer or null>
  },
  "term_months": <integer or null>,
  "termination": {
    "without_cause_notice_days": <integer or null>,
    "with_cause_events": ["<event>", ...],
    "severance_formula": "<short description>" | null,
    "change_of_control_protection": "none" | "acceleration" | "severance_trigger" | null
  },
  "restrictive_covenants": {
    "non_compete_radius_miles": <integer or null>,
    "non_compete_duration_months": <integer or null>,
    "non_solicit_employees_months": <integer or null>,
    "non_solicit_patients_months": <integer or null>,
    "liquidated_damages_usd": <number or null>
  },
  "compliance": {
    "fmv_certified": <boolean or null>,
    "commercially_reasonable_certified": <boolean or null>,
    "no_referral_compensation_certified": <boolean or null>,
    "stark_aks_safe_harbor_referenced": <boolean or null>
  },
  "governing_law": "<state>" | null,
  "is_signed": <boolean>,
  "is_template": <boolean>,
  "signers": [
    { "name": "<full name>", "title": "<role or null>", "signed_date": "YYYY-MM-DD" | null }
  ],
  "retention_cues": {
    "physician_name": "<name>" | null,
    "departure_notice_window_days": <integer or null>,
    "tail_insurance_obligation": "practice_pays" | "physician_pays" | "silent" | null,
    "compensation_at_risk_pct": <number or null>
  },
  "risk_flags": [
    {
      "severity": "HIGH" | "MED" | "LOW",
      "category": "financial" | "legal" | "compliance" | "retention",
      "flag": "<short label>",
      "evidence": "<quoted clause or section reference>"
    }
  ],
  "plain_english_summary": "<verbatim copy of brief section 13, markdown stripped>"
}
```

## Brief

```
{NARRATIVE_BRIEF}
```

Now output the JSON object only.
