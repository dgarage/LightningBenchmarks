Remove-Item "BenchmarkDotNet.Artifacts" -Recurse -Force
dotnet run -c Release -- "bench-alice-pays-bob-via-carol"