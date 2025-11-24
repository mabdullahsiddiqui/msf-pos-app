# Troubleshooting Script for Deployment Issues
# This script helps verify deployment and provides steps to fix common issues

param(
    [Parameter(Mandatory=$true)]
    [string]$FtpServer = "softxonepk.com",
    
    [Parameter(Mandatory=$true)]
    [string]$FtpUsername,
    
    [Parameter(Mandatory=$true)]
    [string]$FtpPassword
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Deployment Troubleshooting" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Function to get file info via FTP
function Get-FtpFileInfo {
    param(
        [string]$FilePath,
        [string]$FtpBaseUri,
        [System.Net.NetworkCredential]$Credential
    )
    
    try {
        $uri = "$FtpBaseUri$FilePath"
        $request = [System.Net.FtpWebRequest]::Create($uri)
        $request.Credentials = $Credential
        $request.Method = [System.Net.WebRequestMethods+Ftp]::GetDateTimestamp
        $request.UsePassive = $true
        
        $response = $request.GetResponse()
        $lastModified = $response.LastModified
        $response.Close()
        
        return $lastModified
    }
    catch {
        return $null
    }
}

# Create credential object
$credential = New-Object System.Net.NetworkCredential($FtpUsername, $FtpPassword)
$ftpBaseUri = "ftp://$FtpServer"

Write-Host "Checking critical files on server..." -ForegroundColor Yellow
Write-Host ""

# Check local file timestamps
$localDll = Get-Item "publish\pos-app.dll"
$localWebConfig = Get-Item "publish\web.config"
$localAppSettings = Get-Item "publish\appsettings.Production.json"

Write-Host "Local Files:" -ForegroundColor Green
Write-Host "  pos-app.dll: $($localDll.LastWriteTime) ($([math]::Round($localDll.Length/1KB, 2)) KB)" -ForegroundColor White
Write-Host "  web.config: $($localWebConfig.LastWriteTime)" -ForegroundColor White
Write-Host "  appsettings.Production.json: $($localAppSettings.LastWriteTime)" -ForegroundColor White
Write-Host ""

# Check remote file timestamps
Write-Host "Remote Files (on server):" -ForegroundColor Green
$remoteDllTime = Get-FtpFileInfo -FilePath "/pos-app.dll" -FtpBaseUri $ftpBaseUri -Credential $credential
$remoteWebConfigTime = Get-FtpFileInfo -FilePath "/web.config" -FtpBaseUri $ftpBaseUri -Credential $credential
$remoteAppSettingsTime = Get-FtpFileInfo -FilePath "/appsettings.Production.json" -FtpBaseUri $ftpBaseUri -Credential $credential

if ($remoteDllTime) {
    Write-Host "  pos-app.dll: $remoteDllTime" -ForegroundColor $(if ($remoteDllTime -ge $localDll.LastWriteTime) { "Green" } else { "Yellow" })
} else {
    Write-Host "  pos-app.dll: NOT FOUND or cannot access" -ForegroundColor Red
}

if ($remoteWebConfigTime) {
    Write-Host "  web.config: $remoteWebConfigTime" -ForegroundColor $(if ($remoteWebConfigTime -ge $localWebConfig.LastWriteTime) { "Green" } else { "Yellow" })
} else {
    Write-Host "  web.config: NOT FOUND or cannot access" -ForegroundColor Red
}

if ($remoteAppSettingsTime) {
    Write-Host "  appsettings.Production.json: $remoteAppSettingsTime" -ForegroundColor $(if ($remoteAppSettingsTime -ge $localAppSettings.LastWriteTime) { "Green" } else { "Yellow" })
} else {
    Write-Host "  appsettings.Production.json: NOT FOUND or cannot access" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Troubleshooting Steps" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "If files are outdated or missing, try these steps:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. RESTART APPLICATION POOL (REQUIRED):" -ForegroundColor Cyan
Write-Host "   - Log into Plesk" -ForegroundColor White
Write-Host "   - Go to: Websites & Domains → softxonepk.com" -ForegroundColor White
Write-Host "   - Click 'Application Pool' or 'IIS Settings'" -ForegroundColor White
Write-Host "   - Click 'Restart' or 'Recycle' application pool" -ForegroundColor White
Write-Host "   - This is CRITICAL - IIS caches DLL files!" -ForegroundColor Yellow
Write-Host ""
Write-Host "2. CLEAR BROWSER CACHE:" -ForegroundColor Cyan
Write-Host "   - Press Ctrl+Shift+R (hard refresh)" -ForegroundColor White
Write-Host "   - Or clear cache for softxonepk.com" -ForegroundColor White
Write-Host "   - Try incognito/private browsing mode" -ForegroundColor White
Write-Host ""
Write-Host "3. VERIFY WWWROOT FOLDER:" -ForegroundColor Cyan
Write-Host "   - Check that wwwroot/_framework/ files were uploaded" -ForegroundColor White
Write-Host "   - Blazor WebAssembly files must be updated for UI changes" -ForegroundColor White
Write-Host ""
Write-Host "4. CHECK APPLICATION LOGS:" -ForegroundColor Cyan
Write-Host "   - Plesk → Files → logs/stdout_*.log" -ForegroundColor White
Write-Host "   - Look for any error messages" -ForegroundColor White
Write-Host ""

