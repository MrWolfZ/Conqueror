@echo off
SET thisFileDir=%~dp0
SET publishDir=%thisFileDir%.publish
set /p NUGET_ORG_API_KEY="Enter nuget.org API key: "
for %%b in (%publishDir%\*.symbols.nupkg) do (
  echo publishing package %%b...
  dotnet nuget push %%b -s https://api.nuget.org/v3/index.json -k %NUGET_ORG_API_KEY%
)
