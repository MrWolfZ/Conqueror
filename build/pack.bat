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
dotnet pack %srcDir%\Conqueror.Common.Middleware.Authentication.Abstractions -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.Common.Middleware.Authorization.Abstractions -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.Common.Transport.Http.Abstractions -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.Common.Transport.Http.Server.AspNetCore -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.CQS -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.CQS.Abstractions -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.CQS.Analyzers -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.CQS.Middleware.Authentication -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.CQS.Middleware.Authorization -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.CQS.Middleware.DataAnnotationValidation -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.CQS.Middleware.Logging -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.CQS.Middleware.Polly -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.CQS.Transport.Http.Abstractions -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.CQS.Transport.Http.Common -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.CQS.Transport.Http.Client -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.CQS.Transport.Http.Server.AspNetCore -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.Eventing -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.Eventing.Abstractions -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.Streaming -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.Streaming.Abstractions -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.Streaming.Transport.Http.Abstractions -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.Streaming.Transport.Http.Common -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.Streaming.Transport.Http.Client -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.Streaming.Transport.Http.Server.AspNetCore -c Release -o %publishDir% --include-symbols
dotnet publish %srcDir%\Conqueror.SourceGenerators -c Release --framework netstandard2.0 -o %publishDir%/Conqueror.SourceGenerators
dotnet pack %srcDir%\Conqueror.Abstractions -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror -c Release -o %publishDir% --include-symbols

REM post-clean

echo cleaning build output directories...
for /D %%p in (%srcDir%\Conqueror*) Do (
  rmdir /S /Q %%p\bin\Release > nul 2> nul
)
