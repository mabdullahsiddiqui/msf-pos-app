# Deploy Critical Files Only - Forces Overwrite
# This script specifically targets critical files and ensures they're overwritten

param(
    [Parameter(Mandatory=$false)]
    [string]$FtpServer = "softxonepk.com",
    
    [Parameter(Mandatory=$true)]
    [string]$FtpUsername,
    
    [Parameter(Mandatory=$true)]
    [string]$FtpPassword
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Deploying Critical Files (Force Overwrite)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$LocalPath = "$PSScriptRoot\publish"
$RemotePath = "/httpdocs"

if (-not (Test-Path $LocalPath)) {
    Write-Host "ERROR: Local path not found: $LocalPath" -ForegroundColor Red
    exit 1
}

# Function to upload file via FTP with DELETE first to force overwrite
function Upload-File-Force {
    param(
        [string]$LocalFile,
        [string]$RemoteFile,
        [string]$FtpUri,
        [System.Net.NetworkCredential]$Credential
    )
    
    try {
        # First, try to delete the existing file to force overwrite
        try {
            $deleteRequest = [System.Net.FtpWebRequest]::Create($FtpUri)
            $deleteRequest.Credentials = $Credential
            $deleteRequest.Method = [System.Net.WebRequestMethods+Ftp]::DeleteFile
            $deleteRequest.UsePassive = $true
            $deleteResponse = $deleteRequest.GetResponse()
            $deleteResponse.Close()
        }
        catch {
            # File might not exist, which is fine
        }
        
        # Now upload the file
        $request = [System.Net.FtpWebRequest]::Create($FtpUri)
        $request.Credentials = $Credential
        $request.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
        $request.UseBinary = $true
        $request.UsePassive = $true
        
        $fileContent = [System.IO.File]::ReadAllBytes($LocalFile)
        $request.ContentLength = $fileContent.Length
        
        $requestStream = $request.GetRequestStream()
        $requestStream.Write($fileContent, 0, $fileContent.Length)
        $requestStream.Close()
        
        $response = $request.GetResponse()
        $response.Close()
        
        return $true
    }
    catch {
        Write-Host "  ERROR uploading $LocalFile : $_" -ForegroundColor Red
        return $false
    }
}

# Create credential object
$credential = New-Object System.Net.NetworkCredential($FtpUsername, $FtpPassword)
$ftpBaseUri = "ftp://$FtpServer"

Write-Host "Deploying critical files with force overwrite..." -ForegroundColor Yellow
Write-Host ""

# Critical files to deploy
$criticalFiles = @(
    "pos-app.dll",
    "web.config",
    "appsettings.Production.json",
    "pos-app.runtimeconfig.json",
    "pos-app.deps.json"
)

# Upload critical DLL files
Write-Host "Uploading critical application files..." -ForegroundColor Green
foreach ($file in $criticalFiles) {
    $localFile = Join-Path $LocalPath $file
    if (Test-Path $localFile) {
        $remoteFilePath = "$RemotePath$file"
        $ftpUri = "$ftpBaseUri$remoteFilePath"
        Write-Host "  Uploading: $file" -ForegroundColor Cyan
        Upload-File-Force -LocalFile $localFile -RemoteFile $remoteFilePath -FtpUri $ftpUri -Credential $credential | Out-Null
    }
}

# Upload all DLL files
Write-Host ""
Write-Host "Uploading DLL files..." -ForegroundColor Green
$dllFiles = Get-ChildItem -Path $LocalPath -Filter "*.dll" -File
foreach ($file in $dllFiles) {
    $relativePath = $file.Name
    $remoteFilePath = "$RemotePath$relativePath"
    $ftpUri = "$ftpBaseUri$remoteFilePath"
    Write-Host "  Uploading: $relativePath" -ForegroundColor Cyan
    Upload-File-Force -LocalFile $file.FullName -RemoteFile $remoteFilePath -FtpUri $ftpUri -Credential $credential | Out-Null
}

# Upload entire wwwroot folder (CRITICAL for Blazor changes)
Write-Host ""
Write-Host "Uploading wwwroot folder (Blazor WebAssembly files)..." -ForegroundColor Yellow
Write-Host "  This is CRITICAL for UI changes to appear!" -ForegroundColor Yellow
$wwwrootPath = Join-Path $LocalPath "wwwroot"
if (Test-Path $wwwrootPath) {
    $wwwrootFiles = Get-ChildItem -Path $wwwrootPath -Recurse -File
    $totalWwwroot = $wwwrootFiles.Count
    $count = 0
    
    foreach ($file in $wwwrootFiles) {
        $count++
        $relativePath = $file.FullName.Substring($wwwrootPath.Length).TrimStart('\', '/')
        $remoteFilePath = "$RemotePath/wwwroot/$relativePath".Replace('\', '/')
        
        # Create directory if needed
        $remoteDir = Split-Path $remoteFilePath -Parent
        if ($remoteDir -and $remoteDir -ne "$RemotePath/wwwroot") {
            try {
                $dirUri = "$ftpBaseUri$remoteDir"
                $dirRequest = [System.Net.FtpWebRequest]::Create($dirUri)
                $dirRequest.Credentials = $credential
                $dirRequest.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory
                $dirRequest.UsePassive = $true
                $dirResponse = $dirRequest.GetResponse()
                $dirResponse.Close()
            }
            catch {
                # Directory might already exist
            }
        }
        
        $ftpUri = "$ftpBaseUri$remoteFilePath"
        Write-Host "  [$count/$totalWwwroot] Uploading: wwwroot/$relativePath" -ForegroundColor Cyan
        Upload-File-Force -LocalFile $file.FullName -RemoteFile $remoteFilePath -FtpUri $ftpUri -Credential $credential | Out-Null
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Deployment Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "CRITICAL NEXT STEPS:" -ForegroundColor Yellow
Write-Host "1. RESTART APPLICATION POOL in Plesk:" -ForegroundColor White
Write-Host "   - Plesk → Websites & Domains → softxonepk.com" -ForegroundColor Gray
Write-Host "   - Click 'Application Pool' or 'IIS Settings'" -ForegroundColor Gray
Write-Host "   - Click 'Restart' or 'Recycle'" -ForegroundColor Gray
Write-Host ""
Write-Host "2. CLEAR BROWSER CACHE:" -ForegroundColor White
Write-Host "   - Press Ctrl+Shift+R (hard refresh)" -ForegroundColor Gray
Write-Host "   - Or use incognito/private browsing" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Test at: https://softxonepk.com" -ForegroundColor White
Write-Host ""

