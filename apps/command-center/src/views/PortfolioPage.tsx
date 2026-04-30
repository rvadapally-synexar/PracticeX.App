import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Button, Card, KpiCard, StatusChip } from '@practicex/design-system';
import {
  analysisApi,
  type BatchExtractionResult,
  type Portfolio,
  type PortfolioFamily,
  type PortfolioDocument,
  type PortfolioInsights,
  readableCandidateType,
  readableFamily,
} from '../lib/api';

type LoadState =
  | { kind: 'loading' }
  | { kind: 'empty' }
  | { kind: 'error'; message: string }
  | { kind: 'ready'; portfolio: Portfolio; insights: PortfolioInsights };

export function PortfolioPage() {
  const [state, setState] = useState<LoadState>({ kind: 'loading' });

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const [portfolio, insights] = await Promise.all([
          analysisApi.getPortfolio(),
          analysisApi.getInsights(),
        ]);
        if (cancelled) return;
        if (portfolio.totalDocuments === 0) {
          setState({ kind: 'empty' });
          return;
        }
        setState({ kind: 'ready', portfolio, insights });
      } catch (err) {
        if (cancelled) return;
        const message = err instanceof Error ? err.message : 'Failed to load portfolio.';
        setState({ kind: 'error', message });
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  if (state.kind === 'loading') {
    return (
      <div className="page">
        <div className="page-subtitle">Loading portfolio…</div>
      </div>
    );
  }

  if (state.kind === 'empty') {
    return (
      <div className="page">
        <header className="page-head">
          <div>
            <div className="eyebrow">
              <span className="eyebrow-dot" />
              Premium analysis surface
            </div>
            <h1 className="page-title">Portfolio</h1>
            <div className="page-subtitle">
              No documents yet. Upload contracts via Source Discovery, then come back here.
            </div>
          </div>
          <Link to="/sources">
            <button className="px-button">Go to Source Discovery</button>
          </Link>
        </header>
      </div>
    );
  }

  if (state.kind === 'error') {
    return (
      <div className="page">
        <div className="banner banner-error">{state.message}</div>
      </div>
    );
  }

  const { portfolio, insights } = state;

  return (
    <div className="page">
      <div className="crumb">
        <span>PracticeX</span>
        <span>›</span>
        <span>Portfolio</span>
      </div>
      <header className="page-head">
        <div>
          <div className="eyebrow">
            <span className="eyebrow-dot" />
            Premium analysis · Auto-extracted from your contracts
          </div>
          <h1 className="page-title">Portfolio intelligence</h1>
          <div className="page-subtitle">
            What we read from {portfolio.totalDocuments} document{portfolio.totalDocuments === 1 ? '' : 's'}
            {' '}across your filing cabinet.
          </div>
        </div>
        <BatchLlmButton
          totalDocs={portfolio.totalDocuments}
          onCompleted={async () => {
            const [p, i] = await Promise.all([
              analysisApi.getPortfolio(),
              analysisApi.getInsights(),
            ]);
            setState({ kind: 'ready', portfolio: p, insights: i });
          }}
        />
      </header>

      <section className="kpi-grid">
        <KpiCard
          label="Documents processed"
          value={portfolio.totalDocuments.toString()}
          helper={`${portfolio.totalPages} pages · ${portfolio.totalSizeMb.toFixed(1)} MB`}
        />
        <KpiCard
          label="Scanned PDFs OCR'd"
          value={portfolio.docIntelPagesProcessed.toString()}
          helper={`Azure Doc Intelligence · $${portfolio.estimatedDocIntelCostUsd.toFixed(2)}`}
          tone="accent"
        />
        <KpiCard
          label="Total rentable sqft"
          value={
            insights.totalRentableSqft != null
              ? insights.totalRentableSqft.toLocaleString('en-US')
              : '—'
          }
          helper={`Across ${portfolio.families.find((f) => f.family === 'lease')?.documentCount ?? 0} leases`}
        />
        <KpiCard
          label="Unique counterparties"
          value={insights.uniqueCounterparties.length.toString()}
          helper={`${insights.uniqueLandlords.length} landlords · ${insights.uniqueTenants.length} tenants`}
        />
      </section>

      <section className="family-grid">
        {portfolio.families.map((family) => (
          <FamilyCard key={family.family} family={family} />
        ))}
      </section>

      <section className="grid-2">
        <Card title={`Cross-document insights · ${insights.amendmentChains.length} amendment chains`}>
          <InsightsPanel insights={insights} />
        </Card>
        <Card title="Document-level findings">
          <div className="muted" style={{ marginBottom: 8, fontSize: 13 }}>
            Click any row to drill into extracted fields.
          </div>
          <div className="doc-table">
            {portfolio.documents.map((d) => (
              <DocumentRow key={d.documentAssetId} doc={d} />
            ))}
          </div>
        </Card>
      </section>
    </div>
  );
}

function BatchLlmButton({
  totalDocs,
  onCompleted,
}: {
  totalDocs: number;
  onCompleted: () => Promise<void>;
}) {
  const [running, setRunning] = useState(false);
  const [result, setResult] = useState<BatchExtractionResult | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function run(force: boolean) {
    setRunning(true);
    setError(null);
    setResult(null);
    try {
      const r = await analysisApi.llmExtractBatch(force);
      setResult(r);
      await onCompleted();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Batch extraction failed.');
    } finally {
      setRunning(false);
    }
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-end', gap: 6 }}>
      <Button onClick={() => run(false)} disabled={running}>
        {running ? `Refining ${totalDocs} docs with LLM…` : 'Refine all with LLM'}
      </Button>
      {result ? (
        <span className="muted" style={{ fontSize: 12 }}>
          ✓ {result.refined} refined · {result.skipped} skipped · {result.failed} failed ·{' '}
          {Math.round(result.latencyMs / 1000)}s · ${(((result.totalTokensIn + result.totalTokensOut) / 1_000_000) * 3).toFixed(2)}
        </span>
      ) : null}
      {error ? <span className="mono-label" style={{ color: 'var(--px-orange)', fontSize: 12 }}>{error}</span> : null}
    </div>
  );
}

function FamilyCard({ family }: { family: PortfolioFamily }) {
  return (
    <Card>
      <div className="family-card-head">
        <div>
          <div className="eyebrow" style={{ fontSize: 11 }}>
            {readableFamily(family.family)}
          </div>
          <div className="family-card-count">{family.documentCount}</div>
        </div>
        {family.docIntelPagesUsed > 0 ? (
          <StatusChip tone="accent">{family.docIntelPagesUsed} OCR'd</StatusChip>
        ) : (
          <StatusChip tone="ok">digital</StatusChip>
        )}
      </div>
      <div className="muted" style={{ fontSize: 12, marginTop: 6 }}>
        {family.totalPages} pages · {family.totalSizeMb.toFixed(2)} MB
      </div>
    </Card>
  );
}

function DocumentRow({ doc }: { doc: PortfolioDocument }) {
  const tone =
    doc.extractionStatus === 'completed'
      ? 'ok'
      : doc.extractionStatus === 'no_extractor'
      ? 'warn'
      : 'muted';

  const statusLabel =
    doc.extractionStatus === 'completed'
      ? 'extracted'
      : doc.extractionStatus === 'no_extractor'
      ? 'no extractor'
      : doc.extractionStatus ?? 'pending';

  return (
    <Link to={`/portfolio/${doc.documentAssetId}`} className="doc-row" style={{ textDecoration: 'none' }}>
      <div className="doc-row-name" title={doc.fileName}>{doc.fileName}</div>
      <div className="doc-row-type">
        {readableCandidateType(doc.candidateType)}
        {doc.extractedSubtype ? <span className="muted"> · {doc.extractedSubtype}</span> : null}
      </div>
      <div className="doc-row-meta">
        {doc.pageCount ?? '—'} pg
        {doc.usedDocIntelligence ? <span className="muted"> · OCR</span> : null}
      </div>
      <div>
        <StatusChip tone={tone}>{statusLabel}</StatusChip>
      </div>
    </Link>
  );
}

function InsightsPanel({ insights }: { insights: PortfolioInsights }) {
  return (
    <div className="insights-panel">
      {insights.uniqueLandlords.length > 0 ? (
        <div className="insight-block">
          <div className="eyebrow" style={{ fontSize: 11 }}>Landlords identified</div>
          <ul className="entity-list">
            {insights.uniqueLandlords.slice(0, 6).map((name) => (
              <li key={name}><strong>{name}</strong></li>
            ))}
            {insights.uniqueLandlords.length > 6 ? (
              <li className="muted">+{insights.uniqueLandlords.length - 6} more variants…</li>
            ) : null}
          </ul>
        </div>
      ) : null}

      {insights.uniqueCounterparties.length > 0 ? (
        <div className="insight-block">
          <div className="eyebrow" style={{ fontSize: 11 }}>Counterparties detected</div>
          <ul className="entity-list">
            {insights.uniqueCounterparties.slice(0, 8).map((name) => (
              <li key={name}>{name}</li>
            ))}
          </ul>
        </div>
      ) : null}

      {insights.amendmentChains.length > 0 ? (
        <div className="insight-block">
          <div className="eyebrow" style={{ fontSize: 11 }}>Amendment chains</div>
          {insights.amendmentChains.map((chain) => (
            <div key={chain.parentDocumentTitle} className="chain">
              <div className="chain-parent">{chain.parentDocumentTitle}</div>
              {chain.amendments.map((amendment) => (
                <div key={amendment} className="chain-amendment">↳ {amendment}</div>
              ))}
            </div>
          ))}
        </div>
      ) : null}

      {Object.keys(insights.documentAddresses).length > 0 ? (
        <div className="insight-block">
          <div className="eyebrow" style={{ fontSize: 11 }}>Addresses surfaced from layout</div>
          <ul className="entity-list">
            {Object.entries(insights.documentAddresses).slice(0, 6).map(([doc, address]) => (
              <li key={doc}>
                <strong>{address}</strong>
                <span className="muted"> · {doc}</span>
              </li>
            ))}
          </ul>
        </div>
      ) : null}
    </div>
  );
}
