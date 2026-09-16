---
title: Options
description: Every option the job system and the silo hosting expose.
---

# Options

## CratisOrleansOptions

The silo co-hosting options, passed to `AddCratisOrleans`.

| Option | Default | Meaning |
| --- | --- | --- |
| `Enabled` | `true` | When `false`, `AddCratisOrleans` configures nothing |
| `Clustering` | `Localhost` | `Localhost` for single-instance development, `MongoDB` for durable cluster membership |
| `ClusterId` | `"default"` | The cluster id every instance joins |
| `ServiceId` | `"default"` | The service id every instance joins |
| `DatabaseName` | `"orleans"` | The MongoDB database holding cluster membership and grain storage |
| `ConnectionString` | `"mongodb://localhost:27017"` | The MongoDB connection string |

## JobsOptions

The job system behavior. Register through `services.Configure<JobsOptions>(...)` to override; the hosting
setup registers the defaults.

| Option | Default | Meaning |
| --- | --- | --- |
| `MaxParallelSteps` | processor count − 1, minimum 1 | How many steps of one job may run concurrently |
| `DeadJobThreshold` | 1 hour | How old a job stuck in preparation with no steps may be before cleanup removes it |
| `CleanupCadence` | 1 hour | How often the scavenger looks for dead jobs |
| `StepCheckpointBatchInterval` | 100 | How many reported batches a step accumulates before its progress checkpoint is persisted |
| `StepCheckpointFlushInterval` | 5 seconds | How long a step may hold an unpersisted checkpoint before it is flushed regardless; zero or less disables the timed flush |

`GetEffectiveMaxParallelSteps()` returns `MaxParallelSteps ?? Math.Max(1, Environment.ProcessorCount - 1)`.

## MongoDBJobsStorageOptions

| Option | Default | Meaning |
| --- | --- | --- |
| `DatabaseNameResolver` | `null` | When set, decides the database name for a scope and namespace; when `null`, `DatabaseNames.ForJobs` applies (`{scope}+jobs`, or `{scope}+jobs+{namespace}`) |

## SqlJobsStorageOptions

| Option | Meaning |
| --- | --- |
| `OptionsResolver` | Required. Returns the `DbContextOptions<JobsDbContext>` for a scope and namespace - the place you choose provider (SQL Server, PostgreSQL, SQLite) and connection |

## Well-known grain storage providers

The silo registers the job system's grain storage under these names; job grains reference them by name:

| Name | Used by |
| --- | --- |
| `jobs` | The job grain's persisted state |
| `job-steps` | The job step grain's persisted state |
