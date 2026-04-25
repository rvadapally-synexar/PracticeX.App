# PracticeX Project Instructions

These instructions apply to the whole repository.

## Product posture

- Build PracticeX as an enterprise-grade product from day one. Delivery slices can be small, but architecture, data modeling, security posture, auditability, and migration practices should be durable.
- Treat pilots and demos as production-shaped slices, not disposable prototypes.
- Prefer data-first design: model tenancy, facilities, contracts, documents, source objects, evidence, review state, audit events, and workflow state before polishing automation.
- Use typed boundaries for all external sources, AI operations, and background jobs.

## Technology choices

- Frontend: React, TypeScript, Vite, React Router, Tailwind CSS, Radix-style primitives, TanStack Query/Table where useful.
- Backend: ASP.NET Core, PostgreSQL, modular monolith, background jobs, typed API contracts.
- Database: PostgreSQL with schema separation and enterprise migration discipline.

## PostgreSQL conventions

- All PostgreSQL identifiers must be snake_case and unquoted.
- This applies to schemas, tables, columns, indexes, constraints, enum values, views, functions, triggers, and migration history objects.
- Do not create mixed-case or quoted PostgreSQL identifiers.
- C# identifiers should remain idiomatic PascalCase/camelCase and map to snake_case at the database boundary.
- Migrations must be safe to run repeatedly through idempotent SQL scripts for deployment.

## Ingestion and connectors

- Model every source through the same ingestion pipeline: source connection -> source object -> ingestion batch -> ingestion job -> document asset -> document candidate -> review/publish.
- Local folders, Outlook, Gmail, Drive/SharePoint, Zoom transcripts, and SFTP are connectors, not separate products.
- Connectors discover candidates and import selected evidence. They must not silently create canonical contract records.

## UI direction

- Use the FCC mock as the application UX foundation and the PracticeX brand board/site as the brand skin.
- Product UI should feel calm, dense, institutional, and operator-first.
- Use warm off-white backgrounds, deep green primary identity, restrained orange accents, and serif display type only for page-level headings.

