# Fix IIS Application Pool Settings for 500 Error

## Problem
- .NET 8.0 confirmed installed and supported
- All files present
- web.config correct
- Still getting 500 error with NO logs

## Root Cause
If no logs are generated, IIS is likely failing to start the application **before** it can write logs. This is usually an **Application Pool configuration issue**.

## Critical Fix: Application Pool Settings

### Step 1: Check Application Pool in Plesk

1. In Plesk, go to **Websites & Domains** → `softxonepk.com`
2. Look for one of these options:
   - **"Application Pool"** (direct link)
   - **"IIS Settings"** → Look for "Application Pool" section
   - **"Hosting Settings"** → Look for "Application Pool" section
   - **"Additional IIS Settings"** or **"Advanced IIS Settings"**

### Step 2: Verify These Settings

**CRITICAL SETTINGS:**

1. **.NET CLR Version:**
   - **MUST be:** `No Managed Code` or `None`
   - **NOT:** `v4.0` or `v2.0` or any version number
   - **Why:** ASP.NET Core runs its own runtime, IIS shouldn't manage it

2. **Managed Pipeline Mode:**
   - **MUST be:** `Integrated`
   - **NOT:** `Classic`

3. **Application Pool Identity:**
   - Should be: `ApplicationPoolIdentity` (default)
   - This user needs **Read** permission on all DLL files
   - This user needs **Write** permission on `App_Data` and `logs` folders

### Step 3: If You Can't Find Application Pool Settings

Some Plesk versions don't expose application pool settings directly. Try:

**Option A: Check via Hosting Settings**
- Go to **Hosting Settings** for your domain
- Look for "Microsoft ASP.NET" section
- Ensure it's configured correctly (but this is for .NET Framework, not Core)

**Option B: Contact Support Again**
Ask HosterPK support:
- "Can you check the Application Pool settings for softxonepk.com?"
- "Please verify .NET CLR Version is set to 'No Managed Code'"
- "Please verify Managed Pipeline Mode is 'Integrated'"
- "Can you check file permissions for ApplicationPoolIdentity on httpdocs folder?"

## Alternative: Try Absolute Path in web.config

If application pool is correct but still failing, try using absolute path:

Change in `web.config`:
```xml
<aspNetCore processPath="dotnet" arguments="D:\httpdocs\pos-app.dll" ... />
```

But first, ask support: "What is the full physical path to my httpdocs folder?"

Then update web.config with the full path.

## Quick Test: Verify File Permissions

In Plesk File Manager:
1. Right-click `pos-app.dll` → **Change Permissions**
2. Ensure:
   - **Owner:** Read, Execute
   - **Group:** Read, Execute  
   - **Public:** Read, Execute
3. Do the same for `web.config`
4. For `App_Data` and `logs` folders: Add **Write** permission

## Most Likely Solution

**Application Pool .NET CLR Version is set to v4.0 instead of "No Managed Code"**

This is the #1 cause of ASP.NET Core apps failing to start with no logs.

Ask support to verify and fix this setting.

