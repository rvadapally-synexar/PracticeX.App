# Database Migration Policy

PracticeX uses PostgreSQL with unquoted snake_case identifiers only.

## Rules

- Do not apply migrations automatically from application startup in production.
- Generate idempotent SQL scripts for deployment.
- Use the EF Core migration history table `audit.__ef_migrations_history`.
- Keep schemas explicit: `org`, `doc`, `contract`, `evidence`, `rate`, `workflow`, `audit`, and later `ref`.
- Review generated SQL before applying it to shared environments.

## Local commands

Create a migration:

```powershell
dotnet ef migrations add <name> --project src/PracticeX.Infrastructure --startup-project src/PracticeX.Api --output-dir Persistence/Migrations
```

Generate an idempotent deployment script:

```powershell
dotnet ef migrations script --idempotent --project src/PracticeX.Infrastructure --startup-project src/PracticeX.Api --output migrations/practicex.sql
```

Apply locally only after reviewing the generated SQL:

```powershell
dotnet ef database update --project src/PracticeX.Infrastructure --startup-project src/PracticeX.Api
```

