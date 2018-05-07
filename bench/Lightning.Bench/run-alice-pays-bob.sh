#!/bin/bash

rm -rf "BenchmarkDotNet.Artifacts"
docker-compose down --v --remove-orphans
dotnet build -c Release
dotnet run -c Release --no-build -- "generate-alice-pays-bob"
docker-compose up -d dev
dotnet run -c Release --no-build -- "bench-alice-pays-bob"
docker-compose down --v --remove-orphans
