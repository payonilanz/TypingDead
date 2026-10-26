@echo off
setlocal
set "ROOT=%~dp0"
set "MSBUILD=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"
if not exist "%MSBUILD%" set "MSBUILD=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
if not exist "%MSBUILD%" (
    echo Could not find MSBuild for .NET Framework 4.x.
    echo Install the .NET Framework 4.8 Developer Pack or build with Visual Studio.
    exit /b 1
)

"%MSBUILD%" "%ROOT%TypingDead.csproj" /t:Rebuild /p:Configuration=Release /v:minimal
if errorlevel 1 exit /b 1

rem A GAC-fallback build can copy framework internals into the output folder.
rem They are not part of the game and can interfere with the clean root copy.
if exist "%ROOT%bin\Release\mscorlib.dll" del /Q "%ROOT%bin\Release\mscorlib.dll"
del /Q "%ROOT%bin\Release\norm*.nlp" >nul 2>&1

copy /Y "%ROOT%bin\Release\TypingDead.exe" "%ROOT%TypingDead.exe" >nul
if errorlevel 1 (
    echo Built Release, but could not replace the root TypingDead.exe.
    echo Close any running copy of the game and run build.bat again.
    exit /b 1
)
if exist "%ROOT%bin\Release\TypingDead.exe.config" (
    copy /Y "%ROOT%bin\Release\TypingDead.exe.config" "%ROOT%TypingDead.exe.config" >nul
)

echo.
echo Built Release: %ROOT%bin\Release\TypingDead.exe
echo Root copy:     %ROOT%TypingDead.exe
echo Run the self-test with: TypingDead.exe --selftest
endlocal
