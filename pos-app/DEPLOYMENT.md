# Deployment Guide for POS Application

## Overview
This is a unified Blazor WebAssembly application that combines both the frontend and backend into a single deployable package. It's designed to run on shared hosting (softxonepk.com) without requiring SignalR.

## Prerequisites
- .NET 8.0 Runtime installed on the server (or hosting provider supports .NET 8.0)
- SQL Server database access (for client databases)
- Write permissions for `App_Data` folder (for SQLite master database)

## Deployment Steps

### 1. Prepare the Application

The application has already been built and published to the `publish` folder. The publish folder contains all necessary files for deployment.

### 2. Upload Files to Server

Upload all files from the `publish` folder to your web hosting root directory (typically `wwwroot` or `httpdocs` on softxonepk.com).

**Important files to include:**
- `pos-app.dll` (main application)
- `web.config` (IIS configuration)
- `appsettings.Production.json` (production configuration)
- `wwwroot/` folder (all static assets and Blazor WebAssembly files)
- All `.dll` files in the root
- `App_Data/` folder (will be created automatically, but ensure write permissions)

### 3. Configure Production Settings

**Before deploying, update `appsettings.Production.json`:**

1. **JWT Secret Key**: Replace the placeholder with a secure random string (at least 32 characters):
   ```json
   "Jwt": {
     "Key": "YOUR_SECURE_RANDOM_STRING_HERE_AT_LEAST_32_CHARACTERS",
     "Issuer": "POS-API-Production",
     "Audience": "POS-Client"
   }
   ```

2. **Database Connection**: The SQLite master database will be created automatically in `App_Data/master.db`. Ensure the `App_Data` folder has write permissions.

3. **Client Database Connections**: Client database connection strings are stored dynamically in the master database for each user account. The `ClientDbTemplate` setting in `appsettings.Production.json` is not used by the application - it's a legacy setting that can be ignored. Each user's database connection information (server, database name, credentials, etc.) is stored in the `Users` table in `master.db` and connections are built dynamically when needed.

### 4. Server Configuration

**Ensure your hosting provider has:**
- ✅ .NET 8.0 Runtime installed
- ✅ ASP.NET Core Module (ANCM) installed
- ✅ Write permissions on `App_Data` folder
- ✅ SQL Server access (if using client databases)

**IIS Configuration (if applicable):**
- The `web.config` file is already configured for IIS
- Ensure the application pool is set to "No Managed Code" (since we're using in-process hosting)
- Set the application pool to use .NET CLR Version "No Managed Code"

### 5. First-Time Setup

After deployment:

1. **Access the application** at your domain (e.g., `https://softxonepk.com`)

2. **Default Super Admin Credentials** (if database is empty):
   - Email: `imabdullahsiddiqui@gmail.com`
   - Password: `test1234`
   - **⚠️ IMPORTANT: Change this password immediately after first login!**

3. **Verify Database Creation**:
   - Check that `App_Data/master.db` is created
   - The application will automatically seed the Super Admin user on first run

### 6. Post-Deployment Checklist

- [ ] Application loads without errors
- [ ] Can log in with Super Admin credentials
- [ ] Database files are created in `App_Data/`
- [ ] API endpoints are accessible (check `/swagger` if enabled)
- [ ] Static files (CSS, JS) are loading correctly
- [ ] JWT authentication is working
- [ ] Changed default Super Admin password

### 7. Troubleshooting

**500 Internal Server Error:**
- See detailed troubleshooting guide: `TROUBLESHOOTING_500_ERROR.md`
- Enable stdout logging in `web.config` (set `stdoutLogEnabled="true"`)
- Check `logs/stdout` folder for actual error messages
- Verify all files from `publish` folder are uploaded
- Ensure `App_Data` folder exists with write permissions
- Verify `.NET 8.0 Runtime` is enabled in hosting control panel

**Application won't start:**
- Check that .NET 8.0 Runtime is installed
- Verify `web.config` is present and correct
- Check server logs in `logs/stdout` folder (if enabled)

**Database errors:**
- Ensure `App_Data` folder exists and has write permissions
- Check connection strings in `appsettings.Production.json`

**Static files not loading:**
- Verify `wwwroot` folder is uploaded
- Check file permissions on static files
- Ensure `web.config` allows static file serving

**Authentication not working:**
- Verify JWT secret key is set correctly
- Check that JWT settings match between appsettings files

### 8. File Structure After Deployment

```
/
├── pos-app.dll
├── web.config
├── appsettings.Production.json
├── App_Data/
│   └── master.db (created automatically)
├── wwwroot/
│   ├── _framework/
│   ├── css/
│   ├── js/
│   └── ...
├── logs/ (if logging enabled)
└── [other .dll files]
```

### 9. Security Recommendations

1. **Change JWT Secret**: Use a strong, random secret key in production
2. **Change Default Password**: Immediately change the default Super Admin password
3. **Enable HTTPS**: Ensure SSL certificate is configured
4. **Restrict App_Data Access**: The `web.config` already denies direct access to `App_Data`
5. **Update Dependencies**: Consider updating packages with known vulnerabilities:
   - `System.Data.SqlClient` (has high severity vulnerability)
   - `System.IdentityModel.Tokens.Jwt` (has moderate severity vulnerability)

### 10. Monitoring

- Check application logs regularly
- Monitor database file size in `App_Data/`
- Monitor server resources (CPU, memory)
- Set up error alerts if available

## Support

For issues or questions:
- Check server logs: `logs/stdout` (if enabled)
- Review application logs in the hosting control panel
- Contact hosting support for server-level issues

---

**Last Updated**: Deployment package created for unified Blazor WebAssembly application
**Version**: 1.0.0
**Target Framework**: .NET 8.0

