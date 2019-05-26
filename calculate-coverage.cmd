pushd %~dp0
nuget install OpenCover -Version 4.7.922 -OutputDirectory OpenCover\tools
nuget install ReportGenerator -Version 4.1.4 -OutputDirectory OpenCover\tools
dotnet build -c Release
for /F "tokens=* USEBACKQ" %%D in (`where dotnet`) do (
set DotNetPath=%%D
)
OpenCover\tools\OpenCover.4.7.922\tools\OpenCover.Console.exe ^
    "-target:%DotNetPath%" ^
    "-targetArgs:test -c Release --no-build" ^
    "-filter:+[Cursively]* +[Cursively.*]* -[Cursively.Tests]*" ^
    -output:OpenCover\raw-coverage-results.xml ^
    -register:user ^
    -oldstyle

dotnet clean -c Release

dotnet OpenCover\tools\ReportGenerator.4.1.4\tools\netcoreapp2.1\ReportGenerator.dll ^
    -reports:OpenCover\raw-coverage-results.xml ^
    -targetdir:OpenCover\results

OpenCover\results\index.htm

popd
