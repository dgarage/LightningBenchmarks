#!/bin/bash

docker-compose down --v
dotnet build -c Release
dotnet run -c Release --no-build -- "generate-alice-pays-bob"
docker-compose up -d dev
dotnet run -c Release --no-build -- "bench-alice-pays-bob"
docker-compose down --v
