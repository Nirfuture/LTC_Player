$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
Push-Location $repo
try {
 & dotnet run --project tests/SyncRecovery/SyncRecovery.csproj -c Release
 if($LASTEXITCODE -ne 0){throw 'Sync recovery regression failed'}
 & ./scripts/build.ps1 -Verification
 $result=Join-Path $repo ('artifacts/tests-'+(Get-Date -Format yyyyMMdd-HHmmss))
 $fixtures=Join-Path $result fixtures
 New-Item -ItemType Directory $fixtures -Force | Out-Null
 & ffmpeg -hide_banner -loglevel error -f lavfi -i 'testsrc2=size=1920x1080:rate=25' -f lavfi -i 'sine=frequency=440:sample_rate=48000' -t 6 -c:v libx264 -preset ultrafast -pix_fmt yuv420p -c:a aac "$fixtures/h264-aac.mp4"
 if($LASTEXITCODE -ne 0){throw 'H264 fixture failed'}
 & ffmpeg -hide_banner -loglevel error -f lavfi -i 'testsrc2=size=640x360:rate=25' -f lavfi -i 'sine=frequency=660:sample_rate=48000' -t 4 -c:v hap -format hap -c:a pcm_s16le "$fixtures/hap-pcm.mov"
 if($LASTEXITCODE -ne 0){throw 'HAP fixture failed'}
 & ffmpeg -hide_banner -loglevel error -f lavfi -i 'testsrc2=size=640x360:rate=25' -t 1 -c:v hap -format hap_q "$fixtures/hap-q-silent.mov"
 if($LASTEXITCODE -ne 0){throw 'HAP Q fixture failed'}
 & ffmpeg -hide_banner -loglevel error -i "$fixtures/h264-aac.mp4" -c:v copy -c:a libmp3lame "$fixtures/h264-mp3.mp4"
 if($LASTEXITCODE -ne 0){throw 'MP3 fixture failed'}
 & ffmpeg -hide_banner -loglevel error -stream_loop 2 -i "$repo/TestMedia/HAP_1_1080p_2997_10s.mov" -c copy "$fixtures/hap-sync-30s.mov"
 if($LASTEXITCODE -ne 0){throw 'Sync fixture failed'}
 foreach($name in 'responsive','output-mode','resume-latency','portrait-monitor','release-features','sync-recovery-output','default-show','delivery-operations','phase2-adapter','ui-update','phase5','preview-recovery','monitor','preview-performance') {
  if($name -eq 'delivery-operations'){ '{"Rate":"29.97 DF","OutputDisplayName":"MISSING-VERIFICATION-DISPLAY"}' | Set-Content "$result/$name-settings.json" }
  if($name -eq 'phase2-adapter'){New-Item -ItemType Directory "$result/$name" -Force | Out-Null;Copy-Item "$fixtures/h264-aac.mp4" "$result/$name/media.mp4"}
  $media=if($name -eq 'sync-recovery-output'){"$fixtures/hap-sync-30s.mov"}else{"$repo/TestMedia/HAP_1_1080p_2997_10s.mov"}
  & ./artifacts/verification/LtcPlayer.Avalonia.exe ("--verify-"+$name) "$result/$name" --settings "$result/$name-settings.json" --fixtures $fixtures --media $media --probe-output | Out-Null
  if($LASTEXITCODE -ne 0){throw "Test failed: $name"}
 }
 Write-Output $result
} finally {Pop-Location}
