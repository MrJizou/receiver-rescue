@echo off
setlocal
cd /d "%~dp0"
set "RESCUE_CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%RESCUE_CSC%" set "RESCUE_CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%RESCUE_CSC%" (
  echo .NET Framework compiler was not found. Windows with .NET Framework 4.8 is required.
  exit /b 1
)
if not exist bin mkdir bin
"%RESCUE_CSC%" /nologo /target:winexe /out:bin\ReceiverRescue.exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Net.Http.dll /reference:System.Web.Extensions.dll /reference:System.Management.dll src\VerifiedPatcher.cs src\DeviceDiscovery.cs src\Support.cs src\RecoveryForm.cs src\Program.cs
if errorlevel 1 exit /b 1
if not exist bin\manifests mkdir bin\manifests
copy /y manifests\*.json bin\manifests\ >nul
echo Built bin\ReceiverRescue.exe
exit /b 0
