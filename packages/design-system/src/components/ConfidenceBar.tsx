export type ConfidenceTone = 'ok' | 'accent' | 'warn';

export interface ConfidenceBarProps {
  value: number;
  tone?: ConfidenceTone;
}

export function ConfidenceBar({ value, tone = 'ok' }: ConfidenceBarProps) {
  const clamped = Math.max(0, Math.min(100, value));
  return (
    <div className={`px-confidence-bar ${tone}`} role="meter" aria-valuemin={0} aria-valuemax={100} aria-valuenow={clamped}>
      <div className="fill" style={{ width: `${clamped}%` }} />
    </div>
  );
}
