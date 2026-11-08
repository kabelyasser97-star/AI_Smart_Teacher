# Phase 3 — SQLite/EF Core Foundation (Implemented)

## Status
Implemented and verified by build + local smoke test on this machine.

## What was added
- EF Core packages in `AI_Smart_Teacher.Data`: `Microsoft.EntityFrameworkCore.Sqlite` (10.0.14), `Microsoft.EntityFrameworkCore.Design` (10.0.14).
- Domain enums/entities in `AI_Smart_Teacher.Core` for: users/roles/profiles/parent links, education stages/grades/curricula/subjects/units/lessons/content, conversations/messages/attachments, assessments/questions/options/results/answers/progress, subscriptions/payments, notifications, system settings, audit logs.
- `AppDbContext` + EF configurations (indexes, lengths, relationships, soft-delete query filter, `UpdatedAtUtc`).
- Design-time factory: `AI_Smart_Teacher.Data/Contexts/AppDbContextFactory.cs`.
- Initial migration: `20261105230808_InitialCreate` in `AI_Smart_Teacher.Data/Migrations/`.
- `IAppPaths` abstraction and `AppPaths` implementation: stores runtime data under `%LocalAppData%\AI_Smart_Teacher\` (`data/`, `backups/`, `logs/`, `attachments/`) — not in Program Files.
- `DatabaseInitializer`: applies migrations (`MigrateAsync`) and seeds only required system roles and minimal non-user system settings (no fake user/content data).
- `DatabaseBackupService`: SQLite online backup API to timestamped backup file; restore uses temp file + SQLite backup + pool clear + WAL/SHM cleanup.
- DI registration in `AI_Smart_Teacher.Infrastructure.ServiceCollectionExtensions`.
- Desktop startup wires DI and runs DB initialization during startup; shows a warning dialog if initialization fails (does not silently pretend success).

## Actual verification
- `dotnet build AI_Smart_Teacher.slnx -c Debug` succeeded: 0 warnings, 0 errors.
- `dotnet run --project tests/AI_Smart_Teacher.DbSmoke/AI_Smart_Teacher.DbSmoke.csproj -c Debug` succeeded.
- Smoke test results (actual):
  - DB created at `%LocalAppData%\AI_Smart_Teacher\data\ai_smart_teacher.db`
  - Applied migration: `20261105230808_InitialCreate`
  - Seeded roles present: `Student,Parent,Teacher,Administrator`
  - Seeded system settings count: `4`
  - Backup created and exists.
  - Restore executed and DB remained connectable; roles count after restore: `4`.

## Important notes / limits
- Target framework remains .NET 10 (`net10.0`/`net10.0-windows`) per approved option A; final user delivery still requires self-contained publishing/installer work in later phases.
- Schema is designed for SQLite now and kept extensible; PostgreSQL migration/multi-user sync is not implemented.
- Sensitive data protection is not complete yet (password hashing/encryption/key management come in later phases). No API keys are stored.
- UI for backup/restore, auth, content, AI, OCR, voice, assessments, admin, and installer is not implemented yet.
