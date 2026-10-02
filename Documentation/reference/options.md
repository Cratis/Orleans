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
| `MaxConcurrentRehydration` | 8 | How many stored jobs are resumed at once when the jobs manager rehydrates (oldest first); values below 1 count as 1 |
| `MaxConcurrentCleanup` | 4 | How many dead jobs the cleanup deletes at once; values below 1 count as 1 |
| `MaxConcurrentStepStarts` | 16 | How many steps of one job are started at once when it starts or resumes; values below 1 count as 1 |

Rehydration and cleanup isolate each job, so one job that fails to resume or delete never stops the others.

`IJobsManager.Rehydrate()` waits for dead-job cleanup and loading the interrupted jobs, then returns before
all jobs resume. The manager drains that snapshot oldest first as bounded background work on its activation
scheduler. Overlapping calls share preparation and the drain instead of starting duplicate work.

A job stopped while waiting for a slot stays stopped. Before dispatch, the manager reloads the job and checks
that its status is still Running, PreparingJob, PreparingSteps, or StartingSteps. Storage has no status-only
read, so this costs one additional job read beyond discovery; the same fresh state is reused to resolve the
grain. An explicit `Resume()` can still resume a stopped job.

The manager keeps its activation alive while the drain runs. This prevents idle collection, not requested
deactivation, migration, memory-pressure shedding (`GrainCollectionOptions.EnableActivationSheddingOnMemoryPressure`),
or silo shutdown. These stop further dispatch; jobs not yet resumed remain in storage and resume on the next
`Rehydrate()` call or host startup. The drain is activation-owned, not a durable queue or a cluster-wide
concurrency limit. Already dispatched calls may finish after cancellation.

`GetEffectiveMaxParallelSteps()` returns `MaxParallelSteps ?? Math.Max(1, Environment.ProcessorCount - 1)`.

## MongoDBJobsStorageOptions

| Option | Default | Meaning |
| --- | --- | --- |
| `DatabaseNameResolver` | `null` | When set, decides the database name for a scope and namespace; when `null`, `DatabaseNames.ForJobs` applies (`{scope}+jobs`, or `{scope}+jobs+{namespace}`) |
| `TransientRetryCount` | 5 | How many times an operation is retried after a transient error (wait queue full, timeout, connection failure); zero disables retrying |
| `TransientRetryBaseDelay` | 200 milliseconds | Delay before the first retry; it doubles each retry and is randomized by up to 50% so callers do not retry in lockstep |

Non-transient errors such as a missing job or a serialization failure are never retried. When the retries run
out, the original exception surfaces and is reported as the storage error. Each retry is logged as a warning.

### MongoDB connection pool

The library does not create the `IMongoClient`; you pass it to `AddCratisOrleansMongoDBJobsStorage`, so the
pool settings are yours. These `MongoClientSettings` matter for the job system:

- Share one `IMongoClient` for the whole process. Each client owns its own connection pool.
- `MaxConnectionPoolSize` (driver default 100) caps concurrent operations; further operations wait in a queue.
- `MaxConnecting` (driver default 2) limits how fast a cold pool opens connections, so a burst at startup queues
  up. Raise it if startup shows `MongoWaitQueueFullException`.
- The driver fails with `MongoWaitQueueFullException` once its wait queue is full. The bounded rehydration and
  cleanup options above, together with the transient retry, keep a restart with many interrupted jobs from
  reaching that point.

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
