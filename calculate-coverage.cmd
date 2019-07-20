pushd %~dp0
nuget install OpenCover -Version 4.7.922 -OutputDirectory tools
nuget install ReportGenerator -Version 4.1.4 -OutputDirectory tools
dotnet build -c Release
for /F "tokens=* USEBACKQ" %%D in (`where dotnet`) do (
set DotNetPath=%%D
)
tools\OpenCover.4.7.922\tools\OpenCover.Console.exe ^
    "-target:%DotNetPath%" ^
    "-targetArgs:test -c Release --no-build" ^
    "-filter:+[Cursively]* +[Cursively.*]* -[Cursively.Tests]*" ^
    -excludebyattribute:System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute ^
    -output:tools\raw-coverage-results.xml ^
    -register:user ^
    -oldstyle

dotnet clean -c Release

dotnet tools\ReportGenerator.4.1.4\tools\netcoreapp2.1\ReportGenerator.dll ^
    -reports:tools\raw-coverage-results.xml ^
    -targetdir:tools\results

tools\results\index.htm

popd
