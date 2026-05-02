import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { Card, KpiCard } from '@practicex/design-system';
import { analysisApi, type RenewalAction, type RenewalsResponse, readableFamily } from '../lib/api';

type LoadState =
  | { kind: 'loading' }
  | { kind: 'error'; message: string }
  | { kind: 'ready'; data: RenewalsResponse };

export function RenewalsPage() {
  const [state, setState] = useState<LoadState>({ kind: 'loading' });
  const [familyFilter, setFamilyFilter] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const data = await analysisApi.getRenewals();
        if (!cancelled) setState({ kind: 'ready', data });
      } catch (err) {
        if (cancelled) return;
        const detail =
          (err as { detail?: string } | undefined)?.detail ??
          (err as Error)?.message ??
          'Failed to load renewals.';
        setState({ kind: 'error', message: detail });
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  const filteredBuckets = useMemo(() => {
    if (state.kind !== 'ready') return [];
    if (!familyFilter) return state.data.buckets;
    return state.data.buckets.map((b) => ({
      ...b,
      items: b.items.filter((i) => i.family === familyFilter),
    }));
  }, [state, familyFilter]);

  if (state.kind === 'loading') {
    return (
      <div className="page">
        <div className="page-subtitle">Loading renewals…</div>
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

  const { data } = state;
  const today = new Date(data.today + 'T00:00:00').toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });

  const families = Array.from(new Set(data.actions.map((a) => a.family))).sort();
  const total = familyFilter
    ? data.actions.filter((a) => a.family === familyFilter).length
    : data.counts.total;
  const overdue = familyFilter
    ? data.actions.filter((a) => a.family === familyFilter && a.daysFromToday < 0).length
    : data.counts.overdue;
  const within30 = familyFilter
    ? data.actions.filter((a) => a.family === familyFilter && a.daysFromToday >= 0 && a.daysFromToday <= 30).length
    : data.counts.within30;
  const within90 = familyFilter
    ? data.actions.filter((a) => a.family === familyFilter && a.daysFromToday >= 0 && a.daysFromToday <= 90).length
    : data.counts.within90;

  return (
    <div className="page">
      <div className="crumb">
        <span>PracticeX</span>
        <span>›</span>
        <span>Renewals</span>
      </div>
      <header className="page-head">
        <div>
          <div className="eyebrow">
            <span className="eyebrow-dot" />
            Operational triggers · As of {today}
          </div>
          <h1 className="page-title">Renewals & deadlines</h1>
          <div className="page-subtitle">
            Every term boundary, notice deadline, and survival window we extracted from your contracts —
            sorted by how soon you need to act.
          </div>
        </div>
      </header>

      <section className="kpi-grid">
        <KpiCard
          label="Total upcoming actions"
          value={total.toString()}
          helper="Across all contract families"
        />
        <KpiCard
          label="Overdue"
          value={overdue.toString()}
          helper={overdue === 0 ? 'Nothing past due' : 'Past their action date'}
          tone={overdue > 0 ? 'warn' : undefined}
        />
        <KpiCard
          label="Next 30 days"
          value={within30.toString()}
          helper={within30 === 0 ? 'Calm month ahead' : 'Decisions needed soon'}
          tone={within30 > 0 ? 'accent' : undefined}
        />
        <KpiCard
          label="Next 90 days"
          value={within90.toString()}
          helper="Plan boardroom-level discussion"
        />
      </section>

      {families.length > 0 ? (
        <section style={{ display: 'flex', gap: 8, marginBottom: 18, flexWrap: 'wrap' }}>
          <button
            type="button"
            className={`renewal-chip ${!familyFilter ? 'is-active' : ''}`}
            onClick={() => setFamilyFilter(null)}
          >
            All families
          </button>
          {families.map((f) => (
            <button
              key={f}
              type="button"
              className={`renewal-chip ${familyFilter === f ? 'is-active' : ''}`}
              onClick={() => setFamilyFilter(familyFilter === f ? null : f)}
            >
              {readableFamily(f)}
            </button>
          ))}
        </section>
      ) : null}

      {data.actions.length === 0 ? (
        <Card title="No renewal actions detected">
          <div className="muted" style={{ fontSize: 14, lineHeight: 1.6 }}>
            Either no contracts have been refined with the canonical-headline LLM yet, or every
            extracted contract is missing both expiration and term-length data. Run "Refine all
            with LLM" on the Portfolio page to populate headline fields.
          </div>
        </Card>
      ) : (
        <section className="renewals-timeline">
          {filteredBuckets.map((bucket) =>
            bucket.items.length === 0 ? null : (
              <BucketSection key={bucket.key} bucketKey={bucket.key} label={bucket.label} items={bucket.items} />
            ),
          )}
        </section>
      )}
    </div>
  );
}

function BucketSection({
  bucketKey,
  label,
  items,
}: {
  bucketKey: string;
  label: string;
  items: RenewalAction[];
}) {
  return (
    <div className={`renewal-bucket renewal-bucket-${bucketKey}`}>
      <div className="renewal-bucket-head">
        <span className="renewal-bucket-label">{label}</span>
        <span className="renewal-bucket-count">{items.length}</span>
      </div>
      <div className="renewal-action-list">
        {items.map((item, idx) => (
          <ActionRow key={`${item.documentAssetId}-${item.actionType}-${idx}`} action={item} />
        ))}
      </div>
    </div>
  );
}

function ActionRow({ action }: { action: RenewalAction }) {
  const dateLabel = new Date(action.actionDate + 'T00:00:00').toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });
  const days = action.daysFromToday;
  const dayLabel =
    days < 0
      ? `${Math.abs(days)} day${Math.abs(days) === 1 ? '' : 's'} overdue`
      : days === 0
      ? 'today'
      : `in ${days} day${days === 1 ? '' : 's'}`;

  return (
    <Link
      to={`/portfolio/${action.documentAssetId}`}
      className={`renewal-action renewal-action-${action.severity}`}
    >
      <div className="renewal-action-date">
        <div className="renewal-action-date-main">{dateLabel}</div>
        <div className="renewal-action-date-rel">{dayLabel}</div>
      </div>
      <div className="renewal-action-body">
        <div className="renewal-action-type">{action.actionType}</div>
        <div className="renewal-action-desc">{action.description}</div>
        <div className="renewal-action-meta">
          {action.counterparty ? <strong>{action.counterparty}</strong> : null}
          {action.counterparty ? <span className="muted"> · </span> : null}
          <span className="muted">{action.fileName}</span>
        </div>
      </div>
      <div className={`renewal-action-badge renewal-badge-${action.severity}`}>
        {action.severity.toUpperCase()}
      </div>
    </Link>
  );
}
