param(
    [ValidateSet("all", "pipeline", "micro", "eviction", "provider")]
    [string]$Suite = "all",
    [switch]$SkipDocker,
    [switch]$KeepContainers
)

$ErrorActionPreference = "Stop"

$BenchmarksDir = $PSScriptRoot
$RepoRoot = Split-Path $BenchmarksDir -Parent
$ComposeFile = Join-Path $BenchmarksDir "docker-compose.yml"
$ProjectName = "nexgen-benchmarks"
$StartedCompose = $false

function Test-TcpPort {
    param(
        [string]$TargetHost,
        [int]$Port,
        [int]$TimeoutMs = 500
    )

    try {
        $client = New-Object System.Net.Sockets.TcpClient
        $async = $client.BeginConnect($TargetHost, $Port, $null, $null)
        $ok = $async.AsyncWaitHandle.WaitOne($TimeoutMs, $false)
        if (-not $ok) {
            $client.Close()
            return $false
        }

        $client.EndConnect($async)
        $client.Close()
        return $true
    }
    catch {
        return $false
    }
}

function Wait-TcpPort {
    param(
        [string]$TargetHost,
        [int]$Port,
        [int]$TimeoutSeconds = 60
    )

    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $TimeoutSeconds) {
        if (Test-TcpPort -TargetHost $TargetHost -Port $Port -TimeoutMs 500) {
            return $true
        }
        Start-Sleep -Seconds 1
    }

    return $false
}

$needsDocker = ($Suite -eq "all" -or $Suite -eq "provider") -and -not $SkipDocker

try {
    if ($needsDocker) {
        $redisReady = Test-TcpPort -TargetHost "127.0.0.1" -Port 6379
        $garnetReady = Test-TcpPort -TargetHost "127.0.0.1" -Port 6380

        if (-not $redisReady -or -not $garnetReady) {
            Write-Host "Starting Redis (6379) and Garnet (6380) via docker compose..."
            docker compose -p $ProjectName -f $ComposeFile up -d
            if ($LASTEXITCODE -ne 0) {
                throw "docker compose up failed with exit code $LASTEXITCODE"
            }
            $StartedCompose = $true
        }
        else {
            Write-Host "Redis and Garnet already reachable; skipping docker compose up."
        }

        if (-not (Wait-TcpPort -TargetHost "127.0.0.1" -Port 6379)) {
            throw "Timed out waiting for Redis on localhost:6379"
        }
        if (-not (Wait-TcpPort -TargetHost "127.0.0.1" -Port 6380)) {
            throw "Timed out waiting for Garnet on localhost:6380"
        }

        Write-Host "Redis and Garnet are ready."
    }

    $project = Join-Path $BenchmarksDir "NexGen.MediatR.Extensions.Caching.Benchmark\NexGen.MediatR.Extensions.Caching.Benchmark.csproj"
    Write-Host "Running benchmark suite '$Suite'..."
    dotnet run -c Release --project $project --no-launch-profile -- $Suite
    if ($LASTEXITCODE -ne 0) {
        throw "Benchmark run failed with exit code $LASTEXITCODE"
    }
}
finally {
    if ($StartedCompose -and -not $KeepContainers) {
        Write-Host "Stopping benchmark containers..."
        docker compose -p $ProjectName -f $ComposeFile down
    }
}
