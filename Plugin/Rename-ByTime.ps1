<#
.SYNOPSIS
    Rename all files in current folder by time order, excluding the script itself.
.DESCRIPTION
    Files are sorted by LastWriteTime (modification time) ascending,
    then renamed to 1.ext, 2.ext, 3.ext... regardless of file type.
    To sort by creation time, replace 'LastWriteTime' with 'CreationTime'.
#>

$scriptPath = $MyInvocation.MyCommand.Path
$currentDir = Get-Location

# Get all files, exclude the script, sort by modification time (earliest first)
$allFiles = Get-ChildItem -Path $currentDir -File |
    Where-Object { $_.FullName -ne $scriptPath } |
    Sort-Object LastWriteTime   # Change to CreationTime if needed

if ($allFiles.Count -eq 0) {
    Write-Host "No files to rename." -ForegroundColor Yellow
    exit
}

# Rename to temporary names to avoid conflicts
$tempSuffix = ".tmp_ren"
$items = foreach ($file in $allFiles) {
    [PSCustomObject]@{
        OriginalPath = $file.FullName
        TempPath     = $file.FullName + $tempSuffix
        Extension    = $file.Extension
    }
}

Write-Host "Creating temporary names..."
foreach ($item in $items) {
    Rename-Item -Path $item.OriginalPath -NewName $item.TempPath -ErrorAction Stop
}

# Rename sequentially from 1
$counter = 1
foreach ($item in $items) {
    $newName = "t-$counter$($item.Extension)"
    $targetPath = Join-Path $currentDir $newName

    if (Test-Path $targetPath) {
        Write-Warning "Target '$newName' already exists, skipping: $($item.TempPath)"
    } else {
        Rename-Item -Path $item.TempPath -NewName $newName
    }
    $counter++
}

Write-Host "Renaming completed successfully." -ForegroundColor Green