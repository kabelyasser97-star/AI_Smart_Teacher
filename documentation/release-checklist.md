# Release Checklist — AI Smart Teacher

## Automated/build (completed in this workspace)
- [x] Solution builds: `dotnet build AI_Smart_Teacher.slnx -c Debug` (0 warnings, 0 errors).
- [x] DbSmoke passed (temp SQLite): AI/settings/DPAPI/cloud/conversations, assessments/progress, admin/backup.
- [x] Portable publish: `publish/portable-win-x64/AI_Smart_Teacher.exe` (self-contained, win-x64, single-file).
- [x] Inno Setup installer compiled: `publish/installer/AI_Smart_Teacher_Setup.exe`.

## Manual QA required before release
- [ ] Clean Windows 10/11 x64: run portable EXE; verify first-run DB creation under `%LocalAppData%\AI_Smart_Teacher`.
- [ ] Install/uninstall with `AI_Smart_Teacher_Setup.exe`; verify shortcuts, launch, SmartScreen prompt, and that user data is retained on uninstall (by design).
- [ ] Auth/RBAC: admin bootstrap rules, login/logout, Student/Parent/Teacher/Admin permissions, tab visibility.
- [ ] Curriculum: stage/grade/curriculum/subject/unit/lesson/content CRUD and read flows.
- [ ] AI Tutor: NotConfigured/Offline behavior; settings save; DPAPI key protection (no plain key in UI storage); cloud disabled/enabled behavior; real OpenAI-compatible call only with user key/network.
- [ ] Vision/Voice: image attach/OCR on real images, Arabic OCR language resources, microphone permission/dictation, TTS playback/voices, graceful failures.
- [ ] Assessments/progress: create/publish/take objective tests, score/results/progress; subjective answers remain manual-review only.
- [ ] Admin: user role/active changes, audit log, non-sensitive settings, backup creation; confirm sensitive AI key not editable via generic settings.
- [ ] RTL/LTR Arabic/English visual QA, accessibility, high DPI, long text.
- [ ] Optional: code-sign EXE and installer.

## Not implemented (do not claim)
- Server sync/multi-user backend, billing/subscriptions, restore UI, subjective grading UI, full exam/admin platform.
