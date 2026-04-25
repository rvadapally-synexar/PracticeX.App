using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PracticeX.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class initial_enterprise_foundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.EnsureSchema(
                name: "contract");

            migrationBuilder.EnsureSchema(
                name: "doc");

            migrationBuilder.EnsureSchema(
                name: "evidence");

            migrationBuilder.EnsureSchema(
                name: "org");

            migrationBuilder.EnsureSchema(
                name: "workflow");

            migrationBuilder.CreateTable(
                name: "tenants",
                schema: "org",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    data_region = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    baa_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_events",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    resource_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    prior_value_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    new_value_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "org",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "counterparties",
                schema: "contract",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    aliases = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    payer_identifier = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_counterparties", x => x.id);
                    table.ForeignKey(
                        name: "fk_counterparties_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "org",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "facilities",
                schema: "org",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    npi = table.Column<string>(type: "text", nullable: true),
                    tax_id = table.Column<string>(type: "text", nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    specialty = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_facilities", x => x.id);
                    table.ForeignKey(
                        name: "fk_facilities_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "org",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "org",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    permissions = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                    table.ForeignKey(
                        name: "fk_roles_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "org",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "source_connections",
                schema: "doc",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    display_name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    oauth_subject = table.Column<string>(type: "text", nullable: true),
                    scope_set = table.Column<string>(type: "text", nullable: true),
                    last_sync_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_source_connections", x => x.id);
                    table.ForeignKey(
                        name: "fk_source_connections_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "org",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "org",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "org",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "source_objects",
                schema: "doc",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    connection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    uri = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    mime_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    source_created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    source_modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_source_objects", x => x.id);
                    table.ForeignKey(
                        name: "fk_source_objects_source_connections_connection_id",
                        column: x => x.connection_id,
                        principalSchema: "doc",
                        principalTable: "source_connections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_source_objects_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "org",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contracts",
                schema: "contract",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    counterparty_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contracts", x => x.id);
                    table.ForeignKey(
                        name: "fk_contracts_counterparties_counterparty_id",
                        column: x => x.counterparty_id,
                        principalSchema: "contract",
                        principalTable: "counterparties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contracts_facilities_facility_id",
                        column: x => x.facility_id,
                        principalSchema: "org",
                        principalTable: "facilities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contracts_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "org",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contracts_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalSchema: "org",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ingestion_batches",
                schema: "doc",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    file_count = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ingestion_batches", x => x.id);
                    table.ForeignKey(
                        name: "fk_ingestion_batches_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "org",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ingestion_batches_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalSchema: "org",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "review_tasks",
                schema: "workflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    assigned_to_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decision = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    effort_seconds = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_review_tasks", x => x.id);
                    table.ForeignKey(
                        name: "fk_review_tasks_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "org",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_review_tasks_users_assigned_to_user_id",
                        column: x => x.assigned_to_user_id,
                        principalSchema: "org",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role_assignments",
                schema: "org",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: true),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_assignments_facilities_facility_id",
                        column: x => x.facility_id,
                        principalSchema: "org",
                        principalTable: "facilities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_role_assignments_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "org",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_role_assignments_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "org",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_role_assignments_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "org",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "document_assets",
                schema: "doc",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_object_id = table.Column<Guid>(type: "uuid", nullable: true),
                    storage_uri = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    mime_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    page_count = table.Column<int>(type: "integer", nullable: true),
                    text_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ocr_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_assets", x => x.id);
                    table.ForeignKey(
                        name: "fk_document_assets_source_objects_source_object_id",
                        column: x => x.source_object_id,
                        principalSchema: "doc",
                        principalTable: "source_objects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_document_assets_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "org",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contract_fields",
                schema: "contract",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    schema_version = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    field_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    value_json = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    normalized_value = table.Column<string>(type: "text", nullable: true),
                    confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    review_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract_fields", x => x.id);
                    table.ForeignKey(
                        name: "fk_contract_fields_contracts_contract_id",
                        column: x => x.contract_id,
                        principalSchema: "contract",
                        principalTable: "contracts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contract_fields_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "org",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "document_candidates",
                schema: "doc",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    facility_hint_id = table.Column<Guid>(type: "uuid", nullable: true),
                    counterparty_hint = table.Column<string>(type: "text", nullable: true),
                    confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_candidates", x => x.id);
                    table.ForeignKey(
                        name: "fk_document_candidates_document_assets_document_asset_id",
                        column: x => x.document_asset_id,
                        principalSchema: "doc",
                        principalTable: "document_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_document_candidates_facilities_facility_hint_id",
                        column: x => x.facility_hint_id,
                        principalSchema: "org",
                        principalTable: "facilities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_document_candidates_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "org",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "evidence_links",
                schema: "evidence",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    page_refs = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    quote = table.Column<string>(type: "text", nullable: false),
                    source_object_id = table.Column<Guid>(type: "uuid", nullable: true),
                    agent_run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_evidence_links", x => x.id);
                    table.ForeignKey(
                        name: "fk_evidence_links_document_assets_document_asset_id",
                        column: x => x.document_asset_id,
                        principalSchema: "doc",
                        principalTable: "document_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_evidence_links_source_objects_source_object_id",
                        column: x => x.source_object_id,
                        principalSchema: "doc",
                        principalTable: "source_objects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_evidence_links_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "org",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ingestion_jobs",
                schema: "doc",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_object_id = table.Column<Guid>(type: "uuid", nullable: true),
                    document_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    error_code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ingestion_jobs", x => x.id);
                    table.ForeignKey(
                        name: "fk_ingestion_jobs_document_assets_document_asset_id",
                        column: x => x.document_asset_id,
                        principalSchema: "doc",
                        principalTable: "document_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ingestion_jobs_ingestion_batches_batch_id",
                        column: x => x.batch_id,
                        principalSchema: "doc",
                        principalTable: "ingestion_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ingestion_jobs_source_objects_source_object_id",
                        column: x => x.source_object_id,
                        principalSchema: "doc",
                        principalTable: "source_objects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ingestion_jobs_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "org",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_resource_type_resource_id",
                schema: "audit",
                table: "audit_events",
                columns: new[] { "resource_type", "resource_id" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_tenant_id_created_at",
                schema: "audit",
                table: "audit_events",
                columns: new[] { "tenant_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_contract_fields_contract_id_field_key_schema_version",
                schema: "contract",
                table: "contract_fields",
                columns: new[] { "contract_id", "field_key", "schema_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contract_fields_tenant_id",
                schema: "contract",
                table: "contract_fields",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_contracts_counterparty_id",
                schema: "contract",
                table: "contracts",
                column: "counterparty_id");

            migrationBuilder.CreateIndex(
                name: "ix_contracts_facility_id",
                schema: "contract",
                table: "contracts",
                column: "facility_id");

            migrationBuilder.CreateIndex(
                name: "ix_contracts_owner_user_id",
                schema: "contract",
                table: "contracts",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_contracts_tenant_id_facility_id_status",
                schema: "contract",
                table: "contracts",
                columns: new[] { "tenant_id", "facility_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_counterparties_tenant_id_name",
                schema: "contract",
                table: "counterparties",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_document_assets_source_object_id",
                schema: "doc",
                table: "document_assets",
                column: "source_object_id");

            migrationBuilder.CreateIndex(
                name: "ix_document_assets_tenant_id_sha256",
                schema: "doc",
                table: "document_assets",
                columns: new[] { "tenant_id", "sha256" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_document_candidates_document_asset_id",
                schema: "doc",
                table: "document_candidates",
                column: "document_asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_document_candidates_facility_hint_id",
                schema: "doc",
                table: "document_candidates",
                column: "facility_hint_id");

            migrationBuilder.CreateIndex(
                name: "ix_document_candidates_tenant_id_status",
                schema: "doc",
                table: "document_candidates",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_evidence_links_document_asset_id",
                schema: "evidence",
                table: "evidence_links",
                column: "document_asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_evidence_links_resource_type_resource_id",
                schema: "evidence",
                table: "evidence_links",
                columns: new[] { "resource_type", "resource_id" });

            migrationBuilder.CreateIndex(
                name: "ix_evidence_links_source_object_id",
                schema: "evidence",
                table: "evidence_links",
                column: "source_object_id");

            migrationBuilder.CreateIndex(
                name: "ix_evidence_links_tenant_id",
                schema: "evidence",
                table: "evidence_links",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_facilities_tenant_id_code",
                schema: "org",
                table: "facilities",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ingestion_batches_created_by_user_id",
                schema: "doc",
                table: "ingestion_batches",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_ingestion_batches_tenant_id_created_at",
                schema: "doc",
                table: "ingestion_batches",
                columns: new[] { "tenant_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_ingestion_jobs_batch_id",
                schema: "doc",
                table: "ingestion_jobs",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_ingestion_jobs_document_asset_id",
                schema: "doc",
                table: "ingestion_jobs",
                column: "document_asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_ingestion_jobs_source_object_id",
                schema: "doc",
                table: "ingestion_jobs",
                column: "source_object_id");

            migrationBuilder.CreateIndex(
                name: "ix_ingestion_jobs_tenant_id_status",
                schema: "doc",
                table: "ingestion_jobs",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_review_tasks_assigned_to_user_id",
                schema: "workflow",
                table: "review_tasks",
                column: "assigned_to_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_review_tasks_tenant_id_decision_priority",
                schema: "workflow",
                table: "review_tasks",
                columns: new[] { "tenant_id", "decision", "priority" });

            migrationBuilder.CreateIndex(
                name: "ix_role_assignments_facility_id",
                schema: "org",
                table: "role_assignments",
                column: "facility_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_assignments_role_id",
                schema: "org",
                table: "role_assignments",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_assignments_tenant_id_user_id_facility_id_role_id",
                schema: "org",
                table: "role_assignments",
                columns: new[] { "tenant_id", "user_id", "facility_id", "role_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_assignments_user_id",
                schema: "org",
                table: "role_assignments",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_roles_tenant_id",
                schema: "org",
                table: "roles",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_source_connections_tenant_id_source_type_display_name",
                schema: "doc",
                table: "source_connections",
                columns: new[] { "tenant_id", "source_type", "display_name" });

            migrationBuilder.CreateIndex(
                name: "ix_source_objects_connection_id_external_id",
                schema: "doc",
                table: "source_objects",
                columns: new[] { "connection_id", "external_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_source_objects_tenant_id_sha256",
                schema: "doc",
                table: "source_objects",
                columns: new[] { "tenant_id", "sha256" });

            migrationBuilder.CreateIndex(
                name: "ix_users_tenant_id_email",
                schema: "org",
                table: "users",
                columns: new[] { "tenant_id", "email" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_events",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "contract_fields",
                schema: "contract");

            migrationBuilder.DropTable(
                name: "document_candidates",
                schema: "doc");

            migrationBuilder.DropTable(
                name: "evidence_links",
                schema: "evidence");

            migrationBuilder.DropTable(
                name: "ingestion_jobs",
                schema: "doc");

            migrationBuilder.DropTable(
                name: "review_tasks",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "role_assignments",
                schema: "org");

            migrationBuilder.DropTable(
                name: "contracts",
                schema: "contract");

            migrationBuilder.DropTable(
                name: "document_assets",
                schema: "doc");

            migrationBuilder.DropTable(
                name: "ingestion_batches",
                schema: "doc");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "org");

            migrationBuilder.DropTable(
                name: "counterparties",
                schema: "contract");

            migrationBuilder.DropTable(
                name: "facilities",
                schema: "org");

            migrationBuilder.DropTable(
                name: "source_objects",
                schema: "doc");

            migrationBuilder.DropTable(
                name: "users",
                schema: "org");

            migrationBuilder.DropTable(
                name: "source_connections",
                schema: "doc");

            migrationBuilder.DropTable(
                name: "tenants",
                schema: "org");
        }
    }
}
