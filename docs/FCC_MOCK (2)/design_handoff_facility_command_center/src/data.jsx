// Data layer — mock contracts, facilities, alerts, obligations.
// Dates computed relative to "today" (April 18, 2026) for realism.

const TODAY = new Date('2026-04-18');

const daysFromNow = (n) => {
  const d = new Date(TODAY);
  d.setDate(d.getDate() + n);
  return d.toISOString().slice(0,10);
};

const FACILITIES = [
  { id: 'all',  code: 'ALL', name: 'All facilities', city: '—' },
  { id: 'nor',  code: 'NOR', name: 'Northside Endoscopy Center', city: 'Bellevue, WA' },
  { id: 'lkv',  code: 'LKV', name: 'Lakeview GI Associates', city: 'Kirkland, WA' },
  { id: 'evg',  code: 'EVG', name: 'Evergreen ASC', city: 'Redmond, WA' },
  { id: 'pug',  code: 'PUG', name: 'Puget Digestive Clinic', city: 'Seattle, WA' },
];

const CATEGORIES = [
  { id: 'payer',    label: 'Payer agreements',    count: 14 },
  { id: 'vendor',   label: 'Vendor contracts',    count: 38 },
  { id: 'lease',    label: 'Leases & real estate', count: 7 },
  { id: 'employee', label: 'Employee & provider', count: 21 },
  { id: 'processor',label: 'Processor & financial', count: 6 },
];

const CP = (name) => ({ name, initials: name.split(/\s+/).slice(0,2).map(w=>w[0]).join('').toUpperCase() });

// status rules: based on end date delta to today
const statusFor = (end) => {
  const diff = Math.round((new Date(end) - TODAY) / 86400000);
  if (diff < 0) return 'expired';
  if (diff <= 45) return 'renewal-urgent';
  if (diff <= 120) return 'renewal-soon';
  return 'active';
};

const CONTRACTS = [
  {
    id: 'C-00142',
    name: 'Regence BlueShield — Commercial PPO',
    category: 'payer',
    facility: 'nor',
    counterparty: CP('Regence BlueShield'),
    effective: '2022-07-01',
    end: daysFromNow(28),
    renewal: 'Auto-renew 12mo',
    notice: '90 days written',
    owner: 'J. Okafor',
    value: null,
    docPages: 12,
    flags: ['Notice window open'],
  },
  {
    id: 'C-00138',
    name: 'Aetna — Commercial & Medicare Advantage',
    category: 'payer',
    facility: 'lkv',
    counterparty: CP('Aetna Health'),
    effective: '2023-01-15',
    end: daysFromNow(63),
    renewal: 'Evergreen',
    notice: '60 days written',
    owner: 'J. Okafor',
    docPages: 18,
  },
  {
    id: 'C-00129',
    name: 'UnitedHealthcare — All Plans',
    category: 'payer',
    facility: 'evg',
    counterparty: CP('UnitedHealthcare'),
    effective: '2021-09-01',
    end: daysFromNow(-12),
    renewal: 'Expired',
    notice: '90 days written',
    owner: 'M. Paredes',
    docPages: 24,
    flags: ['Past due — acknowledge'],
  },
  {
    id: 'C-00156',
    name: 'Cigna — PPO & HMO',
    category: 'payer',
    facility: 'nor',
    counterparty: CP('Cigna'),
    effective: '2023-04-01',
    end: daysFromNow(165),
    renewal: 'Auto-renew 12mo',
    notice: '120 days written',
    owner: 'J. Okafor',
    docPages: 16,
  },
  {
    id: 'C-00201',
    name: 'Premera Blue Cross — Facility Fee Schedule',
    category: 'payer',
    facility: 'pug',
    counterparty: CP('Premera BC'),
    effective: '2024-01-01',
    end: daysFromNow(258),
    renewal: 'Auto-renew 12mo',
    notice: '90 days written',
    owner: 'M. Paredes',
    docPages: 22,
  },

  // Vendor
  {
    id: 'V-00412',
    name: 'Olympus — Scope Service & Loaner Agreement',
    category: 'vendor',
    facility: 'nor',
    counterparty: CP('Olympus America'),
    effective: '2023-06-01',
    end: daysFromNow(42),
    renewal: 'Auto-renew 12mo',
    notice: '60 days written',
    owner: 'R. Chen',
    docPages: 9,
    annualValue: 84000,
    flags: ['Notice window opens in 2 weeks'],
  },
  {
    id: 'V-00389',
    name: 'Stericycle — Medical Waste Collection',
    category: 'vendor',
    facility: 'all',
    counterparty: CP('Stericycle'),
    effective: '2022-03-01',
    end: daysFromNow(-3),
    renewal: 'Auto-renew 36mo',
    notice: '90 days written',
    owner: 'R. Chen',
    annualValue: 28400,
    docPages: 6,
    flags: ['Auto-renewed — review terms'],
  },
  {
    id: 'V-00430',
    name: 'Cardinal Health — GI Consumables',
    category: 'vendor',
    facility: 'all',
    counterparty: CP('Cardinal Health'),
    effective: '2023-09-01',
    end: daysFromNow(112),
    renewal: '24mo term',
    notice: '60 days written',
    owner: 'R. Chen',
    annualValue: 142000,
    docPages: 14,
  },
  {
    id: 'V-00398',
    name: 'Henry Schein — Office & Clinical Supplies',
    category: 'vendor',
    facility: 'lkv',
    counterparty: CP('Henry Schein'),
    effective: '2024-05-01',
    end: daysFromNow(381),
    renewal: 'Auto-renew 12mo',
    notice: '30 days written',
    owner: 'R. Chen',
    annualValue: 46300,
    docPages: 5,
  },
  {
    id: 'V-00444',
    name: 'Epic Community Connect — EHR Hosting',
    category: 'vendor',
    facility: 'all',
    counterparty: CP('Epic Systems'),
    effective: '2022-01-01',
    end: daysFromNow(256),
    renewal: '36mo term',
    notice: '180 days written',
    owner: 'S. Whitfield',
    annualValue: 218000,
    docPages: 62,
  },
  {
    id: 'V-00417',
    name: 'CoverMyMeds — Prior Auth Service',
    category: 'vendor',
    facility: 'all',
    counterparty: CP('CoverMyMeds'),
    effective: '2024-02-01',
    end: daysFromNow(76),
    renewal: 'Auto-renew 12mo',
    notice: '45 days written',
    owner: 'S. Whitfield',
    annualValue: 19800,
    docPages: 4,
  },

  // Leases
  {
    id: 'L-00011',
    name: 'Northside Medical Plaza — Suite 310',
    category: 'lease',
    facility: 'nor',
    counterparty: CP('Meridian Properties'),
    effective: '2021-11-01',
    end: daysFromNow(196),
    renewal: 'Option to extend 5yr',
    notice: '180 days written',
    owner: 'A. Linh',
    docPages: 38,
    annualValue: 284000,
    flags: ['Extension option window open'],
  },
  {
    id: 'L-00007',
    name: 'Kirkland Professional Center — Suite 210',
    category: 'lease',
    facility: 'lkv',
    counterparty: CP('Kirkland Properties LLC'),
    effective: '2020-03-01',
    end: daysFromNow(512),
    renewal: 'Option to extend 3yr',
    notice: '120 days written',
    owner: 'A. Linh',
    docPages: 42,
    annualValue: 198000,
  },
  {
    id: 'L-00015',
    name: 'Evergreen ASC Building — Ground Floor',
    category: 'lease',
    facility: 'evg',
    counterparty: CP('Redmond Medical Holdings'),
    effective: '2022-06-01',
    end: daysFromNow(88),
    renewal: 'Option to extend 5yr',
    notice: '120 days written',
    owner: 'A. Linh',
    docPages: 56,
    annualValue: 412000,
    flags: ['Extension notice deadline approaching'],
  },

  // Employee
  {
    id: 'E-00207',
    name: 'Dr. Priya Raman — Physician Employment',
    category: 'employee',
    facility: 'nor',
    counterparty: CP('Dr. Priya Raman'),
    effective: '2023-08-01',
    end: daysFromNow(470),
    renewal: '3yr term, auto-renew 12mo',
    notice: '90 days written',
    owner: 'K. Sato',
    docPages: 22,
  },
  {
    id: 'E-00214',
    name: 'Dr. Marcus Hale — Physician Employment',
    category: 'employee',
    facility: 'evg',
    counterparty: CP('Dr. Marcus Hale'),
    effective: '2022-11-01',
    end: daysFromNow(196),
    renewal: '3yr term, auto-renew 12mo',
    notice: '90 days written',
    owner: 'K. Sato',
    docPages: 22,
  },
  {
    id: 'E-00222',
    name: 'CRNA Group Services — Northwest Anesthesia',
    category: 'employee',
    facility: 'all',
    counterparty: CP('Northwest Anesthesia'),
    effective: '2023-01-01',
    end: daysFromNow(23),
    renewal: 'Auto-renew 12mo',
    notice: '60 days written',
    owner: 'K. Sato',
    docPages: 14,
    annualValue: 960000,
    flags: ['Critical renewal — notice window open'],
  },
  {
    id: 'E-00198',
    name: 'Dr. Elena Vasquez — Physician Employment',
    category: 'employee',
    facility: 'pug',
    counterparty: CP('Dr. Elena Vasquez'),
    effective: '2024-03-01',
    end: daysFromNow(680),
    renewal: '3yr term',
    notice: '90 days written',
    owner: 'K. Sato',
    docPages: 22,
  },

  // Processor
  {
    id: 'P-00052',
    name: 'Waystar — Claims Clearinghouse',
    category: 'processor',
    facility: 'all',
    counterparty: CP('Waystar'),
    effective: '2023-05-01',
    end: daysFromNow(18),
    renewal: 'Auto-renew 12mo',
    notice: '45 days written',
    owner: 'S. Whitfield',
    annualValue: 38400,
    docPages: 11,
    flags: ['Notice deadline passed — auto-renew imminent'],
  },
  {
    id: 'P-00061',
    name: 'Stripe — Patient Payments',
    category: 'processor',
    facility: 'all',
    counterparty: CP('Stripe Inc.'),
    effective: '2024-01-15',
    end: daysFromNow(272),
    renewal: 'Month-to-month',
    notice: '30 days written',
    owner: 'S. Whitfield',
    docPages: 6,
  },
];

const ALERTS = [
  {
    id: 'A1', severity: 'danger',
    title: 'UnitedHealthcare agreement expired 12 days ago',
    body: 'Reimbursement rates reverted to out-of-network. Replacement agreement draft is in legal review.',
    contract: 'C-00129',
    meta: 'PAYER · EVG · owner M. Paredes'
  },
  {
    id: 'A2', severity: 'danger',
    title: 'Waystar auto-renew imminent — notice deadline passed',
    body: '$38,400 annual commitment will auto-renew in 18 days. Overrides require executive sign-off.',
    contract: 'P-00052',
    meta: 'PROCESSOR · ALL · owner S. Whitfield'
  },
  {
    id: 'A3', severity: 'warn',
    title: 'CRNA Group Services renewal — 23 days',
    body: 'Notice window is open. Rate escalation of 4.2% triggered per Section 6.3.',
    contract: 'E-00222',
    meta: 'EMPLOYEE · ALL · owner K. Sato'
  },
  {
    id: 'A4', severity: 'warn',
    title: 'Regence BlueShield — 28 days to term',
    body: 'Decision needed on renegotiation vs. letting auto-renew at current rates.',
    contract: 'C-00142',
    meta: 'PAYER · NOR · owner J. Okafor'
  },
  {
    id: 'A5', severity: 'info',
    title: 'Evergreen ASC lease — extension window open',
    body: '5-year extension option available. Decision required 88 days before term end.',
    contract: 'L-00015',
    meta: 'LEASE · EVG · owner A. Linh'
  },
  {
    id: 'A6', severity: 'info',
    title: 'Stericycle auto-renewed — review new terms',
    body: 'Contract auto-renewed on April 15. Rate escalation of 6.1% applied.',
    contract: 'V-00389',
    meta: 'VENDOR · ALL · owner R. Chen'
  },
];

const OBLIGATIONS = [
  { id: 'O1', title: 'Send 90-day renewal notice — Regence BlueShield', due: daysFromNow(-2), facility: 'nor', contract: 'C-00142', done: false },
  { id: 'O2', title: 'Quarterly compliance attestation — OSHA bloodborne pathogens', due: daysFromNow(5), facility: 'all', contract: null, done: false },
  { id: 'O3', title: 'Submit rate sheet update — Aetna provider portal', due: daysFromNow(9), facility: 'lkv', contract: 'C-00138', done: false },
  { id: 'O4', title: 'Certificate of Insurance renewal — Northside lease', due: daysFromNow(12), facility: 'nor', contract: 'L-00011', done: false },
  { id: 'O5', title: 'Annual CLIA registration — Evergreen ASC', due: daysFromNow(21), facility: 'evg', contract: null, done: false },
  { id: 'O6', title: 'Scope loaner inventory reconciliation — Olympus', due: daysFromNow(-5), facility: 'nor', contract: 'V-00412', done: true },
  { id: 'O7', title: 'Business Associate Agreement refresh — Waystar', due: daysFromNow(28), facility: 'all', contract: 'P-00052', done: false },
];

// Sample extracted fields for contract detail view
const EXTRACTED = {
  'C-00142': {
    universal: [
      { label: 'Document name', value: 'Regence BlueShield — Commercial PPO', conf: 'high' },
      { label: 'Contract type', value: 'Payer agreement', conf: 'high' },
      { label: 'Facility', value: 'Northside Endoscopy Center', conf: 'high' },
      { label: 'Counterparty', value: 'Regence BlueShield of Washington', conf: 'high' },
      { label: 'Effective date', value: 'July 1, 2022', conf: 'high' },
      { label: 'End date', value: 'May 16, 2026', conf: 'high', anchor: 'term' },
      { label: 'Renewal type', value: 'Auto-renew — 12 months', conf: 'high', anchor: 'renewal' },
      { label: 'Notice period', value: '90 days written notice', conf: 'high', anchor: 'notice' },
      { label: 'Owner', value: 'J. Okafor', conf: 'high' },
      { label: 'Status', value: 'Active — renewal window open', conf: 'high' },
    ],
    payer: [
      { label: 'Payer name', value: 'Regence BlueShield of Washington', conf: 'high' },
      { label: 'Fee schedule present', value: 'Yes — Exhibit A', conf: 'high', anchor: 'fee' },
      { label: 'Reimbursement method', value: 'Percentage of billed charges + fixed per-case rates for ASC CPTs', conf: 'med' },
      { label: 'Amendment present', value: 'Amendment #2 — July 2024 (rate adjustment)', conf: 'med' },
      { label: 'CPT scope', value: '43235, 43239, 45378, 45380, 45385 + facility fees', conf: 'low' },
    ],
  },
};

Object.assign(window, {
  TODAY, daysFromNow, FACILITIES, CATEGORIES, CONTRACTS, ALERTS, OBLIGATIONS, EXTRACTED, statusFor
});
