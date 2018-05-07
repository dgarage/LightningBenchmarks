Remove-Item "BenchmarkDotNet.Artifacts" -Recurse -Force
docker-compose down --v --remove-orphans
dotnet build -c Release
dotnet run -c Release --no-build -- "generate-alices-pay-bob"
docker-compose up -d dev
dotnet run -c Release --no-build -- "bench-alices-pay-bob"
docker-compose down --v --remove-orphans
