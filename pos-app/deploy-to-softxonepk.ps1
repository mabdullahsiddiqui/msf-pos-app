# PowerShell Script to Deploy Unified Blazor App to softxonepk.com
# This script uploads all files from the publish folder to the root of softxonepk.com via FTP

param(
    [Parameter(Mandatory=$false)]
    [string]$FtpServer = "softxonepk.com",
    
    [Parameter(Mandatory=$true)]
    [string]$FtpUsername,
    
    [Parameter(Mandatory=$true)]
    [string]$FtpPassword,
    
    [Parameter(Mandatory=$false)]
    [string]$LocalPath = "$PSScriptRoot\publish",
    
    [Parameter(Mandatory=$false)]
    [string]$RemotePath = "/httpdocs"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Deploying Blazor App to softxonepk.com" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Validate local path
if (-not (Test-Path $LocalPath)) {
    Write-Host "ERROR: Local path not found: $LocalPath" -ForegroundColor Red
    exit 1
}

Write-Host "Local Path: $LocalPath" -ForegroundColor Green
Write-Host "Remote Path: $RemotePath" -ForegroundColor Green
Write-Host "FTP Server: $FtpServer" -ForegroundColor Green
Write-Host ""

# Function to upload file via FTP
function Upload-File {
    param(
        [string]$LocalFile,
        [string]$RemoteFile,
        [string]$FtpUri,
        [System.Net.NetworkCredential]$Credential
    )
    
    try {
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

# Function to create directory via FTP
function Create-FtpDirectory {
    param(
        [string]$DirectoryPath,
        [string]$FtpBaseUri,
        [System.Net.NetworkCredential]$Credential
    )
    
    try {
        $uri = "$FtpBaseUri$DirectoryPath"
        $request = [System.Net.FtpWebRequest]::Create($uri)
        $request.Credentials = $Credential
        $request.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory
        $request.UsePassive = $true
        
        try {
            $response = $request.GetResponse()
            $response.Close()
            Write-Host "  Created directory: $DirectoryPath" -ForegroundColor Yellow
        }
        catch {
            # Directory might already exist, which is fine
            if ($_.Exception.Message -notlike "*550*") {
                Write-Host "  Directory may already exist: $DirectoryPath" -ForegroundColor Yellow
            }
        }
        return $true
    }
    catch {
        return $false
    }
}

# Create credential object
$credential = New-Object System.Net.NetworkCredential($FtpUsername, $FtpPassword)
$ftpBaseUri = "ftp://$FtpServer"

Write-Host "Connecting to FTP server..." -ForegroundColor Yellow

# Get all files to upload
$files = Get-ChildItem -Path $LocalPath -Recurse -File
$totalFiles = $files.Count
$uploadedFiles = 0
$failedFiles = 0

Write-Host "Found $totalFiles files to upload" -ForegroundColor Green
Write-Host ""

# Upload files
foreach ($file in $files) {
    $relativePath = $file.FullName.Substring($LocalPath.Length).TrimStart('\', '/')
    $remoteFilePath = "$RemotePath/$relativePath".Replace('\', '/')
    
    # Create directory structure if needed
    $remoteDir = Split-Path $remoteFilePath -Parent
    if ($remoteDir -and $remoteDir -ne $RemotePath) {
        $dirPath = $remoteDir.Replace($RemotePath, "").Replace('\', '/')
        Create-FtpDirectory -DirectoryPath "$RemotePath$dirPath" -FtpBaseUri $ftpBaseUri -Credential $credential | Out-Null
    }
    
    # Upload file
    $ftpUri = "$ftpBaseUri$remoteFilePath"
    Write-Host "[$($uploadedFiles + 1)/$totalFiles] Uploading: $relativePath" -ForegroundColor Cyan
    
    if (Upload-File -LocalFile $file.FullName -RemoteFile $remoteFilePath -FtpUri $ftpUri -Credential $credential) {
        $uploadedFiles++
    } else {
        $failedFiles++
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Deployment Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Total Files: $totalFiles" -ForegroundColor White
Write-Host "Uploaded: $uploadedFiles" -ForegroundColor Green
Write-Host "Failed: $failedFiles" -ForegroundColor $(if ($failedFiles -gt 0) { "Red" } else { "Green" })
Write-Host ""

if ($failedFiles -eq 0) {
    Write-Host "✅ Deployment completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Verify App_Data folder exists on server (or will be created automatically)" -ForegroundColor White
    Write-Host "2. Ensure App_Data has write permissions" -ForegroundColor White
    Write-Host "3. Test application at: https://softxonepk.com" -ForegroundColor White
} else {
    Write-Host "⚠️  Deployment completed with errors. Please review failed files above." -ForegroundColor Yellow
}

