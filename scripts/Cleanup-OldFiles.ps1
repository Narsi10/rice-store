<#
.SYNOPSIS
    Deletes files older than a retention period from a folder on one or more
    remote servers, using PowerShell remoting.

.DESCRIPTION
    Connects to each server in the list via Invoke-Command and removes files in
    the target folder whose LastWriteTime is older than the retention window.
    Supports -WhatIf (via the script's TestMode switch) so you can preview what
    would be deleted before actually deleting anything.

    Designed to be scheduled (Windows Task Scheduler) to run daily, so log/temp
    folders don't fill the disk and cause outages.

.EXAMPLE
    # Preview only (safe) — logs what WOULD be deleted, deletes nothing:
    .\Cleanup-OldFiles.ps1 -TestMode

.EXAMPLE
    # Actually delete files older than 30 days on all listed servers:
    .\Cleanup-OldFiles.ps1
#>

param(
    # Servers to clean up. Replace with your real server names.
    [string[]] $Servers = @('APPSERVER01', 'APPSERVER02', 'APPSERVER03'),

    # Folder on each server to clean.
    [string] $TargetPath = 'C:\Logs',

    # Delete files older than this many days.
    [int] $RetentionDays = 30,

    # When set, only reports what would be deleted (no deletion).
    [switch] $TestMode
)

# The block that runs ON each remote server.
$cleanupBlock = {
    param($path, $days, $preview)

    if (-not (Test-Path $path)) {
        Write-Output "[$env:COMPUTERNAME] Path not found: $path"
        return
    }

    $cutoff = (Get-Date).AddDays(-$days)
    $oldFiles = Get-ChildItem -Path $path -Recurse -File |
        Where-Object { $_.LastWriteTime -lt $cutoff }

    if (-not $oldFiles) {
        Write-Output "[$env:COMPUTERNAME] No files older than $days days."
        return
    }

    if ($preview) {
        Write-Output "[$env:COMPUTERNAME] WOULD delete $($oldFiles.Count) file(s):"
        $oldFiles | ForEach-Object { Write-Output "   $($_.FullName) ($($_.LastWriteTime))" }
    }
    else {
        $count = 0
        foreach ($file in $oldFiles) {
            try {
                Remove-Item -Path $file.FullName -Force -ErrorAction Stop
                $count++
            }
            catch {
                Write-Output "[$env:COMPUTERNAME] Failed to delete $($file.FullName): $_"
            }
        }
        Write-Output "[$env:COMPUTERNAME] Deleted $count file(s) older than $days days."
    }
}

# Run against every server. Errors on one server don't stop the others.
foreach ($server in $Servers) {
    try {
        Invoke-Command -ComputerName $server -ScriptBlock $cleanupBlock `
            -ArgumentList $TargetPath, $RetentionDays, $TestMode.IsPresent -ErrorAction Stop
    }
    catch {
        Write-Output "[$server] Could not connect or run: $_"
    }
}
