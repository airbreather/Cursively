@echo off
REM ===========================================================================
REM Regenerates the https://airbreather.github.io/Cursively content locally
REM ===========================================================================
set DOCFX_PACKAGE_VERSION=2.45.1
pushd %~dp0
REM incremental / cached builds tweak things about the output, so let's do it
REM all fresh if we can help it...
rd /s /q src\Cursively\obj
rd /s /q doc\obj
dotnet restore
pushd tools
rd /s /q docfx.console.%DOCFX_PACKAGE_VERSION%
nuget install docfx.console -Version %DOCFX_PACKAGE_VERSION%
popd
%~dp0\tools\docfx.console.%DOCFX_PACKAGE_VERSION%\tools\docfx doc\docfx.json
popd
