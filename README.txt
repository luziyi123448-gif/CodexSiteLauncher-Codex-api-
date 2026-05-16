Codex API Site Launcher

What it does
- Stores site definitions in:
  %APPDATA%\CodexSiteLauncher\sites.json
- Stores API keys and selected site endpoint as Windows User environment variables.
- Temporarily switches C:\Users\<you>\.codex\config.toml before site launch, then restores the previous content after a short delay.
- Keeps backups:
  C:\Users\<you>\.codex\config.toml.before-site-launcher.txt
  C:\Users\<you>\.codex\config.toml.last-before-site-launcher.txt
- Starts Codex Desktop through the normal app activation path when it is installed as a WindowsApps/MSIX app.
- Pure launch mode does not edit config.toml.

Built-in sites
- Facai API
  https://api.system-update-center.club/v1
  NEWAPI_API_KEY
- Code Relay
  https://api.code-relay.com/
  CODE_RELAY_API_KEY
- QuickRouter
  https://api.quickrouter.ai/v1
  QUICKROUTER_API_KEY

How to use
1. Run CodexSiteLauncher.exe.
2. Select or browse to Codex Desktop's Codex.exe if it was not detected.
3. Select a site, paste the API key, and click "保存 Key 到用户环境变量".
4. Fully quit Codex Desktop before switching sites.
5. Click the launch button for the site you want.

Interface
- Top area: Codex.exe path and quick launch buttons.
- Left area: site list with filtering.
- Right area: selected site details, API key management, and launch button.
- Bottom status bar: current action and selected site state.

Custom sites
- Click "新增" to add a custom site.
- Select a site and edit fields, then click "保存修改".
- Built-in sites can be edited but not deleted.
