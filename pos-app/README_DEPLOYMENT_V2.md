# Version 2 Deployment - Ready for Upload

## ✅ Preparation Complete

All files have been built and prepared for deployment to softxonepk.com.

## 📍 What's Ready

### Build Output
- **Location:** `D:\POS\pos-app\pos-app\publish\`
- **Status:** ✅ Built successfully in Release mode
- **Total Files:** ~80+ files
- **Total Size:** ~50-60 MB

### Documentation Created
1. **DEPLOYMENT_VERSION_2.md** - Complete detailed deployment guide
2. **DEPLOYMENT_CHECKLIST_V2.md** - Quick checklist for deployment
3. **DEPLOYMENT_SUMMARY_V2.txt** - Summary of deployment information
4. **README_DEPLOYMENT_V2.md** - This file

### Key Verifications
- ✅ web.config verified - identical to server (safe to replace)
- ✅ appsettings.Production.json contains JWT key
- ✅ All critical files present in publish folder
- ✅ wwwroot folder ready for upload

## 🚀 Next Steps (Manual Upload Required)

Since you're using Plesk File Manager, you need to manually upload the files:

1. **Open Plesk File Manager**
   - Log into Plesk → softxonepk.com
   - Navigate to `httpdocs/` folder

2. **Upload Files**
   - Upload ALL files from `D:\POS\pos-app\pos-app\publish\`
   - **EXCEPT:** Skip any `App_Data` folder (preserve server's existing one)
   - Upload entire `wwwroot/` folder (overwrite existing)

3. **Upload web.config**
   - Upload `web.config` from publish folder
   - Safe to replace (identical to server version)

4. **Verify**
   - Check that `App_Data/master.db` still exists after upload
   - Test application at https://softxonepk.com

## 📋 Quick Reference

**Source:** `D:\POS\pos-app\pos-app\publish\`  
**Target:** Plesk File Manager → `httpdocs/`  
**Preserve:** `App_Data/`, `logs/`  
**Replace:** Everything else

## ⚠️ Important Reminders

1. **DO NOT** delete or overwrite `App_Data/` folder - it contains your production database
2. **web.config** is safe to replace - versions are identical
3. **JWT Key** in appsettings.Production.json must match server or users will be logged out
4. Test thoroughly after deployment

## 📖 Detailed Instructions

See **DEPLOYMENT_VERSION_2.md** for complete step-by-step instructions.

---

**Status:** ✅ Ready for Deployment  
**Action Required:** Manual upload via Plesk File Manager

