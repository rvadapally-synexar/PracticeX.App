import type { ReactNode } from 'react';

export type StatusTone = 'ok' | 'accent' | 'warn' | 'muted';

export interface StatusChipProps {
  tone?: StatusTone;
  children?: ReactNode;
}

export function StatusChip({ tone = 'muted', children }: StatusChipProps) {
  return <span className={`px-status-chip ${tone}`}>{children}</span>;
}
