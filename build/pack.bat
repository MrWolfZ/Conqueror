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
dotnet pack %srcDir%\Conqueror.SourceGenerators.Util -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.SourceGenerators.TestUtil -c Release -o %publishDir% --include-symbols
dotnet publish %srcDir%\Conqueror.SourceGenerators -c Release --framework netstandard2.0 -o %publishDir%/Conqueror.SourceGenerators
dotnet pack %srcDir%\Conqueror.Abstractions -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror -c Release -o %publishDir% --include-symbols

REM middlewares
dotnet pack %srcDir%\Conqueror.Middleware.Authorization -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.Middleware.Logging -c Release -o %publishDir% --include-symbols

REM transports
dotnet publish %srcDir%\Conqueror.Transport.Http.SourceGenerators -c Release --framework netstandard2.0 -o %publishDir%/Conqueror.Transport.Http.SourceGenerators
dotnet pack %srcDir%\Conqueror.Transport.Http.Abstractions -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.Transport.Http.Client -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.Transport.Http.Server.AspNetCore -c Release -o %publishDir% --include-symbols

REM post-clean

echo cleaning build output directories...
for /D %%p in (%srcDir%\Conqueror*) Do (
  rmdir /S /Q %%p\bin\Release > nul 2> nul
)
