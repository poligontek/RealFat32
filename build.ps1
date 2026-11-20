param([switch]$Test)
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'Se necesita Windows x64 con .NET Framework 4.8.' }
$output = Join-Path $projectRoot 'dist'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$assetTool = Join-Path $output 'AssetExport.exe'
& $compiler /nologo /target:exe /platform:x64 "/out:$assetTool" /r:System.Drawing.dll (Join-Path $projectRoot 'src\Brand.cs') (Join-Path $projectRoot 'tools\AssetExport.cs')
if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación de los recursos gráficos.' }
& $assetTool (Join-Path $projectRoot 'assets')
if ($LASTEXITCODE -ne 0) { throw 'Falló la exportación de recursos.' }
$sources = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src') -Filter '*.cs' | ForEach-Object FullName)
$worker = Join-Path $output 'RealFat32.Updater.exe'
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /warn:4 /warnaserror+ "/out:$worker" /r:System.Windows.Forms.dll (Join-Path $projectRoot 'tools\UpdateWorker.cs')
if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación del actualizador.' }
$resources = @("/resource:$worker,RealFat32.Updater.exe", "/resource:$projectRoot\assets\RealFat32.ico,RealFat32.ico")
foreach ($language in @('es','en','pt')) { $resources += "/resource:$projectRoot\assets\CHANGELOG.$language.txt,CHANGELOG.$language.txt" }
$references = @('/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Management.dll','/r:System.Net.Http.dll','/r:System.Web.Extensions.dll')
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /warn:4 /warnaserror+ "/out:$output\RealFat32.exe" "/win32manifest:$projectRoot\app.manifest" "/win32icon:$projectRoot\assets\RealFat32.ico" $resources $references $sources
if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación de RealFat32.' }
Remove-Item -LiteralPath $assetTool
Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md') -Destination $output
Copy-Item -LiteralPath (Join-Path $projectRoot 'CHANGELOG.md') -Destination $output
Copy-Item -LiteralPath (Join-Path $projectRoot 'LICENSE') -Destination $output
$exeHash = (Get-FileHash -LiteralPath (Join-Path $output 'RealFat32.exe') -Algorithm SHA256).Hash.ToLowerInvariant()
[System.IO.File]::WriteAllText((Join-Path $output 'RealFat32.exe.sha256'), "$exeHash  RealFat32.exe`n", [System.Text.Encoding]::ASCII)
if ($Test) {
    $testExe = Join-Path $output 'RealFat32.Tests.exe'
    & $compiler /nologo /target:exe /platform:x64 /warn:4 /warnaserror+ "/out:$testExe" (Join-Path $projectRoot 'src\Fat32.cs') (Join-Path $projectRoot 'tests\Fat32Tests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación de las pruebas.' }
    & $testExe
    if ($LASTEXITCODE -ne 0) { throw 'Fallaron las pruebas.' }
    $uiTestExe = Join-Path $output 'RealFat32.UiTests.exe'
    & $compiler /nologo /target:exe /main:UiTests /platform:x64 /warn:4 /warnaserror+ "/out:$uiTestExe" $references $resources $sources (Join-Path $projectRoot 'tests\UiTests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación de las pruebas de interfaz.' }
    & $uiTestExe $projectRoot
    if ($LASTEXITCODE -ne 0) { throw 'Fallaron las pruebas de interfaz.' }
    $fixture = Join-Path $output 'UpdateFixture.exe'
    & $compiler /nologo /target:winexe /platform:x64 "/out:$fixture" (Join-Path $projectRoot 'tests\UpdateFixture.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación del ejecutable de prueba.' }
    $updateTestExe = Join-Path $output 'RealFat32.UpdateTests.exe'
    & $compiler /nologo /target:exe /main:UpdateTests /platform:x64 /warn:4 /warnaserror+ "/out:$updateTestExe" $references $resources $sources (Join-Path $projectRoot 'tests\UpdateTests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación de las pruebas de actualización.' }
    & $updateTestExe $projectRoot $fixture
    if ($LASTEXITCODE -ne 0) { throw 'Fallaron las pruebas de actualización.' }
}
Get-FileHash -LiteralPath (Join-Path $output 'RealFat32.exe') -Algorithm SHA256 | Format-List
Write-Host "Compilado: $output\RealFat32.exe"
