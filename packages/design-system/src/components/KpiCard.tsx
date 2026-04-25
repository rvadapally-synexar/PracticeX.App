import type { ReactNode } from 'react';

export type KpiTone = 'default' | 'accent' | 'warn';

export interface KpiCardProps {
  label: ReactNode;
  value: ReactNode;
  helper?: ReactNode;
  tone?: KpiTone;
}

export function KpiCard({ label, value, helper, tone = 'default' }: KpiCardProps) {
  const cls = ['px-kpi-card', tone !== 'default' ? tone : ''].filter(Boolean).join(' ');
  return (
    <div className={cls}>
      <div className="px-kpi-label">{label}</div>
      <div className="px-kpi-value">{value}</div>
      {helper ? <div className="px-kpi-helper">{helper}</div> : null}
    </div>
  );
}
