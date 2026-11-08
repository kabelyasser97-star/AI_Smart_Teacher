# AI Smart Teacher (المعلم الذكي) — Release/Build Notes

Windows 10/11 x64 desktop application (.NET 10 WPF, SQLite/EF Core). Local-first; user data is stored under `%LocalAppData%\AI_Smart_Teacher\` (`data`, `backups`, `logs`, `attachments`).

## Implemented foundations
- Local auth/RBAC, curriculum/lessons, AI tutor (NotConfigured/Offline/OpenAI-compatible), AI settings with DPAPI-protected local API key, conversations, image attach + Windows OCR foundation, Windows STT/TTS foundation, assessments/progress (objective auto-scoring), admin foundation (users/roles, audit view, non-sensitive settings, local backup trigger), AR/EN RTL/LTR shell.

## Build
```powershell
dotnet build .\AI_Smart_Teacher.slnx -c Debug
```

## Portable (self-contained, win-x64)
```powershell
powershell -ExecutionPolicy Bypass -File .\publish\build-portable-win-x64.ps1
```
Output: `publish\portable-win-x64\AI_Smart_Teacher.exe`

## Installer (Inno Setup 6)
Compiled in this environment when Inno Setup 6 was installed:
```powershell
& "C:\Users\yasser\AppData\Local\Programs\Inno Setup 6\ISCC.exe" ".\installer\AI_Smart_Teacher.iss"
```
Output: `publish\installer\AI_Smart_Teacher_Setup.exe`

## Honest limits
- No manual UI/device QA in this workspace session; OCR/speech depend on Windows components, permissions, and installed language resources.
- Cloud AI requires internet + user-supplied valid configuration; API key is protected locally (DPAPI CurrentUser), never hardcoded.
- Subjective answers require manual review (no grading UI). No restore UI, billing, server sync, or full exam/admin platform.
- Unsigned binaries/installers may show SmartScreen warnings.
