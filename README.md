<div align="center">

# Codex Site Launcher

**A small Windows launcher for switching Codex Desktop API providers without losing your conversation history.**

<p align="center">
  <a href="./README.zh_CN.md">简体中文</a> |
  English
</p>

<p align="center">
  <img src="https://img.shields.io/badge/platform-Windows-0078D4?style=flat-square" alt="Windows">
  <img src="https://img.shields.io/badge/runtime-.NET%20Framework%204.x-512BD4?style=flat-square" alt=".NET Framework">
  <img src="https://img.shields.io/badge/app-Codex%20Desktop-111827?style=flat-square" alt="Codex Desktop">
</p>

<p align="center">
  <a href="#features">Features</a> •
  <a href="#how-it-works">How it works</a> •
  <a href="#quick-start">Quick start</a> •
  <a href="#safety-notes">Safety notes</a>
</p>

</div>

---

## Overview

Codex Site Launcher is a lightweight WinForms utility for users who run Codex Desktop with multiple OpenAI-compatible API providers.

It was built for the Windows Store/MSIX version of Codex Desktop, where launching the app with per-process environment variables or command-line overrides is not reliable. Instead, the launcher temporarily switches Codex configuration before startup, launches Codex through the normal Windows app activation path, and restores the previous configuration shortly after.

---

## Features

| Feature | Description |
| --- | --- |
| Provider switching | One-click launch for Facai API, Code Relay, QuickRouter, and custom providers. |
| Conversation-aware | Normal provider launches do not touch conversation files; optional history sync can copy provider-specific local history with backups. |
| Temporary config switch | Temporarily edits `config.toml`, launches Codex, then restores the previous config. |
| Backups | Keeps launcher baseline and last-before-launch config backups for comparison. |
| Resilient restore | If Codex also edits `config.toml` during startup, restores only launcher-owned fields and preserves other changes. |
| Diagnostics log | Includes a log file and an **Open Log** button for startup and restore troubleshooting. |
| Windows Store/MSIX support | Starts Codex Desktop through the app activation path instead of directly opening `WindowsApps`. |
| API key management | Saves API keys into Windows User environment variables. |
| History sync | Copies `newapi` and `openai` local history in either direction, plus a bidirectional sync mode that refreshes existing pairs by latest update time. |
| Migration rollback | Keeps the latest 10 pre-sync snapshots and can roll local history back from the UI. |
| Dark UI | Dark WinForms interface with built-in site management. |

---

## Built-in Providers

| Provider | Base URL | Environment variable |
| --- | --- | --- |
| Facai API | `https://api.system-update-center.club/v1` | `NEWAPI_API_KEY` |
| Code Relay | `https://api.code-relay.com/` | `CODE_RELAY_API_KEY` |
| QuickRouter | `https://api.quickrouter.ai/v1` | `QUICKROUTER_API_KEY` |

Custom providers can be added from the UI.

---

## How It Works

When launching a provider, the app:

1. Saves the selected provider API key to Windows User environment variables.
2. Temporarily updates:

   ```text
   C:\Users\<you>\.codex\config.toml
   ```

3. Starts Codex Desktop through the normal Windows application activation path.
4. Restores the previous `config.toml` after a short delay.

If Codex Desktop also writes to `config.toml` during startup, the launcher restores its own `model_provider`, `model_providers.newapi`, and `[windows]` changes while preserving unrelated Codex changes where possible.

Backups are stored at:

```text
C:\Users\<you>\.codex\config.toml.before-site-launcher.txt
C:\Users\<you>\.codex\config.toml.last-before-site-launcher.txt
```

Normal provider launch does **not** modify:

```text
C:\Users\<you>\.codex\sessions
C:\Users\<you>\.codex\archived_sessions
C:\Users\<you>\.codex\session_index.jsonl
C:\Users\<you>\.codex\state_*.sqlite
C:\Users\<you>\.codex\logs_*.sqlite
```

The optional history sync buttons do modify local history files. They create backups before touching `state_*.sqlite`, `session_index.jsonl`, and global state.

### History Sync

The **History Sync** area provides:

- **Copy newapi to openai**: create OpenAI-visible copies for `newapi` threads.
- **Copy openai to newapi**: create `newapi`-visible copies for OpenAI threads.
- **Bidirectional sync**: copy missing records in both directions, then refresh existing mapped pairs using the side with the newer `updated_at`.
- **Estimate usage**: preview copy count and added storage.
- **Migration records**: open the snapshot folder.
- **Rollback migration**: restore a selected pre-sync snapshot.

For consistent sorting, titles, archived state, and previews, sync rewrites complete `session_index.jsonl` records instead of minimal index stubs.

Snapshots are stored under:

```text
%APPDATA%\CodexSiteLauncher\migration-backups
```

Each snapshot includes the state database, session index, global state, sync map, `sessions`, and `archived_sessions`. The launcher keeps the newest 10 snapshots.

---

## Quick Start

1. Download [`CodexSiteLauncher.exe`](./dist/CodexSiteLauncher.exe).
2. Fully quit Codex Desktop before switching providers.
3. Run `CodexSiteLauncher.exe`.
4. Select a provider.
5. Paste the provider API key.
6. Click **Save Key to User Environment Variable**.
7. Click the provider launch button.

Use **Clean Launch Test** to verify that Codex Desktop can be opened without changing any provider configuration.

If launch or restore behavior needs troubleshooting, click **Open Log**. The log is stored at:

```text
%APPDATA%\CodexSiteLauncher\launcher.log
```

---

## Build From Source

The project is a single-file C# WinForms app and can be built with the .NET Framework C# compiler included with Windows:

```powershell
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" `
  /target:winexe `
  /platform:anycpu `
  /codepage:65001 `
  /reference:System.Windows.Forms.dll `
  /reference:System.Drawing.dll `
  /reference:System.Runtime.Serialization.dll `
  /out:CodexSiteLauncher.exe `
  CodexSiteLauncher.cs
```

---

## Safety Notes

- Do not commit or publish API keys.
- Do not upload your personal `.codex` directory.
- The current executable is provided under `dist/` for convenient direct download. For formal distribution, GitHub Releases is still recommended.
- Windows Store/MSIX apps live under `C:\Program Files\WindowsApps`; do not change ownership of that folder just to launch Codex.

---

## License

No license has been selected yet. Add one before accepting external contributions.
