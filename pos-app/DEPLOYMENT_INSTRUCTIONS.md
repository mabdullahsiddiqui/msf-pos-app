# Quick Deployment Guide - Unified Blazor App to softxonepk.com

## ✅ Pre-Deployment Checklist

- [x] App built in Release mode
- [x] Publish folder contains all files
- [x] web.config configured correctly
- [x] appsettings.Production.json has correct JWT key

## 🚀 Deployment Options

### Option 1: Automated FTP Script (Recommended)

**Prerequisites:**
- FTP credentials from Plesk (Files & Databases → FTP Access)
- PowerShell execution policy allows scripts

**Steps:**

1. **Get FTP Credentials from Plesk:**
   - Log into Plesk → `softxonepk.com`
   - Go to **Files & Databases** → **FTP Access**
   - Note your FTP username and password

2. **Run the Deployment Script:**
   ```powershell
   cd d:\POS\pos-app\pos-app
   .\deploy-to-softxonepk.ps1 -FtpUsername "your-ftp-username" -FtpPassword "your-ftp-password"
   ```

   The script will:
   - Upload all files from `publish/` folder
   - Create necessary directory structure
   - Show progress for each file
   - Provide deployment summary

3. **Verify Deployment:**
   - Visit: https://softxonepk.com
   - Check that the app loads correctly
   - Test login functionality

---

### Option 2: Manual Plesk File Manager Upload

**Steps:**

1. **Access Plesk File Manager:**
   - Log into Plesk → `softxonepk.com`
   - Click **Files** in the left sidebar
   - Navigate to `httpdocs/` (website root)

2. **Backup Current Files (Optional but Recommended):**
   - Select all files in `httpdocs/`
   - Right-click → **Archive** → Create a backup ZIP

3. **Upload Files:**
   - **Important:** Upload ALL files from `d:\POS\pos-app\pos-app\publish\` folder
   - You can select multiple files and upload them at once
   - Or upload the entire folder structure

4. **Critical Files to Upload:**
   - ✅ `pos-app.dll` (main application - 611 KB)
   - ✅ `web.config` (IIS configuration)
   - ✅ `appsettings.Production.json` (production settings)
   - ✅ `pos-app.runtimeconfig.json`
   - ✅ `pos-app.deps.json`
   - ✅ **ALL** `.dll` files (75+ files)
   - ✅ **Entire** `wwwroot/` folder (with all subfolders)

5. **Preserve Important Folders:**
   - ⚠️ **DO NOT DELETE** `App_Data/` folder (contains production database)
   - ⚠️ **DO NOT DELETE** `logs/` folder (if it exists)

6. **Verify App_Data Permissions:**
   - Right-click `App_Data/` folder → **Properties** → **Permissions**
   - Ensure IIS Application Pool has **Write** permissions
   - If folder doesn't exist, it will be created automatically on first run

7. **Test the Application:**
   - Visit: https://softxonepk.com
   - Check for any errors
   - Test login functionality

---

## ⚙️ Server Configuration (One-Time Setup)

If this is the first deployment, ensure:

1. **.NET 8.0 Runtime is Installed:**
   - Plesk → **Websites & Domains** → `softxonepk.com` → **ASP.NET Settings**
   - Ensure **.NET Core 8.0** is selected

2. **Application Pool Settings:**
   - Plesk → **Websites & Domains** → `softxonepk.com` → **Application Pool**
   - **.NET CLR Version:** No Managed Code
   - **Managed Pipeline Mode:** Integrated
   - **Hosting Model:** In-Process (if available)

3. **App_Data Folder Permissions:**
   - Ensure `App_Data/` folder has Write permissions for IIS Application Pool
   - The app will create this folder automatically if it doesn't exist

---

## 🔍 Post-Deployment Verification

After deployment, verify:

- [ ] Application loads at https://softxonepk.com
- [ ] No 500 Internal Server Error
- [ ] Login page appears correctly
- [ ] Static files load (CSS, JS, images)
- [ ] Dashboard loads after login
- [ ] Reports are accessible
- [ ] Database connections work

---

## 🐛 Troubleshooting

### 500 Internal Server Error

1. **Check Logs:**
   - Plesk → **Files** → `logs/stdout_*.log`
   - Look for error messages

2. **Verify web.config:**
   - Ensure `web.config` is uploaded correctly
   - Check that `stdoutLogEnabled="true"` is set

3. **Check App_Data Permissions:**
   - Ensure `App_Data/` folder has Write permissions
   - The app needs to create/access `master.db`

4. **Verify .NET Runtime:**
   - Ensure .NET 8.0 Runtime is installed and selected

### Users Logged Out After Deployment

- This happens if the JWT key in `appsettings.Production.json` differs from the server
- Verify the JWT key matches the previous deployment (if any)
- All users will need to log in again if the key changed

### Static Files Not Loading

- Verify `wwwroot/` folder and all subfolders are uploaded
- Check that `wwwroot/_framework/` contains Blazor WebAssembly files
- Ensure file permissions allow read access

---

## 📋 File Structure on Server

After deployment, your server should have:

```
httpdocs/
├── pos-app.dll                    (Main application)
├── web.config                     (IIS configuration)
├── appsettings.Production.json    (Production settings)
├── pos-app.runtimeconfig.json
├── pos-app.deps.json
├── [75+ DLL files]
├── wwwroot/
│   ├── _framework/                (Blazor WebAssembly files)
│   ├── app.css
│   ├── bootstrap/
│   ├── js/
│   └── [other static files]
├── App_Data/                      (Created automatically, contains master.db)
└── logs/                          (Created automatically, contains stdout logs)
```

---

## 🔐 Security Notes

- JWT key in `appsettings.Production.json` should be kept secret
- Never commit production credentials to version control
- Ensure `App_Data/` folder is not publicly accessible (IIS should protect it)
- Use HTTPS (SSL certificate) for production

---

## 📞 Support

If you encounter issues:

1. Check the deployment logs in `logs/stdout_*.log`
2. Review the troubleshooting section above
3. Verify all files were uploaded correctly
4. Check Plesk error logs

---

**Ready to deploy?** Choose Option 1 (FTP Script) for automated deployment or Option 2 (Manual Upload) for step-by-step control.


