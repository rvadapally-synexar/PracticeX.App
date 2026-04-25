import { Button, Card, ConfidenceBar } from '@practicex/design-system';

const queue = [
  { id: 'C-00212', name: 'Regence BlueShield — Amendment #3', type: 'Payer', age: '2h ago', confidence: 72, flagged: 3 },
  { id: 'V-00451', name: 'Olympus Scope Service — 2026 Renewal', type: 'Vendor', age: '5h ago', confidence: 86, flagged: 1 },
  { id: 'L-00018', name: 'Northside Suite 310 — Lease Amendment', type: 'Lease', age: '5h ago', confidence: 68, flagged: 4 },
  { id: 'E-00231', name: 'Dr. Marcus Hale — Comp Addendum', type: 'Employee', age: 'Yesterday', confidence: 91, flagged: 0 },
];

const fields = [
  ['Counterparty', 'Page 1, header', 'Regence BlueShield', 'high'],
  ['Effective date', 'Section 1.1', 'April 1, 2026', 'high'],
  ['End date', 'Section 2', 'March 31, 2029', 'high'],
  ['Notice period', 'Section 14', '120 days', 'high'],
  ['Renewal type', 'Conflicting language — see §3 vs §7.4', 'Auto-renew 12mo', 'low'],
  ['Rate schedule attached', 'Exhibit A-2', 'Yes — Exhibit A-2', 'high'],
] as const;

export function ReviewQueuePage() {
  return (
    <div className="page">
      <div className="crumb">
        <span>Command center</span>
        <span>›</span>
        <span>Extraction review</span>
      </div>
      <header className="page-head">
        <div>
          <div className="eyebrow"><span className="eyebrow-dot" />Human QA · 4 contracts awaiting review</div>
          <h1 className="page-title">Review extracted fields</h1>
          <div className="page-subtitle">Confirm or correct AI-extracted fields before contracts go live.</div>
        </div>
        <div style={{ display: 'flex', gap: 10 }}>
          <Button variant="secondary">Skip batch</Button>
          <Button>Approve all high-confidence</Button>
        </div>
      </header>
      <section className="review-layout">
        <Card title="Queue">
          <div style={{ margin: '0 -18px -18px' }}>
            {queue.map((item, index) => (
              <div className={`queue-item ${index === 0 ? 'active' : ''}`} key={item.id}>
                <strong>{item.name}</strong>
                <div className="muted mono-label" style={{ marginTop: 7 }}>{item.id} · {item.type} · {item.age}</div>
                <div style={{ alignItems: 'center', display: 'flex', gap: 10, marginTop: 10 }}>
                  <ConfidenceBar value={item.confidence} tone={item.confidence < 75 ? 'accent' : 'ok'} />
                  <span className="mono-label">{item.confidence}%</span>
                  {item.flagged > 0 ? <span className="mono-label" style={{ color: 'var(--px-orange)', marginLeft: 'auto' }}>{item.flagged} flagged</span> : null}
                </div>
              </div>
            ))}
          </div>
        </Card>
        <Card eyebrow="C-00212 · AI extracted 2h ago · 8 fields" title="Regence BlueShield — Amendment #3">
          <div style={{ background: 'var(--px-surface-2)', border: '1px solid var(--px-line)', borderRadius: 'var(--px-radius)', display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', marginBottom: 18, padding: 14 }}>
            <KpiStat label="Overall confidence" value="72%" helper="Review needed" />
            <KpiStat label="High confidence" value="12" helper="auto-approved" />
            <KpiStat label="Medium" value="2" helper="review recommended" />
            <KpiStat label="Low - flagged" value="3" helper="must confirm" />
          </div>
          <div style={{ margin: '0 -18px' }}>
            {fields.map((field) => (
              <div className="field-row" key={field[0]}>
                <div>
                  <div className="field-label">{field[0]}</div>
                  <div className="field-source">{field[1]}</div>
                </div>
                <div>
                  <div className="field-value">{field[2]}</div>
                  {field[3] === 'low' ? <div className="mono-label" style={{ color: 'var(--px-orange)', marginTop: 7 }}>Manual review required</div> : null}
                </div>
                <div className="confidence">
                  <ConfidenceBar value={field[3] === 'low' ? 36 : 91} tone={field[3] === 'low' ? 'accent' : 'ok'} />
                  <span className="mono-label">{field[3]}</span>
                </div>
                <Button variant={field[3] === 'low' ? 'secondary' : 'confirm'}>Confirm</Button>
              </div>
            ))}
          </div>
        </Card>
      </section>
    </div>
  );
}

function KpiStat({ label, value, helper }: { label: string; value: string; helper: string }) {
  return (
    <div>
      <div className="mono-label">{label}</div>
      <div style={{ fontFamily: 'var(--px-serif)', fontSize: 26, lineHeight: 1.1 }}>{value}</div>
      <div className="muted">{helper}</div>
    </div>
  );
}

