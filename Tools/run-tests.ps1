<#
.SYNOPSIS
  GameClient Unity projesini batch modda derler ve testleri çalıştırır.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File Tools\run-tests.ps1
  powershell -ExecutionPolicy Bypass -File Tools\run-tests.ps1 -CompileOnly
  powershell -ExecutionPolicy Bypass -File Tools\run-tests.ps1 -Filter "GameCore.Tests.WalletTests"

.NOTES
  Çıkış kodu: 0 = hepsi geçti, 1 = test hatası, 2 = derleme/çalıştırma hatası.
  Aynı proje Unity Editör'de açıkken çalışmaz (Unity projeyi kilitler).
  Sonuçlar: GameClient/Logs/test-results.xml, GameClient/Logs/unity-test.log (git'e girmez).
#>
param(
    [ValidateSet('EditMode', 'PlayMode')]
    [string]$Platform = 'EditMode',
    [string]$Filter = '',
    [switch]$CompileOnly,
    [string]$UnityPath = ''
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'GameClient'
$logDir = Join-Path $project 'Logs'
New-Item -ItemType Directory -Force $logDir | Out-Null

# Unity sürümünü ProjectVersion.txt'den bul
if (-not $UnityPath) {
    $verLine = Get-Content (Join-Path $project 'ProjectSettings\ProjectVersion.txt') |
        Where-Object { $_ -match '^m_EditorVersion:' } | Select-Object -First 1
    $version = ($verLine -split ':')[1].Trim()
    $candidates = @(
        "$env:ProgramFiles\Unity\Hub\Editor\$version\Editor\Unity.exe",
        "$env:ProgramFiles\Unity $version\Editor\Unity.exe"
    )
    $UnityPath = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $UnityPath) { Write-Host "HATA: Unity $version bulunamadı. -UnityPath ile verin." -ForegroundColor Red; exit 2 }
}

# Proje başka bir Unity örneğinde açık mı?
$lock = Join-Path $project 'Temp\UnityLockfile'
if (Test-Path $lock) {
    try { [IO.File]::Open($lock, 'Open', 'ReadWrite', 'None').Close() }
    catch { Write-Host "HATA: Proje Unity Editör'de açık. Editörü kapatıp tekrar deneyin." -ForegroundColor Red; exit 2 }
}

$logFile = Join-Path $logDir 'unity-test.log'
$resultFile = Join-Path $logDir 'test-results.xml'
Remove-Item $resultFile -ErrorAction SilentlyContinue

$unityArgs = @('-batchmode', '-nographics', '-projectPath', "`"$project`"", '-logFile', "`"$logFile`"")
if ($CompileOnly) {
    $unityArgs += '-quit'
} else {
    $unityArgs += @('-runTests', '-testPlatform', $Platform, '-testResults', "`"$resultFile`"")
    if ($Filter) { $unityArgs += @('-testFilter', "`"$Filter`"") }
}

Write-Host "Unity: $UnityPath"
Write-Host "Mod  : $(if ($CompileOnly) { 'yalnızca derleme' } else { "$Platform testleri" })"
$sw = [Diagnostics.Stopwatch]::StartNew()
$proc = Start-Process -FilePath $UnityPath -ArgumentList $unityArgs -Wait -PassThru -NoNewWindow
$sw.Stop()
Write-Host ("Süre : {0:N0} sn, Unity çıkış kodu {1}" -f $sw.Elapsed.TotalSeconds, $proc.ExitCode)

# Derleme hatalarını logdan çıkar
$compileErrors = @()
if (Test-Path $logFile) {
    $compileErrors = @(Select-String -Path $logFile -Pattern 'error CS\d+' | ForEach-Object { $_.Line.Trim() } | Sort-Object -Unique)
}
if ($compileErrors.Count -gt 0) {
    Write-Host "`nDERLEME HATALARI ($($compileErrors.Count)):" -ForegroundColor Red
    $compileErrors | ForEach-Object { Write-Host "  $_" }
    exit 2
}

if ($CompileOnly) {
    if ($proc.ExitCode -ne 0) { Write-Host "Unity hata ile çıktı, bkz. $logFile" -ForegroundColor Red; exit 2 }
    Write-Host 'Derleme temiz.' -ForegroundColor Green
    exit 0
}

if (-not (Test-Path $resultFile)) {
    Write-Host "Sonuç dosyası oluşmadı, bkz. $logFile" -ForegroundColor Red
    exit 2
}

[xml]$xml = Get-Content $resultFile -Raw -Encoding UTF8
$run = $xml.'test-run'
Write-Host ("`nToplam {0} | Geçti {1} | Kaldı {2} | Atlandı {3}" -f $run.total, $run.passed, $run.failed, $run.skipped)

$failed = $xml.SelectNodes("//test-case[@result='Failed']")
if ($failed.Count -gt 0) {
    Write-Host "`nKALAN TESTLER:" -ForegroundColor Red
    foreach ($t in $failed) {
        Write-Host "  - $($t.fullname)" -ForegroundColor Red
        $msg = $t.failure.message.'#cdata-section'
        if ($msg) { Write-Host "      $($msg.Trim())" }
    }
    exit 1
}
if ([int]$run.total -eq 0) { Write-Host 'Hiç test bulunamadı.' -ForegroundColor Yellow; exit 2 }
Write-Host 'Tüm testler geçti.' -ForegroundColor Green
exit 0
