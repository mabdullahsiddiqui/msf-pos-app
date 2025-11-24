# Quick Deployment Checklist - Version 2

## Pre-Deployment
- [x] Build completed: `dotnet publish -c Release -o ./publish`
- [x] web.config verified (identical to server - safe to replace)
- [x] appsettings.Production.json JWT key verified

## Deployment Steps

### 1. Access Plesk
- [ ] Log into Plesk → softxonepk.com
- [ ] Open File Manager → Navigate to `httpdocs/`

### 2. Backup (Optional)
- [ ] Download current `web.config` as backup

### 3. Upload Files
- [ ] Upload ALL files from `publish/` folder to `httpdocs/`
- [ ] **SKIP:** App_Data folder (preserve existing)
- [ ] Upload entire `wwwroot/` folder (overwrite existing)
- [ ] Upload `web.config` (safe - identical to server)

### 4. Verify Critical Files
- [ ] `pos-app.dll` exists (~611 KB)
- [ ] `web.config` exists
- [ ] `appsettings.Production.json` exists
- [ ] `pos-app.runtimeconfig.json` exists
- [ ] `pos-app.deps.json` exists
- [ ] `wwwroot/_framework/` folder exists
- [ ] `App_Data/master.db` still exists (NOT deleted)

### 5. Test Application
- [ ] Visit https://softxonepk.com
- [ ] Application loads (no 500 error)
- [ ] Login works
- [ ] Key features work
- [ ] Static files load (CSS, JS)

## Files Location
**Source:** `D:\POS\pos-app\pos-app\publish\`  
**Target:** Plesk File Manager → `httpdocs/`

## Important
- ✅ web.config is safe to replace (identical versions)
- ⚠️ DO NOT delete/overwrite App_Data folder
- ⚠️ JWT key must match or users will be logged out

