# Lightning Benchmark

## Introduction

This repository is meant to host different reproducible scenaris of usage of the lightning protocol.
Those tests are easily reproducible via docker on any environment.

It currently benchmarks three scenaris:

* Alice pays Bob
* Alice pays Bob via Carol
* Alices pay Bob

It is testing 1, 5, 10, 15 payments at a time.

## Pre-requisite

Pre-requisite depends only on docker and .NET Core

* Docker
* Docker-Compose
* .NET Core SDK as specified by [Microsoft website](https://www.microsoft.com/net/download).

## How to run

Run the scripts `run-*` in `bench/Lightning.Bench`.

## Under the hood

Before each tests, a docker-compose file is generated based on the templates found on `bench/Lightning.Bench/docker-fragments`.

Then, the project is built and the benchmark run.

## License

This work is licensed under [MIT](LICENSE).