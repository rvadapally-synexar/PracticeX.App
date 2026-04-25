import { Button, Card, KpiCard, StatusChip } from '@practicex/design-system';

const renewals = [
  ['Apr 30', 'Regence BlueShield amendment', 'Notice window closes in 5d'],
  ['May 14', 'Olympus scope service renewal', 'Owner review due in 19d'],
  ['Jun 01', 'Northside Suite 310 lease', 'Renewal watch'],
];

export function CommandCenterPage() {
  return (
    <div className="page">
      <div className="crumb">
        <span>PracticeX</span>
        <span>›</span>
        <span>Command center</span>
      </div>
      <header className="page-head">
        <div>
          <div className="eyebrow"><span className="eyebrow-dot" />Operations overview · Apr 25, 2026</div>
          <h1 className="page-title">Command center</h1>
          <div className="page-subtitle">Everything you signed. What renews. What needs action this month.</div>
        </div>
        <div style={{ display: 'flex', gap: 10 }}>
          <Button variant="secondary">View all contracts</Button>
          <Button>Upload documents</Button>
        </div>
      </header>
      <section className="kpi-grid">
        <KpiCard label="Active contracts" value="20" helper="Across 5 facilities" />
        <KpiCard label="Renew in 60 days" value="8" helper="3 need owner action" tone="accent" />
        <KpiCard label="Needs attention" value="6" helper="Low-confidence or overdue" tone="warn" />
        <KpiCard label="Tracked annual value" value="$2.4M" helper="Reviewed documents only" />
      </section>
      <section className="grid-2">
        <Card title="Upcoming renewals & notice windows">
          {renewals.map((row) => (
            <div className="source-row" key={row[1]} style={{ gridTemplateColumns: '72px 1fr 150px' }}>
              <div className="mono-label">{row[0]}</div>
              <strong>{row[1]}</strong>
              <StatusChip tone={row[2].includes('5d') ? 'accent' : 'ok'}>{row[2]}</StatusChip>
            </div>
          ))}
        </Card>
        <Card title="Alerts">
          <div className="source-row" style={{ gridTemplateColumns: '1fr 96px' }}>
            <div>
              <strong>3 extracted fields flagged</strong>
              <div className="muted">Regence BlueShield Amendment #3</div>
            </div>
            <Button variant="secondary">Review</Button>
          </div>
          <div className="source-row" style={{ gridTemplateColumns: '1fr 96px' }}>
            <div>
              <strong>Missing accountable owner</strong>
              <div className="muted">Lease amendment needs assignment</div>
            </div>
            <Button variant="secondary">Assign</Button>
          </div>
        </Card>
      </section>
    </div>
  );
}

