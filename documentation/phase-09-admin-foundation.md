# Phase 9 — Admin Foundation and Local System Settings (Implemented foundation)

## Status
Implemented as an admin/system foundation. The full solution builds with 0 warnings and 0 errors. This is not a complete enterprise admin console.

## What was added
- **Contracts (Application):** `Application/Admin/AdminContracts.cs` defines user/admin DTOs, audit log DTOs, visible setting DTOs, system status, and `IAdminService`.
- **Service (Infrastructure):** `Infrastructure/Admin/AdminService.cs` provides:
  - User list and basic role/active-state management (`users.manage` in UI flow).
  - Recent audit log retrieval (`reports.view.all` in UI flow).
  - Visible system settings retrieval; sensitive settings are returned as `[protected]` and not editable by this form.
  - Upsert for non-sensitive settings only; sensitive keys (including `ai.provider.apiKey*`) are blocked.
  - Local SQLite backup trigger via existing `IDatabaseBackupService`; backup actions are audit logged.
  - System status counts for users/assessments/conversations/attachments and local data paths.
- **Desktop UI/viewmodel:** `Desktop/ViewModels/AdminViewModel.cs` and `AdminTab` in `MainWindow.xaml/.cs` provide a permission-gated admin foundation: refresh, user activation/deactivation, role assignment, audit log view, non-sensitive setting save, and backup button. Controls are enabled based on `users.manage`/`users.view`/`reports.view.all`/`system.settings.manage`.
- **DI:** `IAdminService` is registered in `Infrastructure/ServiceCollectionExtensions`.

## Actual verification
- `dotnet build C:\AI_Smart_Teacher\AI_Smart_Teacher.slnx -c Debug` — **succeeded, 0 warnings and 0 errors**.
- Source inspection confirms sensitive settings are masked and protected AI key settings are blocked from admin non-sensitive upsert.
- **Not executed in this session:** manual WPF admin workflow tests, role-change impact tests across sessions, backup restore UI (not implemented), and permission boundary testing with multiple signed-in accounts.

## Limits / honest notes
- No complete admin CRUD for all entities, no user invitations/password reset emails, and no audit export/filter UI.
- No restore-from-backup UI; backup service support exists but restore is not exposed safely in the admin tab.
- Subscription/billing management is not implemented as a working payment flow.
- Multi-user concurrency/centralized server sync is out of scope for the local SQLite desktop foundation.
