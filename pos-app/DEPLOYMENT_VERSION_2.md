# Deployment Guide - Version 2 to softxonepk.com

**Date:** $(Get-Date -Format "yyyy-MM-dd")  
**Target:** softxonepk.com  
**Method:** Plesk File Manager  
**Status:** Ready for Deployment

## ✅ Pre-Deployment Checklist

- [x] Application built successfully in Release mode
- [x] `publish` folder contains all files
- [x] `web.config` verified - identical to server version (safe to replace)
- [x] `appsettings.Production.json` contains JWT key: `LB1QS7XzDJO1jrwW2DtshwKHBzjKx0+KmsKmFmIUR203jrFg3g1TgbC8sBtERkFr`

## 📁 Files to Deploy

### Location
All files are in: `pos-app/pos-app/publish/`

### Critical Files (Root Directory - Upload to httpdocs/)

#### Application Files
- `pos-app.dll` (611,840 bytes) - **MAIN APPLICATION - REQUIRED**
- `pos-app.exe` (151,552 bytes)
- `pos-app.runtimeconfig.json` (557 bytes) - **REQUIRED**
- `pos-app.deps.json` (87,559 bytes) - **REQUIRED**
- `pos-app.staticwebassets.endpoints.json` (194,518 bytes)

#### Configuration Files
- `web.config` (478 bytes) - **REQUIRED** (identical to server - safe to replace)
- `appsettings.Production.json` (652 bytes) - **REQUIRED** (verify JWT key matches)
- `appsettings.json` (614 bytes)
- `appsettings.Development.json` (474 bytes) - Optional

#### All DLL Files (Upload ALL - 75+ files)
**Core Application:**
- `pos-app.Client.dll` (970,240 bytes)
- `pos-app.Client.pdb` (466,940 bytes) - Debug symbols
- `pos-app.pdb` (153,640 bytes) - Debug symbols

**Microsoft.AspNetCore.* (15 files):**
- Microsoft.AspNetCore.Authentication.JwtBearer.dll
- Microsoft.AspNetCore.Authorization.dll
- Microsoft.AspNetCore.Components.dll
- Microsoft.AspNetCore.Components.Forms.dll
- Microsoft.AspNetCore.Components.Web.dll
- Microsoft.AspNetCore.Components.WebAssembly.dll
- Microsoft.AspNetCore.Components.WebAssembly.Server.dll
- Microsoft.AspNetCore.Metadata.dll
- Microsoft.AspNetCore.OpenApi.dll

**Microsoft.EntityFrameworkCore.* (6 files):**
- Microsoft.EntityFrameworkCore.Abstractions.dll
- Microsoft.EntityFrameworkCore.Design.dll
- Microsoft.EntityFrameworkCore.dll
- Microsoft.EntityFrameworkCore.Relational.dll
- Microsoft.EntityFrameworkCore.Sqlite.dll
- Microsoft.EntityFrameworkCore.SqlServer.dll

**Microsoft.Extensions.* (6 files):**
- Microsoft.Extensions.Configuration.Binder.dll
- Microsoft.Extensions.Configuration.FileExtensions.dll
- Microsoft.Extensions.Configuration.Json.dll
- Microsoft.Extensions.DependencyInjection.Abstractions.dll
- Microsoft.Extensions.DependencyInjection.dll
- Microsoft.Extensions.DependencyModel.dll
- Microsoft.Extensions.Logging.Abstractions.dll
- Microsoft.Extensions.Logging.dll
- Microsoft.Extensions.Options.dll

**Microsoft.Identity.* (7 files):**
- Microsoft.Identity.Client.dll
- Microsoft.Identity.Client.Extensions.Msal.dll
- Microsoft.IdentityModel.Abstractions.dll
- Microsoft.IdentityModel.JsonWebTokens.dll
- Microsoft.IdentityModel.Logging.dll
- Microsoft.IdentityModel.Protocols.dll
- Microsoft.IdentityModel.Protocols.OpenIdConnect.dll
- Microsoft.IdentityModel.Tokens.dll

**Microsoft.JSInterop.* (2 files):**
- Microsoft.JSInterop.dll
- Microsoft.JSInterop.WebAssembly.dll

**Other Microsoft.* (8 files):**
- Microsoft.Bcl.AsyncInterfaces.dll
- Microsoft.CodeAnalysis.CSharp.dll
- Microsoft.CodeAnalysis.CSharp.Workspaces.dll
- Microsoft.CodeAnalysis.dll
- Microsoft.CodeAnalysis.Workspaces.dll
- Microsoft.Data.SqlClient.dll
- Microsoft.Data.Sqlite.dll
- Microsoft.OpenApi.dll
- Microsoft.SqlServer.Server.dll
- Microsoft.Win32.SystemEvents.dll

**System.* (12 files):**
- System.CodeDom.dll
- System.Composition.AttributedModel.dll
- System.Composition.Convention.dll
- System.Composition.Hosting.dll
- System.Composition.Runtime.dll
- System.Composition.TypedParts.dll
- System.Configuration.ConfigurationManager.dll
- System.Data.SqlClient.dll
- System.Drawing.Common.dll
- System.IdentityModel.Tokens.Jwt.dll
- System.Memory.Data.dll
- System.Runtime.Caching.dll
- System.Security.Cryptography.ProtectedData.dll
- System.Security.Permissions.dll
- System.Windows.Extensions.dll

**Other Libraries:**
- Azure.Core.dll
- Azure.Identity.dll
- BCrypt.Net-Next.dll
- Humanizer.dll
- Mono.TextTemplating.dll
- SQLitePCLRaw.batteries_v2.dll
- SQLitePCLRaw.core.dll
- SQLitePCLRaw.provider.e_sqlite3.dll
- Swashbuckle.AspNetCore.Swagger.dll
- Swashbuckle.AspNetCore.SwaggerGen.dll
- Swashbuckle.AspNetCore.SwaggerUI.dll

### Folders to Upload

#### wwwroot/ (ENTIRE FOLDER - REQUIRED)
Contains all Blazor WebAssembly files and static assets:
- `_framework/` - Blazor runtime files
- `css/` - Stylesheets
- `js/` - JavaScript files
- All other static assets

**Action:** Upload entire `wwwroot` folder and overwrite existing

#### runtimes/ (if exists)
Contains platform-specific runtime files

#### Language folders (if exist)
- `cs/`, `de/`, `es/`, `fr/`, `it/`, `ja/`, `ko/`, `pl/`, `pt-BR/`, `ru/`, `tr/`, `zh-Hans/`, `zh-Hant/`

### ⚠️ Folders to PRESERVE (DO NOT OVERWRITE)

- **App_Data/** - Contains production database (master.db) - **CRITICAL - DO NOT DELETE**
- **logs/** - Contains application logs (if exists)

## 🚀 Step-by-Step Deployment Instructions

### Step 1: Access Plesk File Manager
1. Log into Plesk control panel
2. Navigate to `softxonepk.com`
3. Click **"Files"** in the left sidebar (File Manager)
4. Navigate to `httpdocs` folder (this is your website root)

### Step 2: Backup Current web.config (Optional but Recommended)
1. Right-click on `web.config` in httpdocs
2. Click **"Download"**
3. Save it locally as `web.config.server-backup-[date].xml`
4. This is your safety backup

### Step 3: Upload All Files from publish Folder

#### Option A: Upload Individual Files (Recommended for First Time)
1. Select all files in `pos-app/pos-app/publish/` folder (EXCEPT App_Data if it exists)
2. Upload them to `httpdocs/` in Plesk File Manager
3. **Important:** Do NOT upload any `App_Data` folder from local publish folder

#### Option B: Upload via ZIP (Faster)
1. Create a ZIP file of all files in `publish/` folder (EXCEPT App_Data)
2. Upload ZIP to Plesk File Manager
3. Extract ZIP in `httpdocs/` folder
4. Delete ZIP file after extraction

### Step 4: Upload wwwroot Folder
1. Navigate to `wwwroot` folder in publish directory
2. Upload entire `wwwroot` folder to `httpdocs/`
3. **Overwrite** existing wwwroot folder when prompted

### Step 5: Handle web.config

**Status:** ✅ Live web.config is IDENTICAL to local version - safe to replace

**Options:**
- **Option 1:** Replace with current `web.config` (identical, no change)
- **Option 2:** Replace with `web.config.alternative` (recommended - adds App_Data protection)

**Action:**
1. Upload `web.config` from publish folder
2. Overwrite existing web.config
3. Both versions are safe since core settings match

### Step 6: Verify Critical Files
After upload, verify these files exist in `httpdocs/`:
- [ ] `pos-app.dll` exists and size is ~611 KB
- [ ] `web.config` exists
- [ ] `appsettings.Production.json` exists
- [ ] `pos-app.runtimeconfig.json` exists
- [ ] `pos-app.deps.json` exists
- [ ] `wwwroot/_framework/` folder exists
- [ ] All DLL files are present

### Step 7: Verify App_Data Folder
1. Check that `App_Data/` folder still exists in `httpdocs/`
2. Verify `App_Data/master.db` file exists (your production database)
3. If missing, DO NOT create from local - contact support

## ✅ Post-Deployment Verification

### 1. Test Application Access
- [ ] Visit `https://softxonepk.com`
- [ ] Application loads without errors
- [ ] No 500 Internal Server Error

### 2. Test Authentication
- [ ] Login page loads
- [ ] Can log in with existing credentials
- [ ] JWT authentication works (users not logged out)

### 3. Test Key Features
- [ ] Dashboard loads
- [ ] Reports are accessible
- [ ] Data operations work correctly
- [ ] Database connections work

### 4. Check Static Files
- [ ] CSS files load (check browser DevTools)
- [ ] JavaScript files load
- [ ] Images/icons display correctly

### 5. Check Server Logs (if issues occur)
- Navigate to `logs/stdout` folder in Plesk File Manager
- Check for error messages
- Review application logs

## 🔄 Rollback Plan

If deployment causes issues:

1. **Restore web.config:**
   - Upload your backed-up `web.config.server-backup-[date].xml`
   - Rename to `web.config`

2. **Restore Previous Files:**
   - If you have a backup of previous version, restore those files
   - Or re-upload previous publish folder contents

3. **Check Logs:**
   - Review `logs/stdout` folder for error details
   - Check Plesk error logs

4. **Contact Support:**
   - If App_Data folder was accidentally deleted, contact hosting support immediately

## 📝 Important Notes

### web.config Status
- ✅ **VERIFIED:** Live web.config is IDENTICAL to local version
- Both use `AspNetCoreModuleV2`
- Both have same logging and hosting settings
- **Safe to replace** - no risk of breaking the application

### JWT Key
- Current JWT key in `appsettings.Production.json`: `LB1QS7XzDJO1jrwW2DtshwKHBzjKx0+KmsKmFmIUR203jrFg3g1TgbC8sBtERkFr`
- **IMPORTANT:** If this key differs from server, all users will be logged out
- Verify server's current JWT key before deployment if possible

### Database
- **CRITICAL:** `App_Data/master.db` contains all production data
- **NEVER** overwrite or delete this folder
- Always preserve `App_Data/` folder during deployment

### File Count
- Total files to upload: ~80+ files
- Total size: ~50-60 MB (estimated)
- Upload time: 5-15 minutes depending on connection

## 🎯 Quick Reference

**Source Folder:** `D:\POS\pos-app\pos-app\publish\`  
**Target Folder:** `httpdocs/` in Plesk File Manager  
**Preserve:** `App_Data/`, `logs/`  
**Replace:** Everything else  
**Critical Files:** `pos-app.dll`, `web.config`, `appsettings.Production.json`, `wwwroot/`

---

**Deployment Prepared By:** Automated Build System  
**Build Date:** $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")  
**Version:** 2.0

