<#
.SYNOPSIS
    Rename current folder files by type, excluding the script itself.
#>

$scriptPath = $MyInvocation.MyCommand.Path
$currentDir = Get-Location

# Get all files, excluding the script itself
$allFiles = Get-ChildItem -Path $currentDir -File | Where-Object { $_.FullName -ne $scriptPath }

if ($allFiles.Count -eq 0) {
    Write-Host "No files to rename." -ForegroundColor Yellow
    exit
}

# Rename to temporary names to avoid collision
$tempSuffix = ".tmp_ren"
$items = foreach ($file in $allFiles) {
    [PSCustomObject]@{
        OriginalPath = $file.FullName
        TempPath     = $file.FullName + $tempSuffix
        OrigExt      = $file.Extension
        OrigBaseName = $file.BaseName
    }
}

Write-Host "Creating temporary names..."
foreach ($item in $items) {
    Rename-Item -Path $item.OriginalPath -NewName $item.TempPath -ErrorAction Stop
}

# Group by extension and rename sequentially
$groups = $items | Group-Object { $_.OrigExt.ToLower() }

foreach ($group in $groups) {
    $ext = $group.Name
    $sorted = $group.Group | Sort-Object OrigBaseName
    $counter = 1
    foreach ($entry in $sorted) {
        $newName = "$counter$ext"
        $targetPath = Join-Path $currentDir $newName
        if (Test-Path $targetPath) {
            Write-Warning "Target '$newName' already exists, skipping: $($entry.TempPath)"
        }
        else {
            Rename-Item -Path $entry.TempPath -NewName $newName
        }
        $counter++
    }
}

Write-Host "Renaming completed successfully." -ForegroundColor Green