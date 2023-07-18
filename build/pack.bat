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
dotnet pack %srcDir%\Conqueror.Common.Middleware.Authentication -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.Common.Middleware.Authentication.Abstractions -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.Common.Middleware.Authentication.AspNetCore -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.Common.Middleware.Authorization.Abstractions -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.Common.Middleware.Authorization.AspNetCore -c Release -o %publishDir% --include-symbols
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
dotnet pack %srcDir%\Conqueror.Streaming.Interactive -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.Streaming.Interactive.Abstractions -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.Streaming.Interactive.Common -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.Streaming.Interactive.Transport.Http.Abstractions -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.Streaming.Interactive.Transport.Http.Common -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.Streaming.Interactive.Transport.Http.Client -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.Streaming.Interactive.Transport.Http.Server.AspNetCore -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.Streaming.Reactive -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.Streaming.Reactive.Abstractions -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror.Abstractions -c Release -o %publishDir% --include-symbols
dotnet pack %srcDir%\Conqueror -c Release -o %publishDir% --include-symbols

REM post-clean

echo cleaning build output directories...
for /D %%p in (%srcDir%\Conqueror*) Do (
  rmdir /S /Q %%p\bin\Release > nul 2> nul
)
