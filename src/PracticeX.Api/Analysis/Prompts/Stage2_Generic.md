# Stage 2 — Generic Contract Extraction (from brief)

You are a precise extractor. The DOCUMENT INTELLIGENCE BRIEF below was
authored by a senior healthcare-transactions attorney for a contract that
did not fit a known family template. **Treat the brief as ground truth.**
Stage 1 already inferred. Your job is mechanical: read the brief and
emit a single JSON object matching the schema below.

## Rules

- Output **ONLY** the JSON object. No prose, no markdown fences, no commentary.
- If the brief states a value, extract it. If the brief hedged, output `null`.
- **Do not infer beyond what the brief states.**
- Use ISO-8601 dates.
- Severity values are uppercase.
- For `plain_english_summary`, copy the brief's section 9 verbatim,
  markdown stripped.

## Schema

```json
{
  "document_type": "<short description from brief>",
  "effective_date": "YYYY-MM-DD" | null,
  "parties": [
    { "name": "<legal name>", "role": "<role>" }
  ],
  "subject_matter": "<short description from brief>" | null,
  "key_economic_terms": [
    { "term": "<short label>", "amount_usd": <number or null>, "period": "<period or null>", "description": "<one-sentence description>" }
  ],
  "term_months": <integer or null>,
  "termination_notice_days": <integer or null>,
  "restrictive_covenants_present": <boolean>,
  "hipaa_business_associate": <boolean or null>,
  "indemnity": "mutual" | "one_way" | "silent" | null,
  "liability_cap_usd": <number or null>,
  "governing_law": "<state>" | null,
  "is_signed": <boolean>,
  "is_template": <boolean>,
  "signers": [
    { "name": "<full name>", "title": "<role or null>", "signed_date": "YYYY-MM-DD" | null }
  ],
  "risk_flags": [
    {
      "severity": "HIGH" | "MED" | "LOW",
      "category": "financial" | "legal" | "compliance" | "operational",
      "flag": "<short label>",
      "evidence": "<quoted clause or section reference>"
    }
  ],
  "plain_english_summary": "<verbatim copy of brief section 9, markdown stripped>"
}
```

## Brief

```
{NARRATIVE_BRIEF}
```

Now output the JSON object only.
