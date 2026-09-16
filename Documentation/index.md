---
title: Cratis.Orleans
description: The shared Orleans building blocks for the Cratis platform - the job system, storage API and silo hosting tooling.
---

# Cratis.Orleans

Cratis.Orleans holds the Orleans building blocks every Cratis service needs: a durable **job system** for work
that outlives a request, a **storage API** the job state persists through with MongoDB and SQL providers, and the
**silo hosting tooling** that co-hosts Orleans next to your application.

Without it, every Cratis service that co-hosts an Orleans silo - Chronicle's kernel, Direct, Studio - carries
its own copy of the job engine, its own serializers, and its own hard-won knowledge about which parts of the
Orleans package graph can be mixed. With it, you add one package and get the same engine, hardened by all
three.

## Start here

- [Getting started](getting-started/index.md) — run your first job, end to end
- [The job system](concepts/jobs.md) — what a job is and how it behaves
- [Storage scopes](concepts/storage-scopes.md) — how job state finds its database
- [Host the silo](how-to/host-the-silo.md)
- [Configure storage](how-to/configure-storage.md)
- [Options reference](reference/options.md)

## Packages

| Package | Holds |
| --- | --- |
| `Cratis.Orleans` | The job engine, Workers, typed grain keys, Orleans serializers, silo hosting |
| `Cratis.Orleans.Storage` | The jobs storage API and the scope resolver |
| `Cratis.Orleans.Storage.MongoDB` | MongoDB storage provider |
| `Cratis.Orleans.Storage.Sql` | SQL storage provider (EF Core: SQL Server, PostgreSQL, SQLite) |

The packages have no dependency on Chronicle; Chronicle is a consumer, like every other Cratis service.
