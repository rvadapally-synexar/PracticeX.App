import { useEffect, useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Card, ConfidenceBar, StatusChip } from '@practicex/design-system';
import {
  analysisApi,
  type DocumentDetail,
  type ExtractedField,
  readableCandidateType,
} from '../lib/api';

const API_BASE = (import.meta.env.VITE_API_BASE as string | undefined) ?? '/api';

type LoadState =
  | { kind: 'loading' }
  | { kind: 'error'; message: string }
  | { kind: 'ready'; detail: DocumentDetail };

export function DocumentDetailPage() {
  const { assetId } = useParams<{ assetId: string }>();
  const [state, setState] = useState<LoadState>({ kind: 'loading' });

  useEffect(() => {
    if (!assetId) return;
    let cancelled = false;
    (async () => {
      try {
        const detail = await analysisApi.getDocument(assetId);
        if (!cancelled) setState({ kind: 'ready', detail });
      } catch (err) {
        if (cancelled) return;
        const message = err instanceof Error ? err.message : 'Failed to load document.';
        setState({ kind: 'error', message });
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [assetId]);

  const sourceUrl = useMemo(() => {
    if (!assetId) return null;
    return `${API_BASE}/analysis/documents/${assetId}/content`;
  }, [assetId]);

  if (state.kind === 'loading') {
    return <div className="page"><div className="page-subtitle">Loading document…</div></div>;
  }
  if (state.kind === 'error') {
    return <div className="page"><div className="banner banner-error">{state.message}</div></div>;
  }

  const { detail } = state;
  const fields = detail.extractedFields?.fields ?? [];
  const isPdf = !!detail.fileName && detail.fileName.toLowerCase().endsWith('.pdf');

  return (
    <div className="page document-detail-page">
      <div className="crumb">
        <Link to="/portfolio">Portfolio</Link>
        <span>›</span>
        <span style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', maxWidth: 600, display: 'inline-block', verticalAlign: 'bottom' }}>{detail.fileName}</span>
      </div>
      <header className="page-head" style={{ alignItems: 'flex-start' }}>
        <div style={{ minWidth: 0, flex: 1 }}>
          <div className="eyebrow">
            <span className="eyebrow-dot" />
            {readableCandidateType(detail.candidateType ?? 'unknown')}
            {detail.extractedSubtype ? <span> · {detail.extractedSubtype}</span> : null}
          </div>
          <h1 className="page-title" style={{ wordBreak: 'break-word', fontSize: 22 }}>{detail.fileName}</h1>
          <div className="page-subtitle">
            {detail.pageCount ? `${detail.pageCount} pages` : 'page count unknown'}
            {detail.layoutProvider ? <> · OCR via {detail.layoutProvider}</> : null}
            {detail.extractorName ? <> · {detail.extractorName}</> : null}
          </div>
        </div>
        <div style={{ display: 'flex', gap: 8, alignItems: 'flex-start', flexWrap: 'wrap', justifyContent: 'flex-end' }}>
          {detail.isExecuted ? <StatusChip tone="ok">signed</StatusChip> : null}
          {detail.isTemplate ? <StatusChip tone="warn">template / unsigned</StatusChip> : null}
          <StatusChip tone={detail.extractionStatus === 'completed' ? 'ok' : 'muted'}>
            {detail.extractionStatus ?? 'pending'}
          </StatusChip>
        </div>
      </header>

      <section className="document-split">
        <Card title="Original document" className="document-source-card">
          {sourceUrl ? (
            isPdf ? (
              <iframe
                title="Original PDF"
                src={sourceUrl}
                className="document-source-frame"
              />
            ) : (
              <div className="document-source-fallback">
                <p className="muted" style={{ marginBottom: 12 }}>
                  Browsers can't render this format inline.{' '}
                  <a href={sourceUrl} target="_blank" rel="noreferrer">Open the original</a> in a new tab,
                  or read the OCR snippet below.
                </p>
                {detail.layoutSnippet ? (
                  <pre className="layout-snippet">{detail.layoutSnippet}</pre>
                ) : null}
              </div>
            )
          ) : (
            <div className="muted">No source URL.</div>
          )}
        </Card>

        <Card title={`Extracted fields · ${fields.length}`} className="document-fields-card">
          {fields.length === 0 ? (
            <div className="muted">
              No structured fields extracted.{' '}
              {detail.extractionStatus === 'no_extractor'
                ? "We don't yet have an extractor for this contract type."
                : 'Try processing again or check that the layout extraction succeeded.'}
            </div>
          ) : (
            <div className="field-grid">
              {fields.map((f) => (
                <FieldRow key={f.name} field={f} />
              ))}
            </div>
          )}
          {detail.extractedFields?.reasonCodes && detail.extractedFields.reasonCodes.length > 0 ? (
            <div style={{ marginTop: 16 }}>
              <div className="eyebrow" style={{ fontSize: 11 }}>Reasoning</div>
              <div className="reason-codes">
                {detail.extractedFields.reasonCodes.map((rc) => (
                  <span key={rc} className="reason-pill">{rc}</span>
                ))}
              </div>
            </div>
          ) : null}
        </Card>
      </section>
    </div>
  );
}

function FieldRow({ field }: { field: ExtractedField }) {
  const valueLabel = field.value === null || field.value === '' ? <span className="muted">— not found</span> : <span>{prettyValue(field.value)}</span>;
  return (
    <div className="field-row">
      <div className="field-name">{prettyFieldName(field.name)}</div>
      <div className="field-value">{valueLabel}</div>
      <div className="field-confidence">
        <ConfidenceBar value={field.confidence} />
      </div>
      {field.sourceCitation ? (
        <div className="field-citation muted">{field.sourceCitation}</div>
      ) : null}
    </div>
  );
}

function prettyFieldName(name: string): string {
  return name
    .replace(/_/g, ' ')
    .replace(/\b\w/g, (c) => c.toUpperCase());
}

function prettyValue(value: string): string {
  if (value.startsWith('"') && value.endsWith('"') && value.length > 1) {
    try {
      return JSON.parse(value);
    } catch {
      return value;
    }
  }
  return value;
}
