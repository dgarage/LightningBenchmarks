# Lightning Benchmark

## Introduction

This repository is meant to host different reproducible scenaris of usage of the lightning protocol.
Those tests are easily reproducible via docker on any environment. 

However one might want to run the different actors of the scenaris on different machines.

## Personas

### Alice

Alice is an actor which always send payments to Bob.

### Bob

Bob is an actor which always receive payments from Alice

### Carol

Carol is an actor which route payments from Alice to Bob

## Scenaris

### Scenario 1: Alice pays Bob (Direct payment)

### Scenario 2: Alice pays Bob through Carol (Multi hop payment)

### Scenario 3: Multiple Alice pays a single Bob (Payment Hub)

## License

This work is licensed under [MIT](LICENSE).