# Stage 2 — NDA Extraction (from brief)

You are a precise extractor. The DOCUMENT INTELLIGENCE BRIEF below was
authored by a senior healthcare-transactions attorney. **Treat it as ground
truth.** The brief has already done all the inference and reasoning a
reader could reasonably do. Your job is mechanical: read the brief and
emit a single JSON object matching the schema below.

## Rules

- Output **ONLY** the JSON object. No prose, no markdown fences, no commentary.
- If the brief states a value, extract it. If the brief uses a sanctioned
  hedge, output `null`.
- **Do not infer beyond what the brief states.**
- Use ISO-8601 dates.
- For `risk_flags`, mirror the brief's table exactly. Severity is uppercase.
- For `plain_english_summary`, copy the brief's section 11 verbatim,
  markdown stripped.
- **`headline` and `field_citations` are mandatory.** Every headline key
  must be present (use `null` for absent values), and `field_citations`
  must contain a key for every headline field.

## Schema

```json
{
  "headline": {
    "counterparty_name": "<legal entity or individual>" | null,
    "counterparty_class": "health_system" | "private_equity" | "physician_group" | "vendor" | "payor" | "investor" | "individual" | "unknown" | null,
    "is_mutual": <boolean or null>,
    "effective_date": "YYYY-MM-DD" | null,
    "discussion_term_months": <integer or null>,
    "confidentiality_survival_months": <integer or null>,
    "trade_secret_perpetual": <boolean or null>,
    "permitted_purpose_quote": "<verbatim quote>" | null,
    "acquirer_signal": <boolean or null>,
    "has_standstill": <boolean or null>,
    "has_non_solicitation": <boolean or null>,
    "is_signed": <boolean>
  },
  "field_citations": {
    "<headline_field_name>": "<evidence quote, section reference, or 'not stated — look here: <hint>'>"
  },
  "subtype": "mutual_org" | "unilateral_disclosing" | "unilateral_receiving" | "m_and_a_target" | "recruitment_target" | "joint_venture_exploration" | "payor_negotiation" | "investor_diligence" | null,
  "effective_date": "YYYY-MM-DD" | null,
  "is_mutual": <boolean>,
  "is_template": <boolean>,
  "is_signed": <boolean>,
  "parties": [
    {
      "type": "person" | "organization",
      "name": "<legal name>",
      "role": "disclosing" | "receiving" | "both" | null
    }
  ],
  "permitted_purpose_quote": "<verbatim quote from the brief>" | null,
  "transaction_context": "<brief plain-language statement of the business situation>" | null,
  "term_months": <integer or null>,
  "survival_months": <integer or null>,
  "trade_secret_perpetual": <boolean or null>,
  "non_solicitation": <boolean or null>,
  "non_circumvention": <boolean or null>,
  "standstill": <boolean or null>,
  "non_compete": <boolean or null>,
  "governing_law": "<state>" | null,
  "signers": [
    { "name": "<full name>", "title": "<role or null>", "signed_date": "YYYY-MM-DD" | null }
  ],
  "strategic_cues": {
    "acquirer_signal": <boolean or null>,
    "recruitment_signal": <boolean or null>,
    "payor_signal": <boolean or null>,
    "counterparty_class": "health_system" | "private_equity" | "physician_group" | "vendor" | "payor" | "investor" | "individual" | "unknown" | null,
    "leverage_direction": "practice_advantaged" | "counterparty_advantaged" | "balanced" | null
  },
  "risk_flags": [
    {
      "severity": "HIGH" | "MED" | "LOW",
      "category": "financial" | "operational" | "legal" | "strategic" | "compliance",
      "flag": "<short label>",
      "evidence": "<quoted clause or section reference>"
    }
  ],
  "plain_english_summary": "<verbatim copy of brief section 11, markdown stripped>"
}
```

## Brief

```
{NARRATIVE_BRIEF}
```

Now output the JSON object only.
