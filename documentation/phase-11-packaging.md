# Phase 11 — Packaging and Installer (Prepared + publish attempted)

## Target
Windows 10/11 64-bit, `win-x64`, self-contained, single-file desktop publish. No manual Python/Node/DB/runtime install expected for end users (Windows desktop dependencies and OS components such as OCR/speech language resources remain environment-dependent).

## What was prepared
- `src/AI_Smart_Teacher.Desktop/AI_Smart_Teacher.Desktop.csproj` includes release-oriented publish properties: `RuntimeIdentifier=win-x64`, `SelfContained=true`, `PublishSingleFile=true`, `IncludeNativeLibrariesForSelfExtract=true`, `EnableCompressionInSingleFile=true`, `PublishReadyToRun=false`, version `0.9.0`.
- Portable build script: `publish/build-portable-win-x64.ps1`.
- Inno Setup script: `installer/AI_Smart_Teacher.iss` (outputs `publish/installer/AI_Smart_Teacher_Setup.exe`, installs to Program Files, optional desktop icon, English/Arabic installer languages, keeps `%LocalAppData%\AI_Smart_Teacher` user data on uninstall).

## Commands
```powershell
# Portable EXE folder
powershell -ExecutionPolicy Bypass -File .\publish\build-portable-win-x64.ps1

# Installer (requires Inno Setup 6 / ISCC.exe)
ISCC.exe .\installer\AI_Smart_Teacher.iss
```

## Actual execution in this session
- Solution build remained green: `dotnet build AI_Smart_Teacher.slnx -c Debug` → 0 warnings, 0 errors.
- Portable publish was **attempted** via `publish/build-portable-win-x64.ps1` (Release, `win-x64`, self-contained, single-file). See the command result recorded by the implementation session: confirm `publish/portable-win-x64/AI_Smart_Teacher.exe` exists and size before release. If publish was interrupted by environment limits, rerun the script on a machine with sufficient disk/time and .NET SDK workloads.
- **Inno Setup compilation was not executed** because `ISCC.exe`/Inno Setup was not detected in PATH on this machine. The `.iss` script is ready to compile where Inno Setup 6 is installed.

## Limits / honest notes
- Single-file self-contained WPF apps are large; first launch may be slower due to extraction.
- Bundling app dependencies does not guarantee Windows OCR/STT/TTS language packs, microphone permissions, or camera hardware.
- SQLite database/backups/logs/attachments are created under `%LocalAppData%\AI_Smart_Teacher`; the installer does not place writable data in Program Files.
- No code signing/notarization-equivalent Windows signing was performed. Unsigned installers/executables may trigger SmartScreen warnings.
- No automated installer smoke test was run.
