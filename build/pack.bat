@echo off
SETLOCAL ENABLEDELAYEDEXPANSION
SET thisFileDir=%~dp0
SET srcDir=%thisFileDir%..\src
SET publishDir=%thisFileDir%.publish

REM pre-clean

echo cleaning publish directory...
rmdir /S /Q %publishDir% > nul 2> nul

echo cleaning build output directories...
for /D %%p in (%srcDir%\Conqueror*) Do (
  rmdir /S /Q %%p\bin\Release > nul 2> nul
)

REM pack

echo packing projects...

mkdir .publish
dotnet pack %srcDir%\Conqueror.Common -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.Common.Abstractions -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.CQS -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.CQS.Abstractions -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.CQS.Extensions.AspNetCore.Abstractions -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.CQS.Extensions.AspNetCore.Common -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.CQS.Extensions.AspNetCore.Client -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.CQS.Extensions.AspNetCore.Server -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.Eventing -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.Eventing.Abstractions -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.Abstractions -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror -c Release -o %publishDir% --include-symbols

REM post-clean

echo cleaning build output directories...
for /D %%p in (%srcDir%\Conqueror*) Do (
  rmdir /S /Q %%p\bin\Release > nul 2> nul
)
