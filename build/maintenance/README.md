# Maintenance pages for updates

`app_offline/<site>/app_offline.htm`: the page the server shows while a site is updated.

1. Upload it to the site's folder (the server stops the application and shows it, with 503).
2. Wait about 5 seconds, then copy the new files.
3. **Delete it** to bring the site back.

| Folder | Site | Names Vixly |
|---|---|---|
| `control` | Control (Platform.Web) | Yes |
| `platform-api` | Platform API | Yes |
| `public` | Public site | Yes |
| `operations-web` | Company websites and staff workspaces | No (serves every company) |
| `operations-api` | Operations API | No |
| `customer` | Customer portals | No |

**Never copy these files into a project:** a published `app_offline.htm` takes the site offline. The full steps are in [docs/global/PRODUCTION-UPDATE-RUNBOOK.md](../../docs/global/PRODUCTION-UPDATE-RUNBOOK.md).
