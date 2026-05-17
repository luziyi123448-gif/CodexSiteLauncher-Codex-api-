<div align="center">

# Codex Site Launcher

**一个用于切换 Codex Desktop 第三方 API 站点的 Windows 小工具，并尽量不影响你的对话记录。**

<p align="center">
  简体中文 |
  <a href="./README.md">English</a>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/platform-Windows-0078D4?style=flat-square" alt="Windows">
  <img src="https://img.shields.io/badge/runtime-.NET%20Framework%204.x-512BD4?style=flat-square" alt=".NET Framework">
  <img src="https://img.shields.io/badge/app-Codex%20Desktop-111827?style=flat-square" alt="Codex Desktop">
</p>

<p align="center">
  <a href="#功能特性">功能特性</a> •
  <a href="#工作方式">工作方式</a> •
  <a href="#快速开始">快速开始</a> •
  <a href="#安全说明">安全说明</a>
</p>

</div>

---

## 项目说明

Codex Site Launcher 是一个轻量级 WinForms 工具，适合需要在 Codex Desktop 里切换多个 OpenAI 兼容 API 站点的 Windows 用户。

Windows 商店 / MSIX 版 Codex Desktop 不能像普通 exe 那样稳定接收临时环境变量或 `-c` 启动参数。因此这个工具采用更稳的方式：启动前临时切换 Codex 配置，打开 Codex 后再恢复原配置。

---

## 功能特性

| 功能 | 说明 |
| --- | --- |
| 站点切换 | 内置 Facai API、Code Relay、QuickRouter，也支持自定义站点。 |
| 保护对话记录 | 不修改 Codex 的 sessions、sqlite 数据库或历史索引。 |
| 临时配置切换 | 启动前临时改 `config.toml`，启动后自动恢复。 |
| 配置备份 | 保留启动器初始对照和上一次启动前配置。 |
| 容错恢复 | 如果 Codex 启动时也改了 `config.toml`，会只恢复启动器改过的字段并保留其它变更。 |
| 诊断日志 | 内置日志文件和“打开日志”按钮，方便排查启动或恢复问题。 |
| 支持商店版 Codex | 通过 Windows 应用激活方式启动，不需要进入 `WindowsApps`。 |
| API Key 管理 | 将 API Key 保存到 Windows 用户环境变量。 |
| 深色界面 | 深色 WinForms 界面，支持站点管理和筛选。 |

---

## 内置站点

| 站点 | Base URL | 环境变量 |
| --- | --- | --- |
| Facai API | `https://api.system-update-center.club/v1` | `NEWAPI_API_KEY` |
| Code Relay | `https://api.code-relay.com/` | `CODE_RELAY_API_KEY` |
| QuickRouter | `https://api.quickrouter.ai/v1` | `QUICKROUTER_API_KEY` |

你也可以在界面里新增自定义站点。

---

## 工作方式

快速启动某个站点时，程序会：

1. 将该站点 API Key 保存到 Windows 用户环境变量。
2. 临时修改：

   ```text
   C:\Users\<你>\.codex\config.toml
   ```

3. 通过 Windows 正常应用激活路径启动 Codex Desktop。
4. 等待一小段时间后恢复启动前的 `config.toml`。

如果 Codex Desktop 在启动期间也写入了 `config.toml`，启动器会优先恢复自己改过的 `model_provider`、`model_providers.newapi` 和 `[windows]` 配置块，同时尽量保留 Codex 写入的其它内容。

启动器会保留这些对照备份：

```text
C:\Users\<你>\.codex\config.toml.before-site-launcher.txt
C:\Users\<你>\.codex\config.toml.last-before-site-launcher.txt
```

它不会修改这些聊天记录相关文件：

```text
C:\Users\<你>\.codex\sessions
C:\Users\<你>\.codex\archived_sessions
C:\Users\<你>\.codex\session_index.jsonl
C:\Users\<你>\.codex\state_*.sqlite
C:\Users\<你>\.codex\logs_*.sqlite
```

---

## 快速开始

1. 下载 [`CodexSiteLauncher.exe`](./dist/CodexSiteLauncher.exe)。
2. 切换站点前，先完全退出 Codex Desktop。
3. 打开 `CodexSiteLauncher.exe`。
4. 选择一个站点。
5. 填入对应 API Key。
6. 点击 **保存 Key 到用户环境变量**。
7. 点击对应站点的启动按钮。

如果只是想测试能不能打开 Codex，点击 **纯净启动测试**。这个按钮不会改配置。

如果启动失败、恢复失败或行为异常，点击 **打开日志**。日志位于：

```text
%APPDATA%\CodexSiteLauncher\launcher.log
```

---

## 从源码编译

项目是单文件 C# WinForms 程序，可以使用 Windows 自带的 .NET Framework C# 编译器构建：

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

## 安全说明

- 不要提交或公开 API Key。
- 不要上传个人 `.codex` 目录。
- 当前可执行文件放在 `dist/` 目录，方便直接下载。正式分发时仍推荐使用 GitHub Releases。
- Windows 商店 / MSIX 应用位于 `C:\Program Files\WindowsApps`，不建议为了启动 Codex 去修改该目录权限。

---

## 许可证

目前还没有选择许可证。若要接受外部贡献，建议后续补充开源许可证。
