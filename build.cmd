@ECHO OFF

dotnet workload update --skip-sign-check
dotnet workload install aspire --skip-sign-check

dotnet build .\build\Build.proj