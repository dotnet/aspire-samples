@ECHO OFF

REM Check if dotnet CLI is installed
WHERE dotnet >nul 2>nul
IF ERRORLEVEL 1 (
    ECHO Error: dotnet CLI is not found. Please install the .NET SDK from https://dotnet.microsoft.com/download
    EXIT /B 1
)

REM Check if Build.proj exists before attempting to build
IF NOT EXIST "%~dp0Build.proj" (
    ECHO Error: Build.proj file not found at %~dp0Build.proj
    EXIT /B 1
)

dotnet build "%~dp0Build.proj" %*
EXIT /B %ERRORLEVEL%
