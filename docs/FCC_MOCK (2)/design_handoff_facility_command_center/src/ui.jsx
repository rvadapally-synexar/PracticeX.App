// UI primitives — icons, chips, breadcrumbs
const { useState, useEffect, useMemo, useRef, useCallback } = React;

// Minimal stroke icons
const Icon = ({ name, size = 14 }) => {
  const paths = {
    home: <><path d="M3 11L10 4l7 7"/><path d="M5 10v7h4v-4h2v4h4v-7"/></>,
    file: <><path d="M5 2h7l3 3v13H5z"/><path d="M12 2v3h3"/></>,
    alert: <><path d="M10 3l8 14H2z"/><path d="M10 8v4M10 14v1"/></>,
    clock: <><circle cx="10" cy="10" r="7"/><path d="M10 6v4l3 2"/></>,
    check: <><path d="M4 10l4 4 8-8"/></>,
    plus: <><path d="M10 4v12M4 10h12"/></>,
    search: <><circle cx="9" cy="9" r="6"/><path d="M13.5 13.5L17 17"/></>,
    chev: <><path d="M6 8l4 4 4-4"/></>,
    chevR: <><path d="M8 6l4 4-4 4"/></>,
    x: <><path d="M5 5l10 10M15 5L5 15"/></>,
    upload: <><path d="M10 3v10M6 7l4-4 4 4"/><path d="M3 15v2h14v-2"/></>,
    filter: <><path d="M3 4h14M6 9h8M9 14h2"/></>,
    settings: <><circle cx="10" cy="10" r="2.5"/><path d="M10 3v2M10 15v2M3 10h2M15 10h2M5 5l1.5 1.5M13.5 13.5L15 15M5 15l1.5-1.5M13.5 6.5L15 5"/></>,
    building: <><path d="M3 17h14"/><path d="M5 17V5h6v12"/><path d="M11 17V9h4v8"/><path d="M7 8h2M7 11h2M7 14h2"/></>,
    user: <><circle cx="10" cy="7" r="3"/><path d="M4 17c0-3 2.7-5 6-5s6 2 6 5"/></>,
    grid: <><rect x="3" y="3" width="6" height="6"/><rect x="11" y="3" width="6" height="6"/><rect x="3" y="11" width="6" height="6"/><rect x="11" y="11" width="6" height="6"/></>,
    doc: <><path d="M5 2h7l3 3v13H5z"/><path d="M7 8h6M7 11h6M7 14h4"/></>,
    bell: <><path d="M5 8a5 5 0 0110 0v4l1.5 2h-13L5 12z"/><path d="M8.5 17a1.5 1.5 0 003 0"/></>,
    stack: <><path d="M10 3l7 4-7 4-7-4 7-4z"/><path d="M3 11l7 4 7-4"/><path d="M3 14l7 4 7-4"/></>,
    sparkle: <><path d="M10 3v4M10 13v4M3 10h4M13 10h4"/><path d="M5.5 5.5l2.5 2.5M12 12l2.5 2.5M5.5 14.5l2.5-2.5M12 8l2.5-2.5"/></>,
    zap: <><path d="M11 3L4 12h5l-1 5 7-9h-5z"/></>,
    book: <><path d="M4 4h5a3 3 0 013 3v11a3 3 0 00-3-3H4z"/><path d="M16 4h-5a3 3 0 00-3 3v11a3 3 0 013-3h5z"/></>,
    download: <><path d="M10 3v10M6 9l4 4 4-4"/><path d="M3 15v2h14v-2"/></>,
  };
  return (
    <svg className="ico" width={size} height={size} viewBox="0 0 20 20" fill="none"
      stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" strokeLinejoin="round">
      {paths[name]}
    </svg>
  );
};

const Chip = ({ kind = 'ink', children, dot }) => (
  <span className={`chip ${kind}`}>
    {dot && <span className="dot"/>}
    {children}
  </span>
);

const statusChip = (c) => {
  const s = window.statusFor(c.end);
  if (s === 'expired') return <Chip kind="danger" dot>Expired</Chip>;
  if (s === 'renewal-urgent') return <Chip kind="accent" dot>Renewal · urgent</Chip>;
  if (s === 'renewal-soon') return <Chip kind="warn" dot>Renewal · soon</Chip>;
  return <Chip kind="ok" dot>Active</Chip>;
};

const daysBetween = (dateStr) => Math.round((new Date(dateStr) - window.TODAY) / 86400000);

const formatDate = (dateStr) => {
  const d = new Date(dateStr);
  return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
};

const CategoryLabel = ({ id }) => {
  const map = { payer: 'Payer', vendor: 'Vendor', lease: 'Lease', employee: 'Employee', processor: 'Processor' };
  return <span>{map[id] || id}</span>;
};

const FacilityBadge = ({ id }) => {
  const f = window.FACILITIES.find(f => f.id === id);
  if (!f) return null;
  return <span className="counterparty-avatar" title={f.name}>{f.code}</span>;
};

Object.assign(window, { Icon, Chip, statusChip, daysBetween, formatDate, CategoryLabel, FacilityBadge });
