# Privacy Policy

**Copilot Tray Stats**  
Last updated: April 13, 2026

---

## Overview

Copilot Tray Stats is a local Windows desktop application that displays your GitHub Copilot premium request quota in the system tray. This policy describes what data the app accesses, where it is stored, and what is transmitted over the network.

---

## Data Collected and Stored Locally

All data is stored exclusively on your device under `%AppData%\CopilotTrayStats\`.

| File | Contents | Purpose |
|------|----------|---------|
| `settings.json` | Refresh interval, run-on-startup flag, display preferences | User preferences |
| `state.json` | Last-known quota values, username, plan type, reset date | Restore display state between sessions without an extra API call |
| `history.json` | Daily snapshots of premium requests remaining/total and quota reset date | Usage history chart |

No personally identifiable information beyond your GitHub username and plan type (as returned by the GitHub API) is stored. No data is written outside `%AppData%\CopilotTrayStats\` except for one Windows registry entry (see [Run on Startup](#run-on-startup) below).

---

## Data Transmitted Over the Network

The app makes outbound HTTPS requests to the GitHub API to fetch your Copilot quota data and to check for updates. The app itself does not store, log, or share anything that is transmitted. GitHub may log API requests in accordance with their own [Privacy Statement](https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement).

No analytics, telemetry, crash reporting, or advertising services are used.

---

## Authentication

The app does **not** store your GitHub credentials or token directly. It shells out to the [GitHub CLI](https://cli.github.com/) (`gh auth token`) at runtime to obtain a short-lived bearer token. The token is held in memory only and is discarded when the app exits. You can revoke access at any time through your [GitHub account security settings](https://github.com/settings/security).

---

## Run on Startup

If you enable the "Run on startup" option, the app writes a single registry value to:

```
HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run
```

This value contains only the path to the application executable. Disabling the option removes the registry value.

---

## Data Retention and Deletion

All locally stored data can be deleted at any time by removing the folder `%AppData%\CopilotTrayStats\`. Uninstalling the app does not automatically remove this folder.

---

## Changes to This Policy

If this policy is updated, the "Last updated" date at the top of this document will be revised. Continued use of the app after changes constitutes acceptance of the updated policy.

---

## Contact

For questions or concerns, open an issue on the [GitHub repository](https://github.com/ilGianfri/copilot-tray-stats).
