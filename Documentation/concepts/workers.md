---
title: Workers
description: The background-task pattern job steps run through - and a building block of its own.
---

# Workers

A **worker** is an Orleans grain that keeps a piece of work running in the background, safely off its turn: it starts, it survives the grain being deactivated in between, and its result is retrieved once it is done. The job step grains run through this pattern — and it is usable directly for any grain that needs the same shape.

## The contract

Derive `GrainWithBackgroundTask<TWork, TResult>`, implement `PerformWork()`, and call `StartWork()`/`GetResult()`:

```csharp
using Cratis.Orleans.Workers;

public interface IEternalWatch : IGrainWithGuidKey;

public class EternalWatch : GrainWithBackgroundTask<object, string>, IEternalWatch
{
    protected override async Task<PerformWorkResult<string>> PerformWork(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await DoAPass(cancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        }
        return "done"; // only reached when cancelled
    }
}
```

## What it guarantees

| Guarantee | Meaning |
| --- | --- |
| Off-turn execution | The work runs on the grain's context but never blocks the caller's turn - `StartWork` returns immediately |
| Cancellation | Deactivating the grain cancels the token; the worker sees it and exits cleanly |
| Result retrieval | `GetResult()` waits for completion and yields the result or the error, once |
| Bounded concurrency | An optional maximum concurrency level rejects starting more work than configured |

## Where the job system uses it

Every job step grain derives `JobStep<TRequest, TResult, TState>`, which derives a worker: the step's `PerformStep` runs as the background task, so a slow step never holds the job grain's bookkeeping turn, and stopping a job cancels the token the step observes. See [the job system](jobs.md).
