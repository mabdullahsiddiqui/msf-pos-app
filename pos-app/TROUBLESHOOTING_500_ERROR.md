# Troubleshooting 500 Internal Server Error

## Step 1: Enable Logging and Check Error Messages

### 1.1 Upload Updated web.config
The `web.config` file has been updated to enable stdout logging. **Re-upload the `web.config` file** from the `publish` folder to your server.

### 1.2 Check Logs
After uploading the updated `web.config`, try accessing your site again, then check for log files:

1. **Check stdout logs** (if enabled):
   - Navigate to: `httpdocs/logs/stdout/`
   - Look for files with timestamps (e.g., `stdout_20240101_123456.log`)
   - These will contain the actual error message

2. **Check Plesk Error Logs**:
   - Go to Plesk Control Panel → Your Domain → **Logs**
   - Look for recent error entries
   - Check both "Error Log" and "Access Log"

3. **Check Windows Event Viewer** (if you have access):
   - Look for ASP.NET Core errors
   - Check Application and System logs

## Step 2: Verify Server Requirements

### 2.1 Check .NET Runtime
In Plesk Control Panel:
1. Go to **Websites & Domains** → `softxonepk.com`
2. Click **ASP.NET Settings** or **IIS Settings**
3. Verify **.NET Core 8.0 Runtime** is selected
4. If not available, contact your hosting provider

### 2.2 Verify ASP.NET Core Module
- The `web.config` requires `AspNetCoreModuleV2`
- If your hosting doesn't support this, you may need to use `AspNetCoreModule` instead
- Contact hosting support if unsure

## Step 3: Verify File Structure

### 3.1 Required Files and Folders
Ensure these exist in your `httpdocs` folder:

```
httpdocs/
├── pos-app.dll (MUST EXIST)
├── web.config (MUST EXIST)
├── appsettings.Production.json (MUST EXIST)
├── pos-app.runtimeconfig.json (MUST EXIST)
├── pos-app.deps.json (MUST EXIST)
├── App_Data/ (folder - will be created if missing, but ensure write permissions)
├── logs/ (folder - will be created automatically for stdout logs)
├── wwwroot/ (MUST EXIST with all subfolders)
│   ├── _framework/
│   ├── css/
│   ├── js/
│   └── ...
└── [All .dll files from publish folder]
```

### 3.2 Verify wwwroot Folder
- Ensure `wwwroot` folder is uploaded completely
- Check that `wwwroot/_framework/` contains Blazor WebAssembly files
- Verify all static assets (CSS, JS, images) are present

## Step 4: Check Permissions

### 4.1 App_Data Folder Permissions
1. In Plesk File Manager, navigate to `httpdocs/`
2. Right-click on `App_Data` folder → **Change Permissions**
3. Set permissions to:
   - **Owner**: Read, Write, Execute
   - **Group**: Read, Execute
   - **Public**: Read, Execute
4. If `App_Data` doesn't exist, create it with write permissions

### 4.2 Logs Folder Permissions
1. Create `logs` folder if it doesn't exist
2. Set write permissions on `logs` folder (same as App_Data)

## Step 5: Verify Configuration

### 5.1 Check appsettings.Production.json
Verify the file exists and contains:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=App_Data/master.db"
  },
  "Jwt": {
    "Key": "LB1QS7XzDJO1jrwW2DtshwKHBzjKx0+KmsKmFmIUR203jrFg3g1TgbC8sBtERkFr",
    "Issuer": "POS-API-Production",
    "Audience": "POS-Client"
  }
}
```

### 5.2 Verify web.config
Ensure `web.config` has:
- `stdoutLogEnabled="true"` (for debugging)
- `processPath="dotnet"`
- `arguments=".\pos-app.dll"`
- `hostingModel="inprocess"`

## Step 6: Common Issues and Fixes

### Issue 1: Missing .NET 8.0 Runtime
**Symptom**: Error mentions .NET runtime not found
**Fix**: Contact hosting provider to install .NET 8.0 Runtime

### Issue 2: Missing Dependencies
**Symptom**: Error about missing DLL files
**Fix**: 
- Ensure ALL .dll files from `publish` folder are uploaded
- Don't skip any files - upload everything

### Issue 3: App_Data Permission Error
**Symptom**: Error about database file access
**Fix**: 
- Create `App_Data` folder manually
- Set write permissions on `App_Data` folder
- Ensure IIS application pool has write access

### Issue 4: Wrong Application Pool Configuration
**Symptom**: Generic 500 error with no details
**Fix**:
- In Plesk, go to **Websites & Domains** → **IIS Settings**
- Set Application Pool to use **.NET CLR Version: No Managed Code**
- Set **Managed Pipeline Mode: Integrated**

### Issue 5: Missing wwwroot Files
**Symptom**: Page loads but shows errors about missing resources
**Fix**:
- Verify `wwwroot` folder is completely uploaded
- Check that `wwwroot/_framework/` contains all Blazor files
- Ensure file permissions allow reading

### Issue 6: Connection String Error
**Symptom**: Database-related errors in logs
**Fix**:
- Verify `appsettings.Production.json` has correct connection string
- Ensure `App_Data` folder exists and has write permissions
- Check that path is relative: `Data Source=App_Data/master.db`

## Step 7: Quick Diagnostic Checklist

Before contacting support, verify:

- [ ] All files from `publish` folder are uploaded
- [ ] `web.config` exists and is correct
- [ ] `appsettings.Production.json` exists and has correct JWT key
- [ ] `App_Data` folder exists with write permissions
- [ ] `wwwroot` folder is completely uploaded
- [ ] `.NET 8.0 Runtime` is enabled in Plesk
- [ ] Application Pool is configured correctly
- [ ] Checked stdout logs for actual error message
- [ ] Checked Plesk error logs

## Step 8: Getting Help

If you've checked everything above and still have issues:

1. **Share the error from stdout logs** - This is the most important information
2. **Share your Plesk configuration** - Screenshot of IIS Settings
3. **Share file structure** - List of files in httpdocs root
4. **Share web.config** - The actual content (remove sensitive info)

## Next Steps After Fixing

Once the application is running:

1. **Disable stdout logging** (for security):
   - Change `stdoutLogEnabled="false"` in web.config
   - Re-upload web.config

2. **Verify application works**:
   - Access the site
   - Try logging in with Super Admin credentials
   - Check that database is created in App_Data

3. **Monitor logs**:
   - Check logs folder periodically
   - Monitor for any errors

