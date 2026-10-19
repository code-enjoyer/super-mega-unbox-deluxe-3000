#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('help', 'setup', 'check', 'test', 'dev', 'stop', 'reset-db')]
    [string] $Action = 'help',
    [ValidateSet('all', 'backend', 'services')]
    [string] $Scope = 'all',
    [string] $TestFilter,
    [string] $EnvironmentFile = '.env.local'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$projectName = 'super-mega-unbox-deluxe-3000'
$databaseVolume = 'super-mega-unbox-deluxe-3000_postgres_data'
$solution = 'backend/SuperMegaUnboxDeluxe.slnx'

function Invoke-Tool([string] $Name, [string[]] $Arguments) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) { throw "Missing tool '$Name'. See docs/development.md." }
    $global:LASTEXITCODE = 0
    & $Name @Arguments
    if ($LASTEXITCODE -ne 0) { throw "'$Name' failed with exit code $LASTEXITCODE. Stopped '$Action'." }
}

function Invoke-Compose([string[]] $Arguments, [switch] $Stopping) {
    $settingsFile = $EnvironmentFile
    if ($Stopping -and -not (Test-Path -LiteralPath $settingsFile)) { $settingsFile = '.env.example' }
    Invoke-Tool docker (@('compose', '--project-name', $projectName, '--env-file', $settingsFile, '-f', 'deploy/compose.yaml') + $Arguments)
}

function Test-ServiceSettings {
    if (-not (Test-Path -LiteralPath $EnvironmentFile)) { throw 'Run ./dev.ps1 setup -Scope services to copy the local development defaults.' }
    $settings = Get-Content -LiteralPath $EnvironmentFile -Raw
    foreach ($name in @('POSTGRES_ADMIN_PASSWORD', 'SMUD_DB_PASSWORD', 'ASPIRE_OTLP_API_KEY')) {
        $match = [regex]::Match($settings, "(?m)^$name\s*=\s*(.+)$")
        if (-not $match.Success -or $match.Groups[1].Value -match 'replace-with-' -or $match.Groups[1].Value.Trim() -in @("''", '""')) {
            throw "Set $name in .env.local. Values are not displayed."
        }
        if ($name -eq 'ASPIRE_OTLP_API_KEY' -and $match.Groups[1].Value.Trim().Trim("'", '"') -notmatch '\A[A-Za-z0-9_-]{22,}\z') {
            throw 'ASPIRE_OTLP_API_KEY must contain at least 22 URL-safe characters.'
        }
    }
    Invoke-Compose @('config', '--quiet')
}

function Test-Backend {
    $arguments = @('test', $solution, '-c', 'Release', '--no-restore')
    if ($TestFilter) { $arguments += @('--filter', $TestFilter) }
    Invoke-Tool dotnet $arguments
}

Push-Location $PSScriptRoot
try {
    if ($TestFilter -and ($Action -notin @('test', 'check') -or $Scope -ne 'backend')) {
        throw '-TestFilter requires check/test -Scope backend.'
    }
    if ($Action -in @('dev', 'stop', 'reset-db') -and $Scope -eq 'backend') { throw "$Action operates on services." }
    switch ($Action) {
        help {
            Write-Output @'
Usage: ./dev.ps1 [setup|check|test|dev|stop|reset-db] [-Scope backend|services|all]
setup     Restore backend dependencies/tools; copy missing local development settings.
check     Verify backend formatting, locked restore, Release build/tests; validate service settings.
test      Run backend tests. -TestFilter is supported with -Scope backend.
dev       Build/start Compose services and the app; run migrations before API startup.
stop      Stop this project's stack and retain volumes.
reset-db  Destroy only this project's development PostgreSQL volume, then rebuild/start.
Use -TestFilter 'Category!=Postgres' with -Scope backend to run without Docker.
See docs/development.md and deploy/README.md. Local values are never printed.
'@
        }
        setup {
            if ($Scope -in @('all', 'backend')) {
                Invoke-Tool dotnet @('restore', $solution)
                Invoke-Tool dotnet @('tool', 'restore')
            }
            if ($Scope -in @('all', 'services')) {
                if (-not (Test-Path -LiteralPath $EnvironmentFile)) {
                    Copy-Item -LiteralPath '.env.example' -Destination $EnvironmentFile
                    Write-Output 'Created local settings from .env.example. The development defaults are ready to use.'
                }
            }
        }
        check {
            if ($Scope -in @('all', 'backend')) {
                Invoke-Tool dotnet @('restore', $solution, '--locked-mode')
                Invoke-Tool dotnet @('format', $solution, '--verify-no-changes', '--no-restore')
                Invoke-Tool dotnet @('build', $solution, '-c', 'Release', '--no-restore')
                Test-Backend
            }
            if ($Scope -in @('all', 'services')) { Test-ServiceSettings }
        }
        test {
            if ($Scope -eq 'services') { throw 'Use check -Scope services for Compose validation.' }
            Test-Backend
        }
        dev {
            Test-ServiceSettings
            Invoke-Compose @('up', '-d', '--wait', '--build')
        }
        stop { Invoke-Compose @('down') -Stopping }
        reset-db {
            Test-ServiceSettings
            # Verify ownership before stopping services or removing anything.
            $labelsJson = (Invoke-Tool docker @('volume', 'inspect', $databaseVolume, '--format', '{{json .Labels}}')) -join "`n"
            $volume = $labelsJson | ConvertFrom-Json
            if ($volume.'com.docker.compose.project' -ne $projectName -or $volume.'com.docker.compose.volume' -ne 'postgres_data') {
                throw 'Database volume ownership does not match this project. Nothing was removed.'
            }
            Write-Output "Resetting development database volume $databaseVolume; existing development items will be lost."
            Invoke-Compose @('down')
            Invoke-Tool docker @('volume', 'rm', $databaseVolume)
            Invoke-Compose @('up', '-d', '--wait', '--build')
        }
    }
}
finally { Pop-Location }
