# Deployment and Operations

## Deployment model

The server project (`pos-app`) is published and deployed as an ASP.NET Core app, commonly hosted on IIS/Plesk (as documented in project deployment guides).

## Publish steps

```powershell
cd d:\POS\pos-app\pos-app
dotnet publish -c Release -o .\publish
```

Deploy contents of `publish/` to host web root (`httpdocs` in Plesk workflows).

## Included deployment scripts

Located in `pos-app/`:

- `deploy-to-softxonepk.ps1`
- `deploy-critical-files.ps1`
- `troubleshoot-deployment.ps1`

Reference:

- `DEPLOYMENT_INSTRUCTIONS.md`
- `DEPLOYMENT.md`
- `DEPLOYMENT_CHECKLIST*.md`
- `TROUBLESHOOTING_500_ERROR.md`

## Critical runtime files

- `pos-app.dll`
- `pos-app.runtimeconfig.json`
- `pos-app.deps.json`
- `web.config`
- `appsettings.Production.json`
- all dependency `.dll` files
- entire `wwwroot/` folder

## Persistent data directories

Do not remove during deployment:

- `App_Data/` (master DB persistence)
- `logs/` (stdout/application logs when configured)

## Production configuration checklist

- .NET 8 runtime installed on host
- correct app pool/runtime settings for ASP.NET Core
- write permissions on `App_Data/`
- valid JWT configuration in production app settings
- HTTPS enabled and certificate valid

## Smoke test after deployment

1. Open site root.
2. Confirm login page and static assets load.
3. Login with valid user and load dashboard.
4. Open 2-3 report screens with date filters.
5. Validate client DB connectivity from app.

## Common failures and first checks

### HTTP 500 on startup

- Verify `web.config` exists and is valid.
- Review host logs (`stdout_*.log` / IIS logs).
- Check DB path and `App_Data` write permission.
- Confirm runtime version compatibility (.NET 8).

### Static resources missing

- Verify full `wwwroot` upload (especially `_framework`).
- Confirm no partial upload or stale file mix.

### Forced logouts after deploy

- Often caused by JWT key changes.
- Keep production JWT key stable across deployments.

## Operations notes

- Startup performs DB write test and master DB creation/seed.
- Health issues frequently surface first in startup logs.
- Keep deployment scripts and docs aligned with any hosting changes.
