#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$repository = Split-Path $PSScriptRoot -Parent
$smokeProject = 'smud-smoke-' + [guid]::NewGuid().ToString('N')
$environmentFile = Join-Path $PSScriptRoot 'smoke-fixture.env'
$overrideFile = Join-Path $PSScriptRoot 'smoke-fixture.compose.yaml'

function Compose([string[]] $Arguments) {
    $global:LASTEXITCODE = 0
    & docker compose --project-name $smokeProject --env-file $environmentFile -f deploy/compose.yaml -f $overrideFile @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Smoke Compose command failed with exit code $LASTEXITCODE." }
}
function Check([bool] $Condition, [string] $Message) { if (-not $Condition) { throw $Message } }

Push-Location $repository
try {
    @'
POSTGRES_ADMIN_PASSWORD='fixture-only-admin'
SMUD_DB_PASSWORD='fixture-only-app;with-delimiters'
ASPIRE_OTLP_API_KEY='fixture-only-otlp-key-never-real'
'@ | Set-Content -LiteralPath $environmentFile
    @"
services:
  postgres:
    container_name: $smokeProject-postgres
    ports: !reset []
  api:
    container_name: $smokeProject-api
    ports: !override ["127.0.0.1::8080"]
  client:
    container_name: $smokeProject-client
    ports: !override ["127.0.0.1::80"]
  aspire:
    ports: !override ["127.0.0.1::18888", "127.0.0.1::18890"]
"@ | Set-Content -LiteralPath $overrideFile
    Compose @('up', '-d', '--wait', '--no-build')
    $api = 'http://' + ((Compose @('port', 'api', '8080')) -join '').Trim()
    $client = 'http://' + ((Compose @('port', 'client', '80')) -join '').Trim()
    $otlp = 'http://' + ((Compose @('port', 'aspire', '18890')) -join '').Trim()
    $dashboard = 'http://' + ((Compose @('port', 'aspire', '18888')) -join '').Trim()
    $status = Invoke-WebRequest "$api/api/status" -Headers @{ 'X-Correlation-ID' = 'container-smoke' }
    Check (($status.Content | ConvertFrom-Json).status -eq 'ok') 'API status failed.'
    Check (($status.Headers['X-Correlation-ID'] -join ',') -eq 'container-smoke') 'Correlation header was lost.'
    Check ((Invoke-WebRequest "$api/health").Content -eq 'Healthy') 'Database readiness failed.'
    Check ((Invoke-RestMethod "$api/loot-sources").Count -eq 2) 'Loot source contract failed.'
    $created = Invoke-WebRequest "$api/items" -Method Post
    Check ($created.StatusCode -eq 201) 'Item creation failed.'
    $retrieved = Invoke-WebRequest ([uri]::new([uri]$api, [string]@($created.Headers.Location)[0]))
    Check ($created.Content -eq $retrieved.Content) 'Persisted item snapshot changed on retrieval.'
    Check ((Invoke-WebRequest $client).StatusCode -eq 200) 'Client container did not serve the app.'
    $userId = ((Compose @('exec', '-T', 'api', 'id', '-u')) -join '').Trim()
    Check ($userId -ne '0') 'API must run as non-root.'
    Compose @('exec', '-T', 'api', 'sh', '-c', 'test -w /app/logs && test -n "$(ls /app/logs/api-*.log)"')
    $privileged = ((Compose @('exec', '-T', 'postgres', 'psql', '-U', 'postgres', '-d', 'postgres', '-Atc', "SELECT rolsuper OR rolcreatedb OR rolcreaterole FROM pg_roles WHERE rolname = 'smud_user'")) -join '').Trim()
    Check ($privileged -eq 'f') 'Application database login is overprivileged.'
    $migrationId = ((Compose @('ps', '--all', '-q', 'api-migrate')) -join '').Trim()
    $migrationExit = & docker inspect --format '{{.State.ExitCode}}' $migrationId
    Check ($LASTEXITCODE -eq 0 -and $migrationExit -eq '0') 'Migration job did not exit successfully.'
    $unauthenticated = Invoke-WebRequest "$otlp/v1/logs" -Method Post -ContentType 'application/x-protobuf' -Body ([byte[]]::new(0)) -SkipHttpErrorCheck
    Check ($unauthenticated.StatusCode -eq 401) 'OTLP ingestion must reject unauthenticated requests.'
    $authenticated = Invoke-WebRequest "$otlp/v1/logs" -Method Post -ContentType 'application/x-protobuf' -Body ([byte[]]::new(0)) -Headers @{ 'x-otlp-api-key' = 'fixture-only-otlp-key-never-real' } -SkipHttpErrorCheck
    Check ($authenticated.StatusCode -eq 200) 'Authenticated OTLP ingestion failed.'
    $httpHandler = [Net.Http.HttpClientHandler]::new()
    $httpHandler.AllowAutoRedirect = $false
    $httpClient = [Net.Http.HttpClient]::new($httpHandler)
    try {
        $ui = $httpClient.GetAsync($dashboard).GetAwaiter().GetResult()
        Check ([int]$ui.StatusCode -in @(200, 302)) 'Aspire UI was not available.'
    }
    finally { $httpClient.Dispose(); $httpHandler.Dispose() }
    & docker run --rm --network none -e ASPNETCORE_ENVIRONMENT=Production -e 'ConnectionStrings__SmudDatabase=Host=127.0.0.1;Port=1;Database=fixture;Username=fixture;Timeout=1' smud-api:latest --migrate 2>&1 | Out-Null
    Check ($LASTEXITCODE -eq 1) 'Failed migrations must return exit code 1.'
    Write-Output 'Isolated Compose smoke passed: migration, status, health, items, client, non-root/logging, database privileges, authenticated dashboard ingestion, and migration failure exit code.'
}
finally {
    # This random project owns only the containers/volume created above.
    if ($smokeProject -notmatch '^smud-smoke-[a-f0-9]{32}$') { throw 'Unexpected smoke cleanup project.' }
    try { Compose @('down', '--volumes') }
    finally {
        Remove-Item -LiteralPath $environmentFile, $overrideFile -Force -ErrorAction SilentlyContinue
        Pop-Location
    }
}
