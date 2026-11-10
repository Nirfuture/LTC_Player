[CmdletBinding()]
param(
 [Parameter(Mandatory=$true)]
 [ValidatePattern('^\d+\.\d+\.\d+(?:-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?$')]
 [string]$Version,
 [string]$Revision='HEAD',
 [string]$DotnetPath='dotnet',
 [switch]$RequireSigned
)
$ErrorActionPreference='Stop'
$repo=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$releaseRoot=[IO.Path]::GetFullPath((Join-Path $repo 'releases'))
$target=[IO.Path]::GetFullPath((Join-Path $releaseRoot $Version))
if((Split-Path $target -Parent) -ne $releaseRoot){throw 'Release path is outside releases/.'}
if(Test-Path -LiteralPath $target){throw "Release directory already exists: $target. Published versions must not be overwritten."}
if((Test-Path -LiteralPath $releaseRoot) -and ((Get-Item -LiteralPath $releaseRoot).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'releases/ must not be a symbolic link or junction.'}
$dotnetExe=(Get-Command $DotnetPath -CommandType Application -ErrorAction Stop).Source
Push-Location $repo
try {
 $commit=(& git rev-parse --verify "$Revision^{commit}").Trim()
 if($LASTEXITCODE -ne 0){throw 'Cannot resolve source revision.'}
 $packagingRoot=[IO.Path]::GetFullPath((Join-Path $repo 'artifacts/packaging'))
 $work=Join-Path $packagingRoot ($Version+'-'+[Guid]::NewGuid().ToString('N'))
 $source=Join-Path $work 'source'
 $stage=Join-Path $work 'release'
 $app=Join-Path $stage 'LtcPlayer'
 New-Item -ItemType Directory -Force $source,$app | Out-Null
 $sourceZip=Join-Path $stage "LtcPlayer-$Version-source.zip"
 & git archive --format=zip "--output=$sourceZip" $commit
 if($LASTEXITCODE -ne 0){throw 'Source archive failed.'}
 Expand-Archive -LiteralPath $sourceZip -DestinationPath $source
 [xml]$project=Get-Content -LiteralPath (Join-Path $source 'src/LtcPlayer.Avalonia/LtcPlayer.Avalonia.csproj') -Raw
 $sourceVersion=@($project.Project.PropertyGroup.Version | Where-Object {$_})[0]
 if($sourceVersion -ne $Version){throw "Source version $sourceVersion does not match requested release $Version. Commit the version change first."}
 $notes=Join-Path $source 'RELEASE_NOTES.md'
 if(-not ([IO.File]::ReadAllText($notes).StartsWith("# LTC Player $Version"))){throw 'RELEASE_NOTES.md version does not match the release.'}
 Write-Output "Building committed source $commit ($Version). Uncommitted working-tree files are not included."
 & (Join-Path $source 'scripts/build.ps1') -DotnetPath $dotnetExe
 if($LASTEXITCODE -ne 0){throw 'Product build failed.'}
 Copy-Item -Path (Join-Path $source 'artifacts/win-x64/*') -Destination $app -Recurse
 foreach($name in @('LICENSE','NOTICE.md','README.md','RELEASE_NOTES.md')){Copy-Item -LiteralPath (Join-Path $source $name) -Destination $app}
 foreach($name in @('LICENSES','docs')){Copy-Item -LiteralPath (Join-Path $source $name) -Destination $app -Recurse}
 $corresponding=Join-Path $app 'CorrespondingSource'
 New-Item -ItemType Directory -Force (Join-Path $corresponding 'native'),(Join-Path $corresponding 'third_party') | Out-Null
 Copy-Item -LiteralPath (Join-Path $source 'native/Audio.cpp'),(Join-Path $source 'native/build-phase2.cmd') -Destination (Join-Path $corresponding 'native')
 Copy-Item -LiteralPath (Join-Path $source 'third_party/libltc') -Destination (Join-Path $corresponding 'third_party') -Recurse
 $sourceNotice="# AudioNative.dll corresponding source`n`nSource commit: $commit`n`nSee ../NOTICE.md and ../LICENSES/LGPL-3.0.txt. Rebuild AudioNative.dll using native/build-phase2.cmd with MSVC x64 and the Windows SDK. The library may be replaced by an ABI-compatible modified build.`n"
 [IO.File]::WriteAllText((Join-Path $corresponding 'README.md'),$sourceNotice)
 foreach($required in @('LtcPlayer.Avalonia.exe','LtcPlayer.Avalonia.dll','AudioNative.dll','PlayerNative.dll','TestMedia/HAP_1_1080p_2997_10s.mov','LICENSES/LGPL-3.0.txt','CorrespondingSource/third_party/libltc/COPYING')){
  if(-not(Test-Path -LiteralPath (Join-Path $app $required) -PathType Leaf)){throw "Missing package file: $required"}
 }
 $signatures=Join-Path $stage 'SIGNATURES.json'
 & (Join-Path $source 'scripts/test-release-signatures.ps1') -Path $app -ReportPath $signatures -ReportOnly:(!$RequireSigned)
 $signatureData=Get-Content -LiteralPath $signatures -Raw | ConvertFrom-Json
 $invalid=@($signatureData | Where-Object {$_.Status -ne 'Valid'}).Count
 $manifest=[ordered]@{Version=$Version;Commit=$commit;Runtime='win-x64';CreatedUtc=[DateTime]::UtcNow.ToString('o');Signed=($invalid -eq 0);FilesWithoutValidSignature=$invalid;Verification='See docs/releases/'+$Version+'.md';ExternalPrerequisites=@('FFmpeg and ffprobe','Visual C++ v14 x64 runtime')}
 $manifestText=$manifest | ConvertTo-Json -Depth 4
 [IO.File]::WriteAllText((Join-Path $stage 'MANIFEST.json'),$manifestText)
 [IO.File]::WriteAllText((Join-Path $app 'MANIFEST.json'),$manifestText)
 Copy-Item -LiteralPath $notes -Destination (Join-Path $stage 'RELEASE_NOTES.md')
 $binaryZip=Join-Path $stage "LtcPlayer-$Version-win-x64.zip"
 Add-Type -AssemblyName System.IO.Compression.FileSystem
 [IO.Compression.ZipFile]::CreateFromDirectory($app,$binaryZip,[IO.Compression.CompressionLevel]::Optimal,$false)
 $hashes=@(Get-ChildItem -LiteralPath $stage -File | Sort-Object Name | ForEach-Object { ((Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant())+'  '+$_.Name })
 [IO.File]::WriteAllText((Join-Path $stage 'SHA256SUMS.txt'),(($hashes -join "`n")+"`n"))
 # Directory.Move fails if another process created this version meanwhile.
 # Both absolute paths are constrained to this repository before the move.
 if(-not ([IO.Path]::GetFullPath($stage).StartsWith($packagingRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase))){throw 'Staging path is outside artifacts/packaging/.'}
 if((Split-Path $target -Parent) -ne $releaseRoot){throw 'Unexpected final release path.'}
 New-Item -ItemType Directory -Force $releaseRoot | Out-Null
 [IO.Directory]::Move($stage,$target)
 Write-Output "Release prepared: $target"
} finally {Pop-Location}
