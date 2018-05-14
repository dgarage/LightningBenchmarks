#!/bin/bash

rm -rf "BenchmarkDotNet.Artifacts"
dotnet run -c Release -- "bench-alice-pays-bob-via-carol"

