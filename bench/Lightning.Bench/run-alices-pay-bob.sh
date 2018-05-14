#!/bin/bash

rm -rf "BenchmarkDotNet.Artifacts"
dotnet run -c Release -- "bench-alices-pay-bob"
