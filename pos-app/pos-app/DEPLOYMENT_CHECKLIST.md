# Deployment Verification Checklist

Use this checklist to verify your deployment is correct before troubleshooting.

## Pre-Deployment

- [ ] Application built successfully in Release mode
- [ ] `publish` folder contains all files
- [ ] `web.config` has `stdoutLogEnabled="true"` (for initial debugging)
- [ ] `appsettings.Production.json` has correct JWT secret key
- [ ] All files ready in `publish` folder

## File Upload Verification

### Critical Files (Must Exist)
- [ ] `pos-app.dll` - Main application DLL
- [ ] `web.config` - IIS configuration
- [ ] `appsettings.Production.json` - Production settings
- [ ] `pos-app.runtimeconfig.json` - Runtime configuration
- [ ] `pos-app.deps.json` - Dependency manifest

### Required Folders
- [ ] `wwwroot/` folder exists with all contents
- [ ] `wwwroot/_framework/` contains Blazor WebAssembly files
- [ ] `App_Data/` folder exists (or will be created automatically)
- [ ] `logs/` folder exists (or will be created automatically)

### All DLL Files
Verify these key DLLs are uploaded (there are many more, but these are critical):
- [ ] `pos-app.Client.dll`
- [ ] `Microsoft.AspNetCore.Components.WebAssembly.dll`
- [ ] `Microsoft.AspNetCore.Components.WebAssembly.Server.dll`
- [ ] `Microsoft.EntityFrameworkCore.Sqlite.dll`
- [ ] `Microsoft.AspNetCore.Authentication.JwtBearer.dll`
- [ ] `BCrypt.Net-Next.dll`
- [ ] All other DLL files from publish folder

## Server Configuration

### Plesk/IIS Settings
- [ ] `.NET Core 8.0 Runtime` is enabled/selected
- [ ] Application Pool uses `.NET CLR Version: No Managed Code`
- [ ] `Managed Pipeline Mode: Integrated`
- [ ] `Hosting Model: In-Process` (if option available)

### Permissions
- [ ] `App_Data` folder has **Write** permissions
- [ ] `logs` folder has **Write** permissions (if exists)
- [ ] All DLL files have **Read** permissions
- [ ] `wwwroot` folder and contents have **Read** permissions

## Post-Deployment Verification

### Initial Access
- [ ] Can access the website (even if showing error)
- [ ] Not getting "404 Not Found" (means files are there)
- [ ] Getting some response (even if 500 error)

### Log Files
- [ ] Checked `logs/stdout/` folder for error logs
- [ ] Found log files with timestamps
- [ ] Read error messages from log files

### Error Type Identification
- [ ] If 500 error: Check stdout logs for actual error
- [ ] If 404 error: Files not uploaded correctly
- [ ] If blank page: Check browser console for JavaScript errors
- [ ] If timeout: Server configuration issue

## Common Issues Quick Check

### Issue: "Could not find .NET runtime"
- [ ] Verify .NET 8.0 Runtime is installed on server
- [ ] Contact hosting provider if not available

### Issue: "File not found" or missing DLL
- [ ] Verify ALL .dll files are uploaded
- [ ] Don't skip any files - upload everything from publish folder
- [ ] Check file sizes match between local and server

### Issue: "Permission denied" or database errors
- [ ] `App_Data` folder exists
- [ ] `App_Data` has write permissions
- [ ] Application pool identity has write access

### Issue: "Configuration error"
- [ ] `appsettings.Production.json` exists
- [ ] JWT secret key is set (not placeholder)
- [ ] Connection string is correct: `Data Source=App_Data/master.db`

### Issue: "Static files not loading"
- [ ] `wwwroot` folder is completely uploaded
- [ ] `wwwroot/_framework/` contains Blazor files
- [ ] File permissions allow reading

## Next Steps After Verification

1. **If all checks pass but still 500 error:**
   - Check stdout logs for actual error message
   - Share error message for further troubleshooting

2. **If application loads:**
   - Test login with Super Admin credentials
   - Verify database is created in `App_Data/master.db`
   - Change default password
   - Disable stdout logging (set to `false` in web.config)

3. **If specific error found:**
   - Refer to `TROUBLESHOOTING_500_ERROR.md` for detailed fixes
   - Follow the specific fix for your error type

## Quick Test Commands

After deployment, you can test these URLs:

- `https://softxonepk.com` - Main application
- `https://softxonepk.com/api/swagger` - API documentation (if enabled in production)
- `https://softxonepk.com/_framework/blazor.web.js` - Blazor WebAssembly loader (should return JavaScript)

If the Blazor loader returns 404, the `wwwroot` folder is not uploaded correctly.

