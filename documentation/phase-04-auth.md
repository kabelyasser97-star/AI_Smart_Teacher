# Phase 4 — Authentication, Users, Roles, Permissions (Implemented)

## Status
Implemented and verified by build + isolated local smoke test. No fake demo users/content were added to the application as defaults.

## Security/auth implementation
- Password hashing: PBKDF2-HMAC-SHA256 (`Rfc2898DeriveBytes.Pbkdf2`, 120,000 iterations, 16-byte random salt, 32-byte hash) in `Infrastructure/Security/Pbkdf2PasswordHasher.cs`.
- Passwords are never stored in plain text. Only Base64 hash+salt fields are persisted in existing `Users` table (`PasswordHash`, `PasswordSalt`).
- Login validates username/email (normalized) + active account + hash verification.
- Registration validates basic input (username 3+, display name 2+, password 8+), prevents duplicate username/email, assigns selected role.
- Bootstrap rule: creating an Admin account through registration is allowed only when the database has no users yet (`allowAdminBootstrap`). After initial setup, Admin self-registration is blocked.
- Student/Parent registrations create corresponding profile rows (`StudentProfiles`/`ParentProfiles`).
- Auth actions are logged to `AuditLogs` (`auth.register`, `auth.login`) without storing passwords.

## Roles/permissions
- Roles seeded in Phase 3 remain the system base: Student, Parent, Teacher, Admin.
- Permission map added in `Core/Security/AppPermissions.cs` (users/curriculum/AI/progress/reports/assessments/subscriptions/system settings). Current session exposes role permissions in UI; full role-specific dashboards and enforcement across all future features are planned for later phases.

## Desktop UI (foundation, real flow)
- `MainWindow` now provides login and account creation UI, language toggle (AR RTL / EN LTR), current session panel, and role permissions list.
- First-run bootstrap messaging is shown when no users exist.
- No fake features are exposed as working (curriculum/AI/OCR/voice/assessments/admin panels remain future phases).

## Actual verification
- `dotnet build AI_Smart_Teacher.slnx -c Debug` succeeded: 0 warnings, 0 errors.
- Isolated smoke test (`tests/AI_Smart_Teacher.DbSmoke`, temporary DB path, cleaned after run) results:
  - `HAS_USERS_BEFORE=False`
  - `REGISTER_ADMIN=True` (bootstrap allowed)
  - `HAS_USERS_AFTER=True`
  - `REGISTER_ADMIN_BLOCKED=False:auth.admin_not_allowed`
  - `REGISTER_STUDENT=True`
  - `LOGIN_OK=True:Admin`
  - `LOGIN_BAD=False:auth.invalid`
  - `HASH_NOT_PLAIN=True`, `SALT_PRESENT=True`
  - `STUDENT_PROFILE=1`
  - `AUDIT_AUTH=3`
  - `ADMIN_PERMS=10`, `STUDENT_HAS_MANAGE_USERS=False`

## Limits
- No account recovery/email verification/MFA.
- No full admin user-management screen yet (CRUD/disable/reset) — planned in admin phase.
- No DPAPI/secret vault integration yet for future AI/API keys/settings.
- UI is a foundation shell; end-user polish and full dashboards come later.
