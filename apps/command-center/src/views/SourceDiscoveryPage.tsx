import { Button, Card, StatusChip } from '@practicex/design-system';
import { FolderSearch, Mail, ShieldCheck } from 'lucide-react';
import type { ReactNode } from 'react';

const candidates = [
  ['local_folder', 'Regence BlueShield Amendment #3.pdf', 'Payer contract', '92%'],
  ['outlook', 'Olympus Scope Service Renewal.docx', 'Vendor agreement', '86%'],
  ['outlook', 'Lease Amendment - Suite 310.pdf', 'Lease amendment', '78%'],
];

export function SourceDiscoveryPage() {
  return (
    <div className="page">
      <div className="crumb">
        <span>Command center</span>
        <span>›</span>
        <span>Source discovery</span>
      </div>
      <header className="page-head">
        <div>
          <div className="eyebrow"><span className="eyebrow-dot" />Connectors · governed candidate discovery</div>
          <h1 className="page-title">Discover contract evidence</h1>
          <div className="page-subtitle">Scan local folders and Outlook attachments, then choose what enters the ingestion pipeline.</div>
        </div>
        <Button>Start discovery</Button>
      </header>
      <section className="grid-2">
        <Card title="Available connectors">
          <div className="source-card">
            <ConnectorRow icon={<FolderSearch size={17} />} title="Local folder crawler" body="Recursive read-only scan of selected folders, with hashing and candidate classification." status="Ready for demo" />
            <ConnectorRow icon={<Mail size={17} />} title="Outlook mailbox connector" body="OAuth read-only candidate discovery for contract-like attachments and messages." status="Planned for demo" />
            <ConnectorRow icon={<ShieldCheck size={17} />} title="Governance guardrails" body="Candidates require explicit selection before import; every import creates source objects and ingestion jobs." status="Enterprise rule" />
          </div>
        </Card>
        <Card title="Recent candidates">
          {candidates.map((candidate) => (
            <div className="source-row" key={candidate[1]}>
              <div className="source-icon">{candidate[0] === 'outlook' ? <Mail size={16} /> : <FolderSearch size={16} />}</div>
              <div>
                <strong>{candidate[1]}</strong>
                <div className="muted">{candidate[2]} · discovered from {candidate[0]}</div>
              </div>
              <StatusChip tone="ok">{candidate[3]} likely</StatusChip>
              <Button variant="secondary">Import</Button>
            </div>
          ))}
        </Card>
      </section>
    </div>
  );
}

function ConnectorRow({ icon, title, body, status }: { icon: ReactNode; title: string; body: string; status: string }) {
  return (
    <div className="source-row">
      <div className="source-icon">{icon}</div>
      <div>
        <strong>{title}</strong>
        <div className="muted">{body}</div>
      </div>
      <StatusChip tone="ok">{status}</StatusChip>
      <Button variant="secondary">Configure</Button>
    </div>
  );
}

