import type { CSSProperties, ReactNode } from 'react';

export interface CardProps {
  title?: ReactNode;
  eyebrow?: ReactNode;
  actions?: ReactNode;
  children?: ReactNode;
  className?: string;
  style?: CSSProperties;
}

export function Card({ title, eyebrow, actions, children, className, style }: CardProps) {
  const cls = ['px-card', className].filter(Boolean).join(' ');
  return (
    <section className={cls} style={style}>
      {(title || eyebrow || actions) && (
        <header className="px-card-header">
          <div>
            {eyebrow ? <div className="px-card-eyebrow">{eyebrow}</div> : null}
            {title ? <h2 className="px-card-title">{title}</h2> : null}
          </div>
          {actions ? <div>{actions}</div> : null}
        </header>
      )}
      {children}
    </section>
  );
}
