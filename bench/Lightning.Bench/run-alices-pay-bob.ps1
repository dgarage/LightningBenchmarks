Remove-Item "BenchmarkDotNet.Artifacts" -Recurse -Force
dotnet run -c Release -- "bench-alices-pay-bob"
