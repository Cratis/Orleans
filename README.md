# Cratis.Orleans

Shared Orleans building blocks for the Cratis platform: the Job system, Orleans serializers for
Cratis primitives, and the silo hosting and storage tooling that every Cratis service that
co-hosts an Orleans silo needs. Consolidated here from Chronicle, Direct and Studio, so the
three consume one canonical implementation instead of three diverging copies.

The repository deliberately has **no dependency on Chronicle**. Chronicle is a *consumer* of
these packages, together with Direct and Studio.

## Packages

| Package | What it holds |
| --- | --- |
| `Cratis.Orleans` | The Job system engine (job, job step and jobs manager grains), the `Workers` background-task pattern, typed grain keys, Orleans serializers for Cratis primitives (`ConceptAs`, OneOf, …), and the silo hosting tooling (clustering, grain storage registration, reminders guidance, package-alignment guard) |
| `Cratis.Orleans.Storage` | The storage API for jobs and job steps — `IJobStorage` / `IJobStepStorage`, job and job step state, errors — plus the scoped storage accessor the engine resolves storage through |
| `Cratis.Orleans.Storage.MongoDB` | MongoDB implementation of the jobs storage API |
| `Cratis.Orleans.Storage.Sql` | SQL (EF Core: SQL Server, PostgreSQL, SQLite) implementation of the jobs storage API |

## Building

```bash
dotnet build
```

Packages target `net8.0;net9.0;net10.0` when packed (`-p:IsPackaging=true`); the development
loop builds `net10.0` only.

## Release

Pull requests are labeled `major` / `minor` / `patch` / `no-release`; merging to `main` cuts and
publishes the version the label asks for, via [cratis/release-action](https://github.com/cratis/release-action).
