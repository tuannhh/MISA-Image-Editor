[CmdletBinding()]
param(
    [string]$PythonPath = $env:MISA_PYTHON,
    [string]$DotnetPath = 'C:\Program Files\dotnet\dotnet.exe',
    [string]$CtestPath = 'C:\Program Files\CMake\bin\ctest.exe',
    [string]$OutputPath = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if ([string]::IsNullOrWhiteSpace($PythonPath) -or -not (Test-Path -LiteralPath $PythonPath)) {
    throw 'Set MISA_PYTHON or pass -PythonPath to the configured Python runtime.'
}
foreach ($tool in @($DotnetPath, $CtestPath)) {
    if (-not (Test-Path -LiteralPath $tool)) { throw "Required tool is missing: $tool" }
}
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $root 'fixtures\p0-release-gate.json'
}

$results = [System.Collections.Generic.List[object]]::new()
function Invoke-GateStep {
    param([string]$Name, [scriptblock]$Action)
    $started = [DateTimeOffset]::UtcNow
    & $Action
    if ($LASTEXITCODE -ne 0) { throw "Gate step '$Name' failed with exit code $LASTEXITCODE" }
    $elapsed = ([DateTimeOffset]::UtcNow - $started).TotalSeconds
    $results.Add([ordered]@{ name = $Name; status = 'passed'; seconds = [Math]::Round($elapsed, 3) })
}

Invoke-GateStep 'python-dependencies' { & $PythonPath (Join-Path $root 'tools\verify_dependencies.py') }
Invoke-GateStep 'python-regression' { & $PythonPath '-m' 'unittest' 'discover' '-s' (Join-Path $root 'tests') '-v' }
Invoke-GateStep 'python-catalog-runner' { & $PythonPath (Join-Path $root 'tools\run_p0.py') }
Invoke-GateStep 'raw-fixtures' { & $PythonPath (Join-Path $root 'tools\verify_raw_fixtures.py') }
Invoke-GateStep 'camera-heif-fixture' { & $PythonPath (Join-Path $root 'tools\verify_camera_heif_fixtures.py') }
Invoke-GateStep 'offline-mask' { & $PythonPath (Join-Path $root 'tools\verify_mask_model.py') }
Invoke-GateStep 'lensfun-coverage' { & $PythonPath (Join-Path $root 'tools\lensfun_coverage.py') }
Invoke-GateStep 'preview-bridge' { & $PythonPath (Join-Path $root 'tools\verify_preview_bridge.py') }
Invoke-GateStep 'color-baseline' { & $PythonPath (Join-Path $root 'tools\verify_color_baseline.py') }
Invoke-GateStep 'dotnet-clean' { & $DotnetPath 'clean' (Join-Path $root 'windows\MisaImageEditor.slnx') '--configuration' 'Release' '--verbosity' 'minimal' }
Invoke-GateStep 'dotnet-build' { & $DotnetPath 'build' (Join-Path $root 'windows\MisaImageEditor.slnx') '--configuration' 'Release' '--no-restore' '--verbosity' 'minimal' }
Invoke-GateStep 'domain-smoke' { & $DotnetPath 'run' '--project' (Join-Path $root 'windows\src\MisaImageEditor.Domain.Smoke\MisaImageEditor.Domain.Smoke.csproj') '--configuration' 'Release' '--no-build' '--no-restore' }
Invoke-GateStep 'native-ctest' { & $CtestPath '--test-dir' (Join-Path $root 'windows\native\ImageWorker\build-final') '-C' 'Release' '--output-on-failure' }

$report = [ordered]@{
    schema = 1
    completed_at_utc = [DateTimeOffset]::UtcNow.ToString('O')
    python = $PythonPath
    dotnet = $DotnetPath
    ctest = $CtestPath
    steps = @($results)
    passed = $true
}
$report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $OutputPath -Encoding utf8
Write-Output $OutputPath
