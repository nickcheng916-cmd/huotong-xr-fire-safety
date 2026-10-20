param([string]$OutputDirectory = (Join-Path $PSScriptRoot '../work/parser-tests'))
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$csc = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (-not (Test-Path -LiteralPath $csc)) { throw '.NET Framework C# compiler is required on Windows.' }
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$testExe = Join-Path (Resolve-Path -LiteralPath $OutputDirectory) 'BpmPacketParserTests.exe'
& $csc /nologo /target:exe "/out:$testExe" (Join-Path $repoRoot 'modules/posture-tracking-feedback/Assets/BpmPacketParser.cs') (Join-Path $PSScriptRoot 'BpmPacketParserTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Parser tests failed to compile.' }
& $testExe
if ($LASTEXITCODE -ne 0) { throw 'Parser tests failed.' }
