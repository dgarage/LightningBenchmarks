#!/bin/bash

docker-compose down --v
dotnet build -c Release
dotnet run -c Release --no-build -- "generate-alice-pays-bob-via-carol"
docker-compose up -d dev
dotnet run -c Release --no-build -- "bench-alice-pays-bob-via-carol"
docker-compose down --v
