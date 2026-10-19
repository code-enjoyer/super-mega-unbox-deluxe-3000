#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
$temporaryDirectory = Join-Path ([IO.Path]::GetTempPath()) ('smud-dev-tests-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $temporaryDirectory
$settingsFile = Join-Path $temporaryDirectory '.env.local'
$global:SmudHelperTestState = @{ Calls = [Collections.Generic.List[object]]::new(); WrongOwner = $false; FailDotnet = $false }
$global:SmudHelperTestState.WrongOwner = $false
$global:SmudHelperTestState.FailDotnet = $false

function Assert-True([bool] $Condition, [string] $Message) {
    if (-not $Condition) { throw $Message }
}

# Mock both tools so these checks never start containers, delete volumes, or run builds.
function docker {
    $global:SmudHelperTestState.Calls.Add(@('docker') + $args)
    $global:LASTEXITCODE = 0
    if ($args[0] -eq 'volume' -and $args[1] -eq 'inspect') {
        $owner = if ($global:SmudHelperTestState.WrongOwner) { 'another-project' } else { 'super-mega-unbox-deluxe-3000' }
        @{ 'com.docker.compose.project' = $owner; 'com.docker.compose.volume' = 'postgres_data' } | ConvertTo-Json -Compress
    }
}

function dotnet {
    $global:SmudHelperTestState.Calls.Add(@('dotnet') + $args)
    $global:LASTEXITCODE = if ($global:SmudHelperTestState.FailDotnet) { 9 } else { 0 }
}

try {
    & "$repository/dev.ps1" setup -Scope services -EnvironmentFile $settingsFile | Out-Null
    Assert-True (Test-Path -LiteralPath $settingsFile) 'Setup must copy missing local settings.'
    $original = Get-Content -LiteralPath $settingsFile -Raw
    & "$repository/dev.ps1" setup -Scope services -EnvironmentFile $settingsFile | Out-Null
    Assert-True ((Get-Content -LiteralPath $settingsFile -Raw) -eq $original) 'Setup must preserve existing local settings.'
    $global:SmudHelperTestState.Calls.Clear()
    & "$repository/dev.ps1" check -Scope services -EnvironmentFile $settingsFile | Out-Null
    Assert-True ($global:SmudHelperTestState.Calls.Count -eq 1 -and $global:SmudHelperTestState.Calls[0][-1] -eq '--quiet') 'Copied development defaults must pass service validation.'
    "POSTGRES_ADMIN_PASSWORD='replace-with-private-admin-password'" | Set-Content -LiteralPath $settingsFile
    try {
        & "$repository/dev.ps1" dev -EnvironmentFile $settingsFile | Out-Null
        throw 'Placeholder validation unexpectedly passed.'
    }
    catch { Assert-True ($_.Exception.Message -like 'Set POSTGRES_ADMIN_PASSWORD in .env.local*') 'Placeholders must fail without exposing values.' }

    @'
POSTGRES_ADMIN_PASSWORD='fixture-only-administrator'
SMUD_DB_PASSWORD='fixture-only-application'
ASPIRE_OTLP_API_KEY='fixture-only-otlp-key-never-real'
'@ | Set-Content -LiteralPath $settingsFile

    $global:SmudHelperTestState.Calls.Clear()
    & "$repository/dev.ps1" stop -EnvironmentFile $settingsFile | Out-Null
    Assert-True ($global:SmudHelperTestState.Calls.Count -eq 1 -and $global:SmudHelperTestState.Calls[0][-1] -eq 'down') 'Stop must retain volumes.'
    Assert-True (-not ($global:SmudHelperTestState.Calls[0] -contains '--volumes')) 'Stop must never delete volumes.'

    $global:SmudHelperTestState.Calls.Clear()
    & "$repository/dev.ps1" test -Scope backend -TestFilter 'Category!=Postgres' | Out-Null
    Assert-True ($global:SmudHelperTestState.Calls.Count -eq 1 -and $global:SmudHelperTestState.Calls[0] -contains 'Category!=Postgres') 'Test filtering must reach dotnet unchanged.'

    $global:SmudHelperTestState.Calls.Clear()
    $global:SmudHelperTestState.FailDotnet = $true
    try {
        & "$repository/dev.ps1" check -Scope backend | Out-Null
        throw 'Native tool failure unexpectedly passed.'
    }
    catch { Assert-True ($_.Exception.Message -like "'dotnet' failed with exit code 9*") 'Native failures must stop the action.' }
    Assert-True ($global:SmudHelperTestState.Calls.Count -eq 1) 'Check must stop after a failed restore.'
    $global:SmudHelperTestState.FailDotnet = $false

    $global:SmudHelperTestState.Calls.Clear()
    $global:SmudHelperTestState.WrongOwner = $true
    try {
        & "$repository/dev.ps1" reset-db -EnvironmentFile $settingsFile | Out-Null
        throw 'Wrong volume ownership unexpectedly passed.'
    }
    catch { Assert-True ($_.Exception.Message -like 'Database volume ownership does not match*') 'Reset must reject another project volume.' }
    Assert-True ($global:SmudHelperTestState.Calls.Count -eq 2) 'Ownership rejection must happen before stopping or deleting.'
    $global:SmudHelperTestState.WrongOwner = $false

    $global:SmudHelperTestState.Calls.Clear()
    & "$repository/dev.ps1" reset-db -EnvironmentFile $settingsFile | Out-Null
    $removals = @($global:SmudHelperTestState.Calls | Where-Object { $_[1] -eq 'volume' -and $_[2] -eq 'rm' })
    Assert-True ($removals.Count -eq 1 -and $removals[0][3] -eq 'super-mega-unbox-deluxe-3000_postgres_data') 'Reset must target exactly the authorized database volume.'
    Assert-True ($global:SmudHelperTestState.Calls[-1] -contains '--wait') 'Reset must recreate the stack and wait for readiness.'
    Write-Output 'Development helper regression checks passed.'
}
finally {
    # The resolved generated directory is the only deletion target.
    $resolvedTemporaryDirectory = [IO.Path]::GetFullPath($temporaryDirectory)
    $temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if (-not $resolvedTemporaryDirectory.StartsWith($temporaryRoot, [StringComparison]::OrdinalIgnoreCase) -or
        (Split-Path $resolvedTemporaryDirectory -Leaf) -notlike 'smud-dev-tests-*') { throw 'Unexpected test cleanup target.' }
    Remove-Item -LiteralPath $resolvedTemporaryDirectory -Recurse -Force
    Remove-Variable SmudHelperTestState -Scope Global
}
