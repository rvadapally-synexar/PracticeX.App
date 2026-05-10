-- Slice 21 (Phase 2 follow-up): renumber placeholder UUIDs to real IDs.
--
-- Earlier slices used pattern UUIDs (`11111111-...`, `22222222-...`,
-- `33333333-...`, `e1111111-...`, `51111111-...`, `a0000001-...`) for
-- the platform tenant, demo super-admin, Eagle facility, Synexar
-- facility, Eagle/Synexar tenants, and the standard role rows. Those
-- show up in URLs and audit metadata and read as obviously-fake to
-- anyone who clicks around. This migration replaces every one of them
-- with a fresh v4 UUID.
--
-- Approach: drop FK constraints, UPDATE every primary key + every
-- referencing column in lockstep, then recreate the FKs. Wrapped in a
-- single transaction so it's all-or-nothing. Idempotent on re-run —
-- guarded by a check on the platform tenant id.
--
-- Mapping (kept here because future readers will need it to interpret
-- old logs / audit metadata that quoted the placeholder ids):
--
--   '11111111-1111-1111-1111-111111111111' (platform tenant)
--     -> '02b32f45-2ad4-4aa3-865a-6150d8fd3f98'
--   'e1111111-1111-1111-1111-111111111111' (Eagle tenant)
--     -> '37bc3e79-34af-4b7a-a3b6-eb7877b4b8d6'
--   '51111111-1111-1111-1111-111111111111' (Synexar tenant)
--     -> '21dd0376-9eac-4319-815d-72e25b72bf0c'
--   '22222222-2222-2222-2222-222222222222' (demo super-admin user — Raghu)
--     -> 'ed785f04-c5a8-4539-ae4c-2f41ed002477'
--   '22222222-2222-2222-2222-222222222222' (Eagle facility — same UUID
--     as the demo user, but in org.facilities not org.users)
--     -> 'e3ca3b8f-ea5b-4dad-81c0-98d28f4f8ea7'
--   '33333333-3333-3333-3333-333333333333' (Synexar facility)
--     -> '8329f966-0be5-4c18-acf3-bcc7b5873097'
--   'a0000001-0000-0000-0000-000000000001' (super_admin role)
--     -> 'b34194f9-2f13-41eb-8cb8-ea7d171fb34c'
--   'a0000001-0000-0000-0000-000000000002' (org_admin role)
--     -> '55c4cae8-fd81-48bb-a42d-d9ce803b2e20'
--   'a0000001-0000-0000-0000-000000000003' (facility_user role)
--     -> 'c1bf53cf-4fcf-4f1d-b768-a85d7df0d060'
--   'a0000002-0000-0000-0000-000000000001' (Parag user)
--     -> '9454aada-ca00-49cb-96af-92910c3ee201'
--   'a0000003-0000-0000-0000-000000000001' (Parag role assignment)
--     -> 'da914ca9-ad73-47c0-b0ed-42dde5da54d1'
--
-- The all-facilities sentinel '00000000-0000-0000-0000-000000000000' is
-- left alone — it's Guid.Empty and not a placeholder.

DO $$
BEGIN
  -- Idempotency guard: if the platform tenant has already been renumbered,
  -- skip everything. Detection key is the OLD platform tenant id.
  IF NOT EXISTS (
    SELECT 1 FROM org.tenants
    WHERE id = '11111111-1111-1111-1111-111111111111'::uuid
  ) THEN
    RAISE NOTICE 'Renumber migration: platform tenant already absent — skipping.';
    RETURN;
  END IF;

  -- ============================================================
  -- 1. Drop FK constraints that reference the IDs we're renaming.
  -- ============================================================
  -- (Constraints are recreated at the end with the same definitions.)

  -- FKs to org.tenants(id)
  ALTER TABLE org.facilities         DROP CONSTRAINT IF EXISTS fk_facilities_tenants_tenant_id;
  ALTER TABLE org.users              DROP CONSTRAINT IF EXISTS fk_users_tenants_tenant_id;
  ALTER TABLE org.roles              DROP CONSTRAINT IF EXISTS fk_roles_tenants_tenant_id;
  ALTER TABLE org.role_assignments   DROP CONSTRAINT IF EXISTS fk_role_assignments_tenants_tenant_id;
  ALTER TABLE doc.source_connections DROP CONSTRAINT IF EXISTS fk_source_connections_tenants_tenant_id;
  ALTER TABLE doc.source_objects     DROP CONSTRAINT IF EXISTS fk_source_objects_tenants_tenant_id;
  ALTER TABLE doc.document_assets    DROP CONSTRAINT IF EXISTS fk_document_assets_tenants_tenant_id;
  ALTER TABLE doc.document_candidates DROP CONSTRAINT IF EXISTS fk_document_candidates_tenants_tenant_id;
  ALTER TABLE doc.ingestion_batches  DROP CONSTRAINT IF EXISTS fk_ingestion_batches_tenants_tenant_id;
  ALTER TABLE doc.ingestion_jobs     DROP CONSTRAINT IF EXISTS fk_ingestion_jobs_tenants_tenant_id;
  ALTER TABLE evidence.evidence_links DROP CONSTRAINT IF EXISTS fk_evidence_links_tenants_tenant_id;
  ALTER TABLE workflow.review_tasks  DROP CONSTRAINT IF EXISTS fk_review_tasks_tenants_tenant_id;
  ALTER TABLE contract.contracts     DROP CONSTRAINT IF EXISTS fk_contracts_tenants_tenant_id;
  ALTER TABLE contract.contract_fields DROP CONSTRAINT IF EXISTS fk_contract_fields_tenants_tenant_id;
  ALTER TABLE contract.counterparties DROP CONSTRAINT IF EXISTS fk_counterparties_tenants_tenant_id;
  ALTER TABLE audit.audit_events     DROP CONSTRAINT IF EXISTS fk_audit_events_tenants_tenant_id;

  -- FKs to org.users(id)
  ALTER TABLE org.role_assignments    DROP CONSTRAINT IF EXISTS fk_role_assignments_users_user_id;
  ALTER TABLE doc.ingestion_batches   DROP CONSTRAINT IF EXISTS fk_ingestion_batches_users_created_by_user_id;
  ALTER TABLE workflow.review_tasks   DROP CONSTRAINT IF EXISTS fk_review_tasks_users_assigned_to_user_id;
  ALTER TABLE contract.contracts      DROP CONSTRAINT IF EXISTS fk_contracts_users_owner_user_id;

  -- FKs to org.facilities(id)
  ALTER TABLE org.role_assignments     DROP CONSTRAINT IF EXISTS fk_role_assignments_facilities_facility_id;
  ALTER TABLE doc.document_candidates  DROP CONSTRAINT IF EXISTS fk_document_candidates_facilities_facility_hint_id;
  ALTER TABLE contract.contracts       DROP CONSTRAINT IF EXISTS fk_contracts_facilities_facility_id;

  -- FKs to org.roles(id)
  ALTER TABLE org.role_assignments     DROP CONSTRAINT IF EXISTS fk_role_assignments_roles_role_id;

  -- ============================================================
  -- 2. Update primary keys.
  -- ============================================================

  -- Tenants
  UPDATE org.tenants SET id = '02b32f45-2ad4-4aa3-865a-6150d8fd3f98'::uuid
    WHERE id = '11111111-1111-1111-1111-111111111111'::uuid;
  UPDATE org.tenants SET id = '37bc3e79-34af-4b7a-a3b6-eb7877b4b8d6'::uuid
    WHERE id = 'e1111111-1111-1111-1111-111111111111'::uuid;
  UPDATE org.tenants SET id = '21dd0376-9eac-4319-815d-72e25b72bf0c'::uuid
    WHERE id = '51111111-1111-1111-1111-111111111111'::uuid;

  -- Users
  UPDATE org.users SET id = 'ed785f04-c5a8-4539-ae4c-2f41ed002477'::uuid
    WHERE id = '22222222-2222-2222-2222-222222222222'::uuid;
  UPDATE org.users SET id = '9454aada-ca00-49cb-96af-92910c3ee201'::uuid
    WHERE id = 'a0000002-0000-0000-0000-000000000001'::uuid;

  -- Facilities
  UPDATE org.facilities SET id = 'e3ca3b8f-ea5b-4dad-81c0-98d28f4f8ea7'::uuid
    WHERE id = '22222222-2222-2222-2222-222222222222'::uuid;
  UPDATE org.facilities SET id = '8329f966-0be5-4c18-acf3-bcc7b5873097'::uuid
    WHERE id = '33333333-3333-3333-3333-333333333333'::uuid;

  -- Roles
  UPDATE org.roles SET id = 'b34194f9-2f13-41eb-8cb8-ea7d171fb34c'::uuid
    WHERE id = 'a0000001-0000-0000-0000-000000000001'::uuid;
  UPDATE org.roles SET id = '55c4cae8-fd81-48bb-a42d-d9ce803b2e20'::uuid
    WHERE id = 'a0000001-0000-0000-0000-000000000002'::uuid;
  UPDATE org.roles SET id = 'c1bf53cf-4fcf-4f1d-b768-a85d7df0d060'::uuid
    WHERE id = 'a0000001-0000-0000-0000-000000000003'::uuid;

  -- Role assignments
  UPDATE org.role_assignments SET id = 'da914ca9-ad73-47c0-b0ed-42dde5da54d1'::uuid
    WHERE id = 'a0000003-0000-0000-0000-000000000001'::uuid;

  -- ============================================================
  -- 3. Update referencing columns.
  -- ============================================================

  -- ----- tenant_id rewrites -----

  PERFORM 1; -- (anchor for readability)

  -- platform tenant
  UPDATE org.facilities         SET tenant_id = '02b32f45-2ad4-4aa3-865a-6150d8fd3f98'::uuid WHERE tenant_id = '11111111-1111-1111-1111-111111111111'::uuid;
  UPDATE org.users              SET tenant_id = '02b32f45-2ad4-4aa3-865a-6150d8fd3f98'::uuid WHERE tenant_id = '11111111-1111-1111-1111-111111111111'::uuid;
  UPDATE org.roles              SET tenant_id = '02b32f45-2ad4-4aa3-865a-6150d8fd3f98'::uuid WHERE tenant_id = '11111111-1111-1111-1111-111111111111'::uuid;
  UPDATE org.role_assignments   SET tenant_id = '02b32f45-2ad4-4aa3-865a-6150d8fd3f98'::uuid WHERE tenant_id = '11111111-1111-1111-1111-111111111111'::uuid;
  UPDATE doc.source_connections SET tenant_id = '02b32f45-2ad4-4aa3-865a-6150d8fd3f98'::uuid WHERE tenant_id = '11111111-1111-1111-1111-111111111111'::uuid;
  UPDATE doc.source_objects     SET tenant_id = '02b32f45-2ad4-4aa3-865a-6150d8fd3f98'::uuid WHERE tenant_id = '11111111-1111-1111-1111-111111111111'::uuid;
  UPDATE doc.document_assets    SET tenant_id = '02b32f45-2ad4-4aa3-865a-6150d8fd3f98'::uuid WHERE tenant_id = '11111111-1111-1111-1111-111111111111'::uuid;
  UPDATE doc.document_candidates SET tenant_id = '02b32f45-2ad4-4aa3-865a-6150d8fd3f98'::uuid WHERE tenant_id = '11111111-1111-1111-1111-111111111111'::uuid;
  UPDATE doc.ingestion_batches  SET tenant_id = '02b32f45-2ad4-4aa3-865a-6150d8fd3f98'::uuid WHERE tenant_id = '11111111-1111-1111-1111-111111111111'::uuid;
  UPDATE doc.ingestion_jobs     SET tenant_id = '02b32f45-2ad4-4aa3-865a-6150d8fd3f98'::uuid WHERE tenant_id = '11111111-1111-1111-1111-111111111111'::uuid;
  UPDATE evidence.evidence_links SET tenant_id = '02b32f45-2ad4-4aa3-865a-6150d8fd3f98'::uuid WHERE tenant_id = '11111111-1111-1111-1111-111111111111'::uuid;
  UPDATE workflow.review_tasks  SET tenant_id = '02b32f45-2ad4-4aa3-865a-6150d8fd3f98'::uuid WHERE tenant_id = '11111111-1111-1111-1111-111111111111'::uuid;
  UPDATE contract.contracts     SET tenant_id = '02b32f45-2ad4-4aa3-865a-6150d8fd3f98'::uuid WHERE tenant_id = '11111111-1111-1111-1111-111111111111'::uuid;
  UPDATE contract.contract_fields SET tenant_id = '02b32f45-2ad4-4aa3-865a-6150d8fd3f98'::uuid WHERE tenant_id = '11111111-1111-1111-1111-111111111111'::uuid;
  UPDATE contract.counterparties SET tenant_id = '02b32f45-2ad4-4aa3-865a-6150d8fd3f98'::uuid WHERE tenant_id = '11111111-1111-1111-1111-111111111111'::uuid;
  UPDATE audit.audit_events     SET tenant_id = '02b32f45-2ad4-4aa3-865a-6150d8fd3f98'::uuid WHERE tenant_id = '11111111-1111-1111-1111-111111111111'::uuid;
  UPDATE doc.portfolio_briefs   SET tenant_id = '02b32f45-2ad4-4aa3-865a-6150d8fd3f98'::uuid WHERE tenant_id = '11111111-1111-1111-1111-111111111111'::uuid;
  UPDATE doc.counsel_briefs     SET tenant_id = '02b32f45-2ad4-4aa3-865a-6150d8fd3f98'::uuid WHERE tenant_id = '11111111-1111-1111-1111-111111111111'::uuid;

  -- Eagle tenant
  UPDATE org.facilities         SET tenant_id = '37bc3e79-34af-4b7a-a3b6-eb7877b4b8d6'::uuid WHERE tenant_id = 'e1111111-1111-1111-1111-111111111111'::uuid;
  UPDATE org.users              SET tenant_id = '37bc3e79-34af-4b7a-a3b6-eb7877b4b8d6'::uuid WHERE tenant_id = 'e1111111-1111-1111-1111-111111111111'::uuid;
  UPDATE org.roles              SET tenant_id = '37bc3e79-34af-4b7a-a3b6-eb7877b4b8d6'::uuid WHERE tenant_id = 'e1111111-1111-1111-1111-111111111111'::uuid;
  UPDATE org.role_assignments   SET tenant_id = '37bc3e79-34af-4b7a-a3b6-eb7877b4b8d6'::uuid WHERE tenant_id = 'e1111111-1111-1111-1111-111111111111'::uuid;
  UPDATE doc.source_connections SET tenant_id = '37bc3e79-34af-4b7a-a3b6-eb7877b4b8d6'::uuid WHERE tenant_id = 'e1111111-1111-1111-1111-111111111111'::uuid;
  UPDATE doc.source_objects     SET tenant_id = '37bc3e79-34af-4b7a-a3b6-eb7877b4b8d6'::uuid WHERE tenant_id = 'e1111111-1111-1111-1111-111111111111'::uuid;
  UPDATE doc.document_assets    SET tenant_id = '37bc3e79-34af-4b7a-a3b6-eb7877b4b8d6'::uuid WHERE tenant_id = 'e1111111-1111-1111-1111-111111111111'::uuid;
  UPDATE doc.document_candidates SET tenant_id = '37bc3e79-34af-4b7a-a3b6-eb7877b4b8d6'::uuid WHERE tenant_id = 'e1111111-1111-1111-1111-111111111111'::uuid;
  UPDATE doc.ingestion_batches  SET tenant_id = '37bc3e79-34af-4b7a-a3b6-eb7877b4b8d6'::uuid WHERE tenant_id = 'e1111111-1111-1111-1111-111111111111'::uuid;
  UPDATE doc.ingestion_jobs     SET tenant_id = '37bc3e79-34af-4b7a-a3b6-eb7877b4b8d6'::uuid WHERE tenant_id = 'e1111111-1111-1111-1111-111111111111'::uuid;
  UPDATE evidence.evidence_links SET tenant_id = '37bc3e79-34af-4b7a-a3b6-eb7877b4b8d6'::uuid WHERE tenant_id = 'e1111111-1111-1111-1111-111111111111'::uuid;
  UPDATE workflow.review_tasks  SET tenant_id = '37bc3e79-34af-4b7a-a3b6-eb7877b4b8d6'::uuid WHERE tenant_id = 'e1111111-1111-1111-1111-111111111111'::uuid;
  UPDATE contract.contracts     SET tenant_id = '37bc3e79-34af-4b7a-a3b6-eb7877b4b8d6'::uuid WHERE tenant_id = 'e1111111-1111-1111-1111-111111111111'::uuid;
  UPDATE contract.contract_fields SET tenant_id = '37bc3e79-34af-4b7a-a3b6-eb7877b4b8d6'::uuid WHERE tenant_id = 'e1111111-1111-1111-1111-111111111111'::uuid;
  UPDATE contract.counterparties SET tenant_id = '37bc3e79-34af-4b7a-a3b6-eb7877b4b8d6'::uuid WHERE tenant_id = 'e1111111-1111-1111-1111-111111111111'::uuid;
  UPDATE audit.audit_events     SET tenant_id = '37bc3e79-34af-4b7a-a3b6-eb7877b4b8d6'::uuid WHERE tenant_id = 'e1111111-1111-1111-1111-111111111111'::uuid;
  UPDATE doc.portfolio_briefs   SET tenant_id = '37bc3e79-34af-4b7a-a3b6-eb7877b4b8d6'::uuid WHERE tenant_id = 'e1111111-1111-1111-1111-111111111111'::uuid;
  UPDATE doc.counsel_briefs     SET tenant_id = '37bc3e79-34af-4b7a-a3b6-eb7877b4b8d6'::uuid WHERE tenant_id = 'e1111111-1111-1111-1111-111111111111'::uuid;

  -- Synexar tenant
  UPDATE org.facilities         SET tenant_id = '21dd0376-9eac-4319-815d-72e25b72bf0c'::uuid WHERE tenant_id = '51111111-1111-1111-1111-111111111111'::uuid;
  UPDATE org.users              SET tenant_id = '21dd0376-9eac-4319-815d-72e25b72bf0c'::uuid WHERE tenant_id = '51111111-1111-1111-1111-111111111111'::uuid;
  UPDATE org.roles              SET tenant_id = '21dd0376-9eac-4319-815d-72e25b72bf0c'::uuid WHERE tenant_id = '51111111-1111-1111-1111-111111111111'::uuid;
  UPDATE org.role_assignments   SET tenant_id = '21dd0376-9eac-4319-815d-72e25b72bf0c'::uuid WHERE tenant_id = '51111111-1111-1111-1111-111111111111'::uuid;
  UPDATE doc.source_connections SET tenant_id = '21dd0376-9eac-4319-815d-72e25b72bf0c'::uuid WHERE tenant_id = '51111111-1111-1111-1111-111111111111'::uuid;
  UPDATE doc.source_objects     SET tenant_id = '21dd0376-9eac-4319-815d-72e25b72bf0c'::uuid WHERE tenant_id = '51111111-1111-1111-1111-111111111111'::uuid;
  UPDATE doc.document_assets    SET tenant_id = '21dd0376-9eac-4319-815d-72e25b72bf0c'::uuid WHERE tenant_id = '51111111-1111-1111-1111-111111111111'::uuid;
  UPDATE doc.document_candidates SET tenant_id = '21dd0376-9eac-4319-815d-72e25b72bf0c'::uuid WHERE tenant_id = '51111111-1111-1111-1111-111111111111'::uuid;
  UPDATE doc.ingestion_batches  SET tenant_id = '21dd0376-9eac-4319-815d-72e25b72bf0c'::uuid WHERE tenant_id = '51111111-1111-1111-1111-111111111111'::uuid;
  UPDATE doc.ingestion_jobs     SET tenant_id = '21dd0376-9eac-4319-815d-72e25b72bf0c'::uuid WHERE tenant_id = '51111111-1111-1111-1111-111111111111'::uuid;
  UPDATE evidence.evidence_links SET tenant_id = '21dd0376-9eac-4319-815d-72e25b72bf0c'::uuid WHERE tenant_id = '51111111-1111-1111-1111-111111111111'::uuid;
  UPDATE workflow.review_tasks  SET tenant_id = '21dd0376-9eac-4319-815d-72e25b72bf0c'::uuid WHERE tenant_id = '51111111-1111-1111-1111-111111111111'::uuid;
  UPDATE contract.contracts     SET tenant_id = '21dd0376-9eac-4319-815d-72e25b72bf0c'::uuid WHERE tenant_id = '51111111-1111-1111-1111-111111111111'::uuid;
  UPDATE contract.contract_fields SET tenant_id = '21dd0376-9eac-4319-815d-72e25b72bf0c'::uuid WHERE tenant_id = '51111111-1111-1111-1111-111111111111'::uuid;
  UPDATE contract.counterparties SET tenant_id = '21dd0376-9eac-4319-815d-72e25b72bf0c'::uuid WHERE tenant_id = '51111111-1111-1111-1111-111111111111'::uuid;
  UPDATE audit.audit_events     SET tenant_id = '21dd0376-9eac-4319-815d-72e25b72bf0c'::uuid WHERE tenant_id = '51111111-1111-1111-1111-111111111111'::uuid;
  UPDATE doc.portfolio_briefs   SET tenant_id = '21dd0376-9eac-4319-815d-72e25b72bf0c'::uuid WHERE tenant_id = '51111111-1111-1111-1111-111111111111'::uuid;
  UPDATE doc.counsel_briefs     SET tenant_id = '21dd0376-9eac-4319-815d-72e25b72bf0c'::uuid WHERE tenant_id = '51111111-1111-1111-1111-111111111111'::uuid;

  -- ----- user_id rewrites -----

  -- Demo super-admin (Raghu)
  UPDATE org.role_assignments  SET user_id            = 'ed785f04-c5a8-4539-ae4c-2f41ed002477'::uuid WHERE user_id            = '22222222-2222-2222-2222-222222222222'::uuid;
  UPDATE doc.ingestion_batches SET created_by_user_id = 'ed785f04-c5a8-4539-ae4c-2f41ed002477'::uuid WHERE created_by_user_id = '22222222-2222-2222-2222-222222222222'::uuid;
  UPDATE doc.source_connections SET created_by_user_id = 'ed785f04-c5a8-4539-ae4c-2f41ed002477'::uuid WHERE created_by_user_id = '22222222-2222-2222-2222-222222222222'::uuid;
  UPDATE workflow.review_tasks SET assigned_to_user_id = 'ed785f04-c5a8-4539-ae4c-2f41ed002477'::uuid WHERE assigned_to_user_id = '22222222-2222-2222-2222-222222222222'::uuid;
  UPDATE contract.contracts    SET owner_user_id      = 'ed785f04-c5a8-4539-ae4c-2f41ed002477'::uuid WHERE owner_user_id      = '22222222-2222-2222-2222-222222222222'::uuid;
  UPDATE audit.audit_events    SET actor_id           = 'ed785f04-c5a8-4539-ae4c-2f41ed002477'::uuid WHERE actor_id           = '22222222-2222-2222-2222-222222222222'::uuid;

  -- Parag user
  UPDATE org.role_assignments  SET user_id            = '9454aada-ca00-49cb-96af-92910c3ee201'::uuid WHERE user_id            = 'a0000002-0000-0000-0000-000000000001'::uuid;
  UPDATE doc.ingestion_batches SET created_by_user_id = '9454aada-ca00-49cb-96af-92910c3ee201'::uuid WHERE created_by_user_id = 'a0000002-0000-0000-0000-000000000001'::uuid;
  UPDATE doc.source_connections SET created_by_user_id = '9454aada-ca00-49cb-96af-92910c3ee201'::uuid WHERE created_by_user_id = 'a0000002-0000-0000-0000-000000000001'::uuid;
  UPDATE workflow.review_tasks SET assigned_to_user_id = '9454aada-ca00-49cb-96af-92910c3ee201'::uuid WHERE assigned_to_user_id = 'a0000002-0000-0000-0000-000000000001'::uuid;
  UPDATE contract.contracts    SET owner_user_id      = '9454aada-ca00-49cb-96af-92910c3ee201'::uuid WHERE owner_user_id      = 'a0000002-0000-0000-0000-000000000001'::uuid;
  UPDATE audit.audit_events    SET actor_id           = '9454aada-ca00-49cb-96af-92910c3ee201'::uuid WHERE actor_id           = 'a0000002-0000-0000-0000-000000000001'::uuid;

  -- ----- facility_id / facility_hint_id rewrites -----

  -- Eagle facility
  UPDATE org.role_assignments    SET facility_id      = 'e3ca3b8f-ea5b-4dad-81c0-98d28f4f8ea7'::uuid WHERE facility_id      = '22222222-2222-2222-2222-222222222222'::uuid;
  UPDATE doc.document_candidates SET facility_hint_id = 'e3ca3b8f-ea5b-4dad-81c0-98d28f4f8ea7'::uuid WHERE facility_hint_id = '22222222-2222-2222-2222-222222222222'::uuid;
  UPDATE contract.contracts      SET facility_id      = 'e3ca3b8f-ea5b-4dad-81c0-98d28f4f8ea7'::uuid WHERE facility_id      = '22222222-2222-2222-2222-222222222222'::uuid;
  UPDATE doc.portfolio_briefs    SET facility_id      = 'e3ca3b8f-ea5b-4dad-81c0-98d28f4f8ea7'::uuid WHERE facility_id      = '22222222-2222-2222-2222-222222222222'::uuid;

  -- Synexar facility
  UPDATE org.role_assignments    SET facility_id      = '8329f966-0be5-4c18-acf3-bcc7b5873097'::uuid WHERE facility_id      = '33333333-3333-3333-3333-333333333333'::uuid;
  UPDATE doc.document_candidates SET facility_hint_id = '8329f966-0be5-4c18-acf3-bcc7b5873097'::uuid WHERE facility_hint_id = '33333333-3333-3333-3333-333333333333'::uuid;
  UPDATE contract.contracts      SET facility_id      = '8329f966-0be5-4c18-acf3-bcc7b5873097'::uuid WHERE facility_id      = '33333333-3333-3333-3333-333333333333'::uuid;
  UPDATE doc.portfolio_briefs    SET facility_id      = '8329f966-0be5-4c18-acf3-bcc7b5873097'::uuid WHERE facility_id      = '33333333-3333-3333-3333-333333333333'::uuid;

  -- ----- role_id rewrites -----
  UPDATE org.role_assignments SET role_id = 'b34194f9-2f13-41eb-8cb8-ea7d171fb34c'::uuid WHERE role_id = 'a0000001-0000-0000-0000-000000000001'::uuid;
  UPDATE org.role_assignments SET role_id = '55c4cae8-fd81-48bb-a42d-d9ce803b2e20'::uuid WHERE role_id = 'a0000001-0000-0000-0000-000000000002'::uuid;
  UPDATE org.role_assignments SET role_id = 'c1bf53cf-4fcf-4f1d-b768-a85d7df0d060'::uuid WHERE role_id = 'a0000001-0000-0000-0000-000000000003'::uuid;

  -- ----- audit_events.resource_id rewrites -----
  --
  -- audit_events.resource_id is polymorphic (resource_type tells you the
  -- target table). When a row references a renamed entity, update it so
  -- audit log -> entity navigation keeps working.

  -- resource_type='tenant' rows pointing at the platform/Eagle/Synexar tenants
  UPDATE audit.audit_events SET resource_id = '02b32f45-2ad4-4aa3-865a-6150d8fd3f98'::uuid
    WHERE resource_type = 'tenant' AND resource_id = '11111111-1111-1111-1111-111111111111'::uuid;
  UPDATE audit.audit_events SET resource_id = '37bc3e79-34af-4b7a-a3b6-eb7877b4b8d6'::uuid
    WHERE resource_type = 'tenant' AND resource_id = 'e1111111-1111-1111-1111-111111111111'::uuid;
  UPDATE audit.audit_events SET resource_id = '21dd0376-9eac-4319-815d-72e25b72bf0c'::uuid
    WHERE resource_type = 'tenant' AND resource_id = '51111111-1111-1111-1111-111111111111'::uuid;

  -- resource_type='user' rows pointing at the demo super-admin / Parag
  UPDATE audit.audit_events SET resource_id = 'ed785f04-c5a8-4539-ae4c-2f41ed002477'::uuid
    WHERE resource_type = 'user' AND resource_id = '22222222-2222-2222-2222-222222222222'::uuid;
  UPDATE audit.audit_events SET resource_id = '9454aada-ca00-49cb-96af-92910c3ee201'::uuid
    WHERE resource_type = 'user' AND resource_id = 'a0000002-0000-0000-0000-000000000001'::uuid;

  -- resource_type='facility' rows pointing at Eagle / Synexar facilities
  UPDATE audit.audit_events SET resource_id = 'e3ca3b8f-ea5b-4dad-81c0-98d28f4f8ea7'::uuid
    WHERE resource_type = 'facility' AND resource_id = '22222222-2222-2222-2222-222222222222'::uuid;
  UPDATE audit.audit_events SET resource_id = '8329f966-0be5-4c18-acf3-bcc7b5873097'::uuid
    WHERE resource_type = 'facility' AND resource_id = '33333333-3333-3333-3333-333333333333'::uuid;

  -- ============================================================
  -- 4. Recreate FK constraints with the original semantics.
  -- ============================================================

  ALTER TABLE org.facilities         ADD CONSTRAINT fk_facilities_tenants_tenant_id          FOREIGN KEY (tenant_id) REFERENCES org.tenants (id) ON DELETE RESTRICT;
  ALTER TABLE org.users              ADD CONSTRAINT fk_users_tenants_tenant_id               FOREIGN KEY (tenant_id) REFERENCES org.tenants (id) ON DELETE RESTRICT;
  ALTER TABLE org.roles              ADD CONSTRAINT fk_roles_tenants_tenant_id               FOREIGN KEY (tenant_id) REFERENCES org.tenants (id) ON DELETE RESTRICT;
  ALTER TABLE org.role_assignments   ADD CONSTRAINT fk_role_assignments_tenants_tenant_id    FOREIGN KEY (tenant_id) REFERENCES org.tenants (id) ON DELETE RESTRICT;
  ALTER TABLE doc.source_connections ADD CONSTRAINT fk_source_connections_tenants_tenant_id  FOREIGN KEY (tenant_id) REFERENCES org.tenants (id) ON DELETE RESTRICT;
  ALTER TABLE doc.source_objects     ADD CONSTRAINT fk_source_objects_tenants_tenant_id      FOREIGN KEY (tenant_id) REFERENCES org.tenants (id) ON DELETE RESTRICT;
  ALTER TABLE doc.document_assets    ADD CONSTRAINT fk_document_assets_tenants_tenant_id     FOREIGN KEY (tenant_id) REFERENCES org.tenants (id) ON DELETE RESTRICT;
  ALTER TABLE doc.document_candidates ADD CONSTRAINT fk_document_candidates_tenants_tenant_id FOREIGN KEY (tenant_id) REFERENCES org.tenants (id) ON DELETE RESTRICT;
  ALTER TABLE doc.ingestion_batches  ADD CONSTRAINT fk_ingestion_batches_tenants_tenant_id   FOREIGN KEY (tenant_id) REFERENCES org.tenants (id) ON DELETE RESTRICT;
  ALTER TABLE doc.ingestion_jobs     ADD CONSTRAINT fk_ingestion_jobs_tenants_tenant_id      FOREIGN KEY (tenant_id) REFERENCES org.tenants (id) ON DELETE RESTRICT;
  ALTER TABLE evidence.evidence_links ADD CONSTRAINT fk_evidence_links_tenants_tenant_id     FOREIGN KEY (tenant_id) REFERENCES org.tenants (id) ON DELETE RESTRICT;
  ALTER TABLE workflow.review_tasks  ADD CONSTRAINT fk_review_tasks_tenants_tenant_id        FOREIGN KEY (tenant_id) REFERENCES org.tenants (id) ON DELETE RESTRICT;
  ALTER TABLE contract.contracts     ADD CONSTRAINT fk_contracts_tenants_tenant_id           FOREIGN KEY (tenant_id) REFERENCES org.tenants (id) ON DELETE RESTRICT;
  ALTER TABLE contract.contract_fields ADD CONSTRAINT fk_contract_fields_tenants_tenant_id   FOREIGN KEY (tenant_id) REFERENCES org.tenants (id) ON DELETE RESTRICT;
  ALTER TABLE contract.counterparties ADD CONSTRAINT fk_counterparties_tenants_tenant_id     FOREIGN KEY (tenant_id) REFERENCES org.tenants (id) ON DELETE RESTRICT;
  ALTER TABLE audit.audit_events     ADD CONSTRAINT fk_audit_events_tenants_tenant_id        FOREIGN KEY (tenant_id) REFERENCES org.tenants (id) ON DELETE RESTRICT;

  ALTER TABLE org.role_assignments    ADD CONSTRAINT fk_role_assignments_users_user_id              FOREIGN KEY (user_id) REFERENCES org.users (id) ON DELETE RESTRICT;
  ALTER TABLE doc.ingestion_batches   ADD CONSTRAINT fk_ingestion_batches_users_created_by_user_id  FOREIGN KEY (created_by_user_id) REFERENCES org.users (id) ON DELETE RESTRICT;
  ALTER TABLE workflow.review_tasks   ADD CONSTRAINT fk_review_tasks_users_assigned_to_user_id      FOREIGN KEY (assigned_to_user_id) REFERENCES org.users (id) ON DELETE RESTRICT;
  ALTER TABLE contract.contracts      ADD CONSTRAINT fk_contracts_users_owner_user_id               FOREIGN KEY (owner_user_id) REFERENCES org.users (id) ON DELETE RESTRICT;

  ALTER TABLE org.role_assignments     ADD CONSTRAINT fk_role_assignments_facilities_facility_id            FOREIGN KEY (facility_id) REFERENCES org.facilities (id) ON DELETE RESTRICT;
  ALTER TABLE doc.document_candidates  ADD CONSTRAINT fk_document_candidates_facilities_facility_hint_id    FOREIGN KEY (facility_hint_id) REFERENCES org.facilities (id) ON DELETE RESTRICT;
  ALTER TABLE contract.contracts       ADD CONSTRAINT fk_contracts_facilities_facility_id                   FOREIGN KEY (facility_id) REFERENCES org.facilities (id) ON DELETE RESTRICT;

  ALTER TABLE org.role_assignments     ADD CONSTRAINT fk_role_assignments_roles_role_id  FOREIGN KEY (role_id) REFERENCES org.roles (id) ON DELETE RESTRICT;

  RAISE NOTICE 'Renumber migration: complete.';
END $$;
