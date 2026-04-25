# Handoff: Facility Command Center

## For the engineer picking this up (Claude Code)

**Target stack (confirmed by product team):** React + TypeScript + Tailwind CSS.

**Scope of this handoff — two deliverables:**

1. **A reusable design system package** (`packages/design-system/`) extracted from the HTML mocks in this bundle:
   - Design tokens (colors, type, spacing, radii, shadows) as CSS custom properties + a Tailwind preset
   - Primitive components: `Button`, `IconButton`, `Chip`, `StatusChip`, `Input`, `Select`, `Textarea`, `Card`, `Tabs`, `Table`, `Avatar`, `Badge`, `Kbd`, `ConfidenceBar`
   - Layout components: `PageHeader`, `Eyebrow`, `Crumb`, `SectionLabel`, `EmptyState`, `Modal`, `SidebarNav`
   - Domain components: `FacilityBadge`, `CategoryLabel`, `AlertBanner`, `AlertRow`, `ObligationRow`, `ContractRow`, `KpiCard`, `ActivityTimeline`, `NegotiationPlaybookCard`, `BenchmarkBars`
   - Icon component backed by a lucide-style set: `home, stack, clock, alert, check, building, user, settings, bell, search, plus, upload, download, doc, sparkle, zap, book, chev`
   - Storybook with one story per component documenting all variants and states
   - Theming: `operator` (default clay), `dark`, plus `[data-density="compact"]` support

2. **The production web application** (`apps/command-center/`) consuming the design system:
   - Routes: `/dashboard`, `/contracts`, `/contracts/:id`, `/renewals`, `/alerts`, `/obligations`, `/review`, `/rates`
   - Persisted UI state (facility, theme, density, tier) in localStorage or URL
   - Stubbed data layer — keep the shapes from `src/data.jsx` and `src/benchmarks.jsx` as TypeScript types, wire them behind a `useContracts()` / `useAlerts()` / `useBenchmark()` hook that returns the mock data today and can swap to a real API later
   - All flows from the HTML prototype implemented end-to-end: dashboard → contracts → contract detail with split document + extracted fields → upload modal → extraction review → premium rate benchmarking

**Suggested monorepo layout:**
```
.
├── apps/command-center/          # Next.js (App Router) or Vite + React Router
├── packages/design-system/       # the extracted design system
│   ├── src/
│   │   ├── tokens.css            # CSS custom properties
│   │   ├── tailwind-preset.ts
│   │   ├── components/
│   │   ├── icons/
│   │   └── index.ts
│   ├── .storybook/
│   └── package.json
└── package.json                  # workspaces: apps/*, packages/*
```

**Recommended tools inside that stack:** Vite or Next.js App Router, Tailwind CSS v3, Radix UI primitives for a11y-sensitive components (Dialog, Tabs, DropdownMenu, Popover, Tooltip), TanStack Table for the contracts list, TanStack Query for data fetching, Zod for runtime type validation of API responses, Storybook 8, Vitest + Testing Library.

**Suggested first prompt to Claude Code** (paste this after `cd`-ing into the unzipped folder and running `claude`):

> Read `README.md` in this directory top to bottom. Then read every file under `src/` to understand the components and data shapes. Then read `Facility Command Center.html` to see how they're composed and what the global CSS looks like.
>
> Scaffold a pnpm workspaces monorepo with `apps/command-center` (Vite + React + TypeScript + React Router) and `packages/design-system` (React + TypeScript + Tailwind + Storybook). Set up the design system first: port the CSS custom properties from `README.md` into `packages/design-system/src/tokens.css`, write a Tailwind preset that exposes them as semantic color/space/radius tokens, and build the primitive components listed in the README with Storybook stories. Then scaffold the app routes and port each view from `src/*.jsx`, consuming the design system. Use the existing JSX files as behavior references but rewrite everything in idiomatic TypeScript + Tailwind — do not preserve the inline styles. Keep the data mocks as a local in-memory layer behind typed hooks so the real API can replace them later.
>
> Start by showing me the proposed file tree and the first design-system primitive (Button) with its Storybook story. Then wait for my review before continuing.

---

## Overview

Facility Command Center is a contract intelligence platform for independent Ambulatory Surgery Centers (ASCs) and physician practices — starting with GI/endoscopy. The product extracts structured fields from uploaded contracts using AI, surfaces renewal and notice deadlines, tracks obligations, and (as a Premium tier) benchmarks payer rates against regional market data.

The primary persona is a practice / ASC administrator ("Jordan Okafor, ASC Administrator" in the mockups). The operator lens informs the whole design: calm, data-forward, dense-but-not-cluttered, with every surface optimized for triage and accountability.

## About the design files

The files in this bundle are **design references created in HTML** — a high-fidelity prototype showing intended look and behavior, not production code to copy directly. The task is to **recreate these designs in React + TypeScript + Tailwind CSS** (confirmed by the product team), using the team's established patterns for state management and routing. The JSX files under `src/` are loaded via Babel-standalone in the browser — they are a reference for structure and behavior, **not** a shippable React codebase. Treat them as pseudo-code.

## Fidelity

**High-fidelity.** Colors, type, spacing, and interactions are final. Recreate pixel-perfectly, honoring the design tokens below.

## Visual language

### Theme: Roland Garros

The product uses a clay-court inspired palette. It is NOT a sports brand — it uses Roland Garros' material vocabulary (terracotta clay, court green, cream linen) to convey calm, institutional, expert warmth. The result reads as a serious operator tool that happens to be warm.

### Design tokens

All tokens live as CSS custom properties on `:root`.

```
/* Surfaces */
--bg:        #F4EFE2   /* cream paper — app background */
--surface:   #FFFBF0   /* card surface */
--surface-2: #EFE8D4   /* tinted inset surface (table headers, code) */

/* Ink */
--ink:       #1E2A1A   /* primary text, ~95% */
--ink-2:     #2F3E2A   /* body */
--ink-3:     #5C6A54   /* muted / captions */
--ink-4:     #8F9B85   /* meta / placeholder */

/* Lines */
--line:      #D9D0B8   /* borders, dividers */
--line-2:    #E4DCC5   /* subtle dividers */

/* Semantic */
--accent:        #C85A2E   /* clay terracotta — urgency, primary action */
--accent-soft:   #F5DCCC
--ok:            #2E5D3A   /* court green — confirmed, approved */
--ok-soft:       #D3E3C8
--warn:          #B47A12   /* amber */
--warn-soft:     #F2E3BF

/* Type */
--ff-serif: 'Instrument Serif', Georgia, serif   /* display only: page titles, hero numbers */
--ff-sans:  'Inter', system-ui, sans-serif       /* UI, body */
--ff-mono:  'IBM Plex Mono', ui-monospace, monospace   /* data, IDs, codes, labels */

/* Geometry */
--radius:    6px
--radius-lg: 10px
```

### Dark theme

A dark variant exists; the designer opted to keep the light clay palette as default since it matches the institutional feel best.

```
[data-theme="dark"] {
  --bg:        #111110
  --surface:   #1A1A18
  --surface-2: #181816
  --ink:       #F2EEE2
  --ink-2:     #D7D2C2
  --ink-3:     #A29D8B
  --ink-4:     #6F6B5E
  --line:      #2A2925
  --line-2:    #242320
  --accent:    #E07A3E   /* lifted terracotta for dark */
}
```

### Density

Two densities supported via `[data-density]` attribute on `<body>`: `comfortable` (default) and `compact`. Compact reduces card padding, row height, and font sizes by ~15%.

### Typography scale

- **Page title** (hero): Instrument Serif, 34px, line-height 1.08, -0.01em tracking
- **Section/card title**: Inter, 13px, weight 500, tracking 0
- **Body**: Inter, 12.5–13px, line-height 1.5
- **Data / IDs / labels**: IBM Plex Mono, 10–11px, uppercase, letter-spacing .06–.08em, color var(--ink-3) or --ink-4
- **Eyebrow**: IBM Plex Mono, 10px, uppercase, with a 4px terracotta dot bullet

### Iconography

All icons are 1.4px stroke lucide-style line icons. Keep them in an `Icon` component that accepts `name` and `size` props. The full set used: `home, stack, clock, alert, check, building, user, settings, bell, search, plus, upload, download, doc, sparkle, zap, book, chev`.

### Imagery

No photographs or illustrations. The product is deliberately text-forward. The only "visual" elements are the document preview (rendered as styled HTML pages), the brand mark (a terracotta rectangle with two cream columns evoking file tabs), and data visualizations.

## App shell

- **Top bar** (64px): brand mark + name on left, facility switcher pill, flexible gap, global ⌘K search bar, bell icon, settings icon, user avatar
- **Sidebar** (240px): Workspace section, Facilities section, Intelligence section (Premium-gated), spacer, Admin section, Plan card at bottom
- **Main**: scrolling content area with 32px padding, max-width 1280px

Sidebar sections use a small uppercase IBM Plex Mono h5 label. Nav items are 32px tall with icon + label + optional count badge. Active state uses `var(--accent-soft)` background with a 3px terracotta left border.

## Screens

### 1. Command Center (Dashboard) — `view='dashboard'`

**Purpose:** Morning standup view. "Everything you signed. What renews. What needs action this month."

**Layout** (top to bottom):
- Page header: eyebrow "Operations overview · {today}", hero title "Command center", subtitle, right-side actions "View all contracts" + "Upload documents" (primary terracotta)
- **4-column KPI row**:
  - Active contracts (count)
  - Renew in 60 days (count, terracotta if >0)
  - Needs attention (count, urgent/notice breakdown)
  - Tracked annual value ($ in serif)
- **2-column grid**:
  - **Left (wider)**: "Upcoming renewals & notice windows" card — timeline-style list of next 6 contracts by end date, each row shows date, counterparty + category, days remaining with urgency coloring
  - **Right**: "Alerts" card — scrollable list, severity-coded (danger/warn/info), each with Snooze + Acknowledge
- Full-width: **"Obligations this month"** card (list with owner avatars and due dates) + **"Recent activity"** card (timeline)
- Premium teaser card at bottom: "Am I getting paid fairly?" with locked overlay if `tier !== 'premium'`

### 2. Contracts — `view='contracts'`

**Purpose:** The repository. All contracts filterable, searchable, sortable.

**Layout:**
- Page header with count subtitle
- **Filter bar** (sticky-ish row): search input, category pills (All / Payer / Vendor / Lease / Employee / Processor / Other), status pills (All / Active / Renewing / Expired), facility filter pill
- **Table** with columns: ID · Name · Counterparty · Category · Facility · End date + days remaining · Renewal · Annual value · Status chip
  - Row height 40px, alternating `surface`/`surface-2`
  - Click row → navigate to detail view
  - Mono font for ID, dates, values
- Empty state if filters return nothing

### 3. Contract Detail — `view='detail'`

**Purpose:** The most important screen. Document + extracted fields + AI guidance + accountability.

**Layout** (top to bottom):
1. **Breadcrumb**: Command center › Contracts › {contract ID}
2. **Page head**:
   - Eyebrow: ID · category · facility
   - **Title** (24px serif, line-height 1.3): counterparty initial avatar + contract name
   - **Meta row**: status chip + END {date} + RENEWAL {type} + NOTICE {period} + OWNER {name} + VALUE {$}/yr. Each metadata label is a small mono kbd-style chip.
   - **Right side**: Original (download) / Set alert / Start renegotiation (primary)
3. **Alert banner** (if flags exist): terracotta soft background, 3px left bar, title + body, Acknowledge button
4. **Split view** (main area, 60/40 columns):
   - **Document pane (left)**: PDF-like rendering with serif body, highlighted clauses that respond to clicks from the right pane
   - **Extracted pane (right)**: tabs (Universal fields / Payer-specific / Dates & obligations), then rows of `label · input · confidence`. Low-confidence fields flagged in terracotta. Each row with an anchor can "↗ jump to source clause"
5. **Negotiation Playbook** card (AI): priority-sorted levers, each with:
   - Priority chip (HIGH/MED)
   - Lever title + detail + "Ask →" recommendation
   - Confidence % with serif number + progress bar
   - Top strip: Posture / Window / Total opportunity
   - Footer actions: Export brief / Draft email / Start renegotiation
6. **Ownership & activity** card (2 columns):
   - **Left (240px)**: Accountable people with avatar, role, owner tag; "+ Assign collaborator"; "Acknowledgments" list with per-person status
   - **Right**: timeline with colored-dot markers for AI / ack / assign / version / created events, plus a composer for notes with @mentions and # links

### 4. Extraction Review — `view='review'`

**Purpose:** Post-upload QA queue. Confirm or correct AI-extracted fields before contracts go live.

**Layout:**
- Page head: "{N} contracts awaiting review", "Review extracted fields"
- **2-column**: left queue (280px) + right review panel
  - Queue items show: name, ID · category · age, confidence bar, flagged count in terracotta
  - Selected item: terracotta soft background + 3px left border
- **Review panel**: overall-confidence strip + per-field rows (label/source · editable input · confidence badge · Confirm/Flag buttons)
- **Assign owner** row: horizontal pill group
- Footer actions: Save draft / Skip → / Approve & continue (primary)

### 5. Rate Visibility / Benchmarks (Premium) — `view='rates'` or `view='benchmarks'`

**Purpose:** The killer feature. "Am I getting paid fairly?"

On Basic tier: shows a skeleton + lock overlay with "Preview Premium" button.

On Premium:
- Page head with Premium badge in breadcrumb
- **4 KPIs**: Annualized opportunity (terracotta) / CPTs tracked / Peer ASCs in benchmark / Data source (serves as provenance: "PayerPrice · MRF · TiC")
- **Selector bar**: CPT dropdown (with description), Site of service, Geography, CMS reference on the right
- **Benchmark chart card**: custom bars — for each of the 5 payers, a horizontal p10–p90 range band, regional median tick, CMS dashed reference line, and a colored dot for your contracted rate. Legend above.
- **Variance table**: sortable rows with Payer, Contracted, Median, Δ, % diff, CMS multiple, position bar, annual impact
- **Top opportunities table**: Across all CPTs, where you're below median, sorted by annual gap $
- **Methodology** card: small-text source attribution

### 6. Upload Modal

**Purpose:** Drag-drop contracts; watch AI extract them.

**Layout:** modal (640px), drop zone with dashed border, queued files list showing per-file status (Uploading → Extracting → Ready), batch button "Review extracted fields →" that routes to the Extraction Review screen.

### 7. Alerts & Obligations full pages — `view='alerts'`, `view='obligations'`

Simple list pages, same treatment as embedded versions on the dashboard but full-width with filters.

## Interactions & behavior

- **Navigation**: single-page React state machine with a `view` string switched from sidebar clicks, table row clicks, breadcrumb clicks. Should become real routes in production (`/dashboard`, `/contracts`, `/contracts/:id`, `/review`, `/rates`, etc.).
- **Facility switcher**: changes the `facility` state; contracts/alerts/obligations should filter by it everywhere.
- **Tier toggle**: `tier: 'basic' | 'premium'` gates the Premium intelligence section of the sidebar and the Rate Visibility page.
- **Persistence**: `view`, `facility`, `contractId`, `theme`, `density`, `tier` saved to `localStorage` under key `fcc-state`.
- **Clause anchors**: clicking an extracted field with an `anchor` property highlights the corresponding `<span class="hl">` in the document pane. Two-way: clicking a highlighted clause sets the anchor active.
- **Upload simulation**: after drop, each file moves through states with timeouts (800ms upload, 2200ms extract) to simulate processing. In production this is websocket / polling from the extraction service.
- **Auto-save on edit**: editing an extracted field updates local state immediately and flashes "Saved" in the tab bar. Production: debounced PATCH to the contract.
- **Tweaks panel**: a developer-facing control surface toggled via an `__edit_mode_available` / `__activate_edit_mode` postMessage contract. Not user-facing in production.

## State management

Minimal global state:
```ts
type AppState = {
  view: 'dashboard' | 'contracts' | 'renewals' | 'alerts' | 'obligations' | 'detail' | 'review' | 'rates' | 'benchmarks';
  facility: 'all' | 'nor' | 'lkv' | 'evg' | 'pug';
  contractId: string | null;
  uploadOpen: boolean;
  tweaksOpen: boolean;
  theme: 'operator' | 'clinical' | 'dark';
  density: 'comfortable' | 'compact';
  tier: 'basic' | 'premium';
};
```

Per-contract state: extracted fields (editable), activity log, acknowledgments, alerts, obligations. Each is fetched lazily in production.

## Data shapes

See `src/data.jsx` in the bundle for the full mock structure, but the key shapes:

```ts
type Contract = {
  id: string;            // e.g. 'C-00142'
  name: string;
  category: 'payer' | 'vendor' | 'lease' | 'employee' | 'processor' | 'other';
  facility: FacilityId;
  counterparty: { name: string; initials: string };
  effective: string;     // ISO date
  end: string;
  renewal: string;       // human string: "Auto-renew 12mo" / "Option to extend"
  notice: string;        // "90 days written"
  owner: string;
  annualValue?: number;
  docPages: number;
  flags?: string[];
};

type Alert = { id, severity: 'danger'|'warn'|'info', title, body, meta, contract? };
type Obligation = { id, title, due, owner, contract?, done };
type ExtractedField = { label, value, conf: 'high'|'med'|'low', anchor?, edited? };
```

For Premium benchmarking, see `src/benchmarks.jsx` for the CPT catalog, payer list, and rate/market structures.

## Accessibility

- All interactive controls should be focusable with visible focus rings (use `outline: 2px solid var(--accent)` or equivalent)
- Color is never the only signal — status chips use text labels plus a dot
- Tables should have proper `<th scope="col">` and row click handlers should also have keyboard equivalents
- Modals should trap focus, close on Escape, restore focus to trigger

## Assets

No external assets beyond the Google Fonts: Instrument Serif, Inter (400/500/600/700), IBM Plex Mono (400/500/600). Import via `<link>` or the fonts package of your choice.

The brand mark is pure CSS (see `.brand-mark` rule): a 22px terracotta rectangle with two cream pseudo-element bars evoking file tabs.

## Files in this bundle

- `Facility Command Center.html` — entry HTML shell with all CSS inlined and script tags pointing at `src/*.jsx`
- `Facility Command Center (standalone).html` — fully self-contained single file (same app, all dependencies inlined) for offline/demo use
- `src/data.jsx` — mock contracts, alerts, obligations, facilities, extraction results
- `src/benchmarks.jsx` — CPT catalog, payer list, benchmark rates
- `src/ui.jsx` — shared primitives: Icon, Chip, statusChip, CategoryLabel, daysBetween, formatDate
- `src/dashboard.jsx` — dashboard view
- `src/contracts.jsx` — contracts list with filter bar + table
- `src/detail.jsx` — contract detail split view + negotiation playbook + activity
- `src/review.jsx` — post-upload extraction review queue
- `src/benchmark-page.jsx` — Premium rate benchmarking page + `BenchmarkBars` visualization
- `src/upload.jsx` — upload modal
- `src/tweaks.jsx` — developer tweaks panel
- `src/app.jsx` — app shell: top bar, sidebar, view router

## Questions for the product team before implementation

1. **What's the extraction backend?** The mock assumes a two-phase upload→extract flow with confidence scores per field. The API shape affects the review screen substantially.
2. **Where does benchmark data come from?** The mock references PayerPrice + MRF/TiC. Confirm the data partner and licensing before building the Premium tier.
3. **How do acknowledgments map to the auth/team model?** Currently mocked as name strings; needs to hook into real users and roles.
4. **Multi-facility permissions** — are there facility-scoped roles, or is anyone who can see the org see all facilities?
