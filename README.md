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
| Conversation-safe | Does not touch Codex session files, SQLite databases, or conversation history. |
| Temporary config switch | Temporarily edits `config.toml`, launches Codex, then restores the previous config. |
| Backups | Keeps launcher baseline and last-before-launch config backups for comparison. |
| Windows Store/MSIX support | Starts Codex Desktop through the app activation path instead of directly opening `WindowsApps`. |
| API key management | Saves API keys into Windows User environment variables. |
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

Backups are stored at:

```text
C:\Users\<you>\.codex\config.toml.before-site-launcher.txt
C:\Users\<you>\.codex\config.toml.last-before-site-launcher.txt
```

The launcher does **not** modify:

```text
C:\Users\<you>\.codex\sessions
C:\Users\<you>\.codex\archived_sessions
C:\Users\<you>\.codex\session_index.jsonl
C:\Users\<you>\.codex\state_*.sqlite
C:\Users\<you>\.codex\logs_*.sqlite
```

---

## Quick Start

1. Download `CodexSiteLauncher.exe` from the latest release.
2. Fully quit Codex Desktop before switching providers.
3. Run `CodexSiteLauncher.exe`.
4. Select a provider.
5. Paste the provider API key.
6. Click **Save Key to User Environment Variable**.
7. Click the provider launch button.

Use **Clean Launch Test** to verify that Codex Desktop can be opened without changing any provider configuration.

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
- The executable is intentionally not tracked in git; release binaries should be uploaded under GitHub Releases.
- Windows Store/MSIX apps live under `C:\Program Files\WindowsApps`; do not change ownership of that folder just to launch Codex.

---

## License

No license has been selected yet. Add one before accepting external contributions.

