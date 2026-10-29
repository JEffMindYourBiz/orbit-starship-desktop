param([string]$OutFile='StarshipDesktop.exe')
$ErrorActionPreference='Stop'
$framework=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
Push-Location $PSScriptRoot
try {
 & (Join-Path $framework 'csc.exe') /nologo /target:winexe /platform:x64 "/out:$OutFile" /win32icon:Starship.ico /reference:System.Xaml.dll /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll "/reference:$framework/WPF/WindowsBase.dll" "/reference:$framework/WPF/PresentationCore.dll" "/reference:$framework/WPF/PresentationFramework.dll" AssemblyInfo.cs Starship.cs FlightProfile.cs MissionEffects.cs MissionController.cs Renderer.cs Validation.cs FrameDriver.cs ModelView.cs ContinuousFlight.cs Wreckage.cs CrewLift.cs BlastFrames.cs
 if($LASTEXITCODE -ne 0){throw 'Compilation failed'}
} finally {Pop-Location}
