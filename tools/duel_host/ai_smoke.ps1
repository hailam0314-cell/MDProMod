# 无头验证：p0 用 auto 应答，p1 用真 WindBot。用法：ai_smoke.ps1 <aiDeckYdk> <aiDeckName> [seconds]
param(
    [string]$AiDeck = "Data/Windbot/Decks/AI_BlueEyes.ydk",
    [string]$AiName = "AI_BlueEyes",
    [int]$Seconds = 75
)

$dotnet = 'D:\MDProMod\tools\dotnet\dotnet.exe'
$dll = 'D:\MDProMod\tools\duel_host\bin\Release\net8.0\duel_host.dll'
$staging = 'D:\MDProMod\tools\ygo_tcp_probe\staging'

$psi = [System.Diagnostics.ProcessStartInfo]::new()
$psi.FileName = $dotnet
$psi.Arguments = '"' + $dll + '" --root "' + $staging + '"'
$psi.WorkingDirectory = $staging
$psi.UseShellExecute = $false
$psi.RedirectStandardInput = $true
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true

$process = [System.Diagnostics.Process]::Start($psi)
$start = @{
    cmd = 'start'
    deck0 = 'starter.ydk'
    deck1 = $AiDeck
    auto = $true
    ai = $true
    aiSeat = 1
    aiName = $AiName
    aiDeck = $AiName
    aiDeckFile = $AiDeck
    aiDialog = 'cirno.zh-CN'
} | ConvertTo-Json -Compress
$process.StandardInput.WriteLine($start)
$process.StandardInput.Flush()

$watch = [System.Diagnostics.Stopwatch]::StartNew()
$lines = New-Object System.Collections.Generic.List[string]
while ($watch.Elapsed.TotalSeconds -lt $Seconds) {
    $task = $process.StandardOutput.ReadLineAsync()
    if ($task.Wait(3000)) {
        if ($null -eq $task.Result) { break }
        $lines.Add($task.Result)
    }
    if ($process.HasExited) { break }
}
if (-not $process.HasExited) { $process.Kill() }

$ai = ($lines | Where-Object { $_ -match '"type":"ai"' }).Count
$msgs = ($lines | Where-Object { $_ -match '"type":"message"' }).Count
$ended = $lines | Where-Object { $_ -match '"type":"ended"' } | Select-Object -Last 1
$win = $lines | Where-Object { $_ -match '"name":"Win"' } | Select-Object -Last 1
$errorLines = $lines | Where-Object { $_ -match 'AI feed failed|"type":"error"' } | Select-Object -First 3

Write-Output "lines=$($lines.Count) messages=$msgs aiDecisions=$ai"
Write-Output ("winner: " + ($(if ($win) { ($win | ConvertFrom-Json).winner } else { 'none' })))
Write-Output ("ended: " + ($(if ($ended) { $ended } else { 'no' })))
if ($errorLines) { Write-Output "errors:"; $errorLines | ForEach-Object { Write-Output "  $_" } }
