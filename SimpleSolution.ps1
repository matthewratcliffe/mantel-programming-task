<#
.SYNOPSIS
Parses an HTTP access log and reports traffic statistics.

.DESCRIPTION
For a given log file, this script reports:
- Number of unique IP addresses
- Top 3 most visited URLs
- Top 3 most active IP addresses

ASSUMPTIONS
- Log format is Apache-style access logs
- IP address is the first token on each line
- Requested URL is the second token inside the quoted request section
- Malformed lines are skipped, not fatal
#>

param (
    [Parameter(Mandatory = $true)]
    [string]$LogFilePath
)

if (-not (Test-Path $LogFilePath)) {
    throw "Log file not found: $LogFilePath"
}

# Regex to extract IP and requested URL
$logRegex = [regex]::new(
    '^(?<ip>\d{1,3}(\.\d{1,3}){3}).*?"\w+\s+(?<url>\S+)',
    [System.Text.RegularExpressions.RegexOptions]::Compiled
)

$ipCounts  = @{}
$urlCounts = @{}

Get-Content $LogFilePath | ForEach-Object {
    $line = $_

    $match = $logRegex.Match($line)
    if (-not $match.Success) {
        return
    }

    $ip  = $match.Groups['ip'].Value
    $url = $match.Groups['url'].Value

    # Normalize full URLs to paths
    if ($url -match '^https?://') {
        try {
            $url = ([Uri]$url).AbsolutePath
        } catch {
            return
        }
    }

    $ipCounts[$ip]  = ($ipCounts[$ip]  + 1)
    $urlCounts[$url] = ($urlCounts[$url] + 1)
}

Write-Host "`n===== Log Analysis Report =====`n"

# Unique IPs
$uniqueIpCount = $ipCounts.Keys.Count
Write-Host "Unique IP addresses: $uniqueIpCount`n"

# Top 3 URLs
Write-Host "Top 3 most visited URLs:"
$urlCounts.GetEnumerator()
| Sort-Object Value -Descending
| Select-Object -First 3
| ForEach-Object {
    Write-Host "  $($_.Key) - $($_.Value) visits"
}

Write-Host ""

# Top 3 IPs
Write-Host "Top 3 most active IP addresses:"
$ipCounts.GetEnumerator()
| Sort-Object Value -Descending
| Select-Object -First 3
| ForEach-Object {
    Write-Host "  $($_.Key) - $($_.Value) requests"
}

Write-Host "`n================================"
