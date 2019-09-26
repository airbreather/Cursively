pushd %~dp0
dotnet run -c Release --project test\Cursively.Benchmark\Cursively.Benchmark.csproj --framework netcoreapp3.0
for %%D in (BenchmarkDotNet.Artifacts\results\*.html) do %%D
popd
