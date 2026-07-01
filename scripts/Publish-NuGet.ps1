# Publish-NuGet.ps1 — NetDAQmx full publish workflow
# Usage: .\scripts\Publish-NuGet.ps1 -Version 2.0.0-beta4
#
# Steps:
#   1. Bump version in NetDAQmx.csproj
#   2. Commit + tag + push to GitHub
#   3. dotnet pack -c Release (always recompiles from source)
#   4. Verify the nupkg exists at the expected path
#   5. Push to RUTester_Feed
#   6. Create GitHub pre-release with nupkg attached

param(
    [Parameter(Mandatory)]
    [string]$Version
)

$ErrorActionPreference = "Stop"

$dotnet  = "C:\Program Files\dotnet\dotnet.exe"
$gh      = "C:\Program Files\GitHub CLI\gh.exe"
$gitExe  = "C:\Program Files\Git\cmd\git.exe"
$root    = Split-Path $PSScriptRoot
$csproj  = Join-Path $root "NetDAQmx\NetDAQmx.csproj"
$artifacts = Join-Path $root "artifacts"
$nupkg   = Join-Path $artifacts "NetDAQmx.$Version.nupkg"

# ── 1. Bump version ────────────────────────────────────────────────────────────
Write-Host "`n==> Bumping version to $Version" -ForegroundColor Cyan
(Get-Content $csproj -Raw) -replace '<Version>[^<]+</Version>', "<Version>$Version</Version>" |
    Set-Content $csproj -NoNewline
Write-Host "    Updated $csproj"

# ── 2. Commit, tag, push ───────────────────────────────────────────────────────
Write-Host "`n==> Committing, tagging v$Version, pushing" -ForegroundColor Cyan
& $gitExe add "NetDAQmx/NetDAQmx.csproj"
& $gitExe commit -m "chore: bump version to $Version"
& $gitExe tag "v$Version"
& $gitExe push origin HEAD --tags
if ($LASTEXITCODE -ne 0) { throw "git push failed" }

# ── 3. Pack (Release, x86 — always recompiles) ────────────────────────────────
Write-Host "`n==> Packing Release/x86" -ForegroundColor Cyan
& $dotnet pack $csproj -c Release -p:Platform=x86 -o $artifacts
if ($LASTEXITCODE -ne 0) { throw "dotnet pack failed" }

# ── 4. Verify nupkg ───────────────────────────────────────────────────────────
if (-not (Test-Path $nupkg)) {
    throw "Expected nupkg not found: $nupkg — aborting push."
}
Write-Host "    Verified: $nupkg"

# ── 5. Push to RUTester_Feed ──────────────────────────────────────────────────
Write-Host "`n==> Pushing to RUTester_Feed" -ForegroundColor Cyan
& $dotnet nuget push --source "RUTester_Feed" --api-key az "$nupkg"
if ($LASTEXITCODE -ne 0) { throw "nuget push failed" }

# ── 6. GitHub pre-release ─────────────────────────────────────────────────────
Write-Host "`n==> Creating GitHub release v$Version" -ForegroundColor Cyan
& $gh release create "v$Version" --title "v$Version" --prerelease --notes "Version $Version" "$nupkg"
if ($LASTEXITCODE -ne 0) { throw "gh release create failed" }

Write-Host "`nDone! v$Version published to RUTester_Feed and GitHub." -ForegroundColor Green
