#!/bin/bash

docker-compose down --v
dotnet build -c Release
dotnet run -c Release --no-build -- "generate-alices-pay-bob"
docker-compose up -d dev
dotnet run -c Release --no-build -- "bench-alices-pay-bob"