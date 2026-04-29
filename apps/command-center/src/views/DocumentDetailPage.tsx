import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Card, ConfidenceBar, StatusChip } from '@practicex/design-system';
import {
  analysisApi,
  type DocumentDetail,
  type ExtractedField,
  readableCandidateType,
} from '../lib/api';

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

  if (state.kind === 'loading') {
    return <div className="page"><div className="page-subtitle">Loading document…</div></div>;
  }
  if (state.kind === 'error') {
    return <div className="page"><div className="banner banner-error">{state.message}</div></div>;
  }

  const { detail } = state;
  const fields = detail.extractedFields?.fields ?? [];

  return (
    <div className="page">
      <div className="crumb">
        <Link to="/portfolio">Portfolio</Link>
        <span>›</span>
        <span>{detail.fileName}</span>
      </div>
      <header className="page-head">
        <div>
          <div className="eyebrow">
            <span className="eyebrow-dot" />
            {readableCandidateType(detail.candidateType ?? 'unknown')}
            {detail.extractedSubtype ? <span> · {detail.extractedSubtype}</span> : null}
          </div>
          <h1 className="page-title" style={{ wordBreak: 'break-all' }}>{detail.fileName}</h1>
          <div className="page-subtitle">
            {detail.pageCount ? `${detail.pageCount} pages` : 'page count unknown'}
            {detail.layoutProvider ? <> · OCR via {detail.layoutProvider}</> : null}
            {detail.extractorName ? <> · {detail.extractorName}</> : null}
          </div>
        </div>
        <div style={{ display: 'flex', gap: 8, alignItems: 'flex-start' }}>
          {detail.isExecuted ? <StatusChip tone="ok">signed</StatusChip> : null}
          {detail.isTemplate ? <StatusChip tone="warn">template / unsigned</StatusChip> : null}
          <StatusChip tone={detail.extractionStatus === 'completed' ? 'ok' : 'muted'}>
            {detail.extractionStatus ?? 'pending'}
          </StatusChip>
        </div>
      </header>

      <section className="grid-2">
        <Card title={`Extracted fields · ${fields.length}`}>
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

        <Card title="Source content">
          <div className="muted" style={{ fontSize: 13, marginBottom: 8 }}>
            {detail.layoutProvider
              ? `First 600 characters of OCR-extracted text from ${detail.layoutProvider}.`
              : 'Text snippet from local extraction.'}
          </div>
          <pre className="layout-snippet">{detail.layoutSnippet ?? '(No text snippet available.)'}</pre>
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
  // value comes through JSON.stringify on the server side, so unwrap quoted strings
  if (value.startsWith('"') && value.endsWith('"') && value.length > 1) {
    try {
      return JSON.parse(value);
    } catch {
      return value;
    }
  }
  return value;
}
