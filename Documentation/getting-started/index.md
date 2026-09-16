---
title: Run your first job
description: Add Cratis.Orleans to a service, define a job, and watch it survive a restart.
---

# Run your first job

By the end of this page you will have a service that co-hosts an Orleans silo, runs a job with steps in it,
and picks the job back up after the process restarts.

You need the .NET 10 SDK and a MongoDB server running on `mongodb://localhost:27017`.

## 1. Reference the packages

Create a minimal ASP.NET Core service and add the packages:

```bash
dotnet add package Cratis.Orleans
dotnet add package Cratis.Orleans.Storage.MongoDB
```

## 2. Host the silo and its storage

In `Program.cs`, co-host the silo and give the job system its MongoDB storage:

```csharp
using Cratis.Orleans.Hosting;
using Cratis.Orleans.Setup;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.AddCratisOrleans(new CratisOrleansOptions
{
    ClusterId = "my-service",
    ServiceId = "my-service"
});

builder.Services.AddCratisOrleansMongoDBJobsStorage(
    new MongoClient("mongodb://localhost:27017"));

var app = builder.Build();
app.Run();
```

`AddCratisOrleans` configures localhost clustering by default and keeps reminders on the in-memory 10.x
reminder service - the safe arrangement for the current Orleans package graph. When you run more than one
instance, switch to `ClusteringMode.MongoDB` for durable membership; see
[Host the silo](../how-to/host-the-silo.md).

## 3. Define a step and a job

A job's work happens in **step grains** - each step is its own Orleans grain, so slow steps run in parallel
and a hanging step cannot stall the job's bookkeeping. Define a step for provisioning one feature:

```csharp
using Cratis.Monads;
using Cratis.Orleans.Jobs;

public record FeatureStepState : JobStepState;

public interface IFeatureStep : IJobStep<string, JobStepResult, FeatureStepState>;

public class FeatureStep : JobStep<string, JobStepResult, FeatureStepState>, IFeatureStep
{
    protected override Task<Result<PrepareJobStepError>> PrepareStep(string request) =>
        Task.FromResult(Result<PrepareJobStepError>.Success());

    protected override ValueTask InitializeState(string request) =>
        ValueTask.CompletedTask;

    protected override async Task<Catch<JobStepResult>> PerformStep(FeatureStepState currentState, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken); // the real work
        return Catch<JobStepResult>.Success(JobStepResult.None);
    }

    protected override ValueTask<JobStepResult?> CreateCancelledResultFromCurrentState(FeatureStepState currentState) =>
        ValueTask.FromResult<JobStepResult?>(JobStepResult.None);
}
```

Then the job itself: a grain class deriving `Job<TRequest, TJobState>`, paired with an interface deriving
`IJob<TRequest>`. Planning is separate from performing - `PrepareSteps` says what will happen before any of
it happens, which is what lets somebody watch progress rather than a spinner:

```csharp
using System.Collections.Immutable;
using Cratis.Orleans.Jobs;

public record ProvisionTenant(string TenantId, string[] Features) : IJobRequest;

public interface IProvisionTenant : IJob<ProvisionTenant>;

public class ProvisionTenantJob : Job<ProvisionTenant, JobState>, IProvisionTenant
{
    // Completed jobs remove themselves by default; keep this one queryable after it is done.
    protected override bool KeepAfterCompleted => true;

    protected override Task<IImmutableList<JobStepDetails>> PrepareSteps(ProvisionTenant request) =>
        Task.FromResult<IImmutableList<JobStepDetails>>(
            request.Features.Select(feature => new JobStepDetails(
                Type: typeof(IFeatureStep),
                Id: JobStepId.New(),
                Key: new JobStepKey(JobId, string.Empty, string.Empty),
                Request: feature,
                ResultType: typeof(JobStepResult))).ToImmutableList());
}
```

Nothing to register - job types and step types are discovered automatically.

## 4. Start it

Start the job through the jobs manager. The two strings are your storage scope and namespace - opaque values
the storage uses to find the right database (see [Storage scopes](../concepts/storage-scopes.md)):

```csharp
app.MapGet("/provision/{tenantId}", async (string tenantId, IGrainFactory grainFactory) =>
{
    var manager = grainFactory.GetJobsManager("my-service", string.Empty);
    var result = await manager.Start<IProvisionTenant, ProvisionTenant>(
        new ProvisionTenant(tenantId, ["billing", "reporting"]));

    return result.Match(
        jobId => Results.Accepted($"/jobs/{jobId}"),
        _ => Results.Problem("The job could not be started"));
});
```

Call the endpoint:

```bash
curl -i http://localhost:5000/provision/42
```

The response is `202 Accepted` with a job id - starting a job returns as soon as the job exists, not when it
is done. The steps run as separate grain activations while the request has long returned.

## 5. Watch it survive a restart

Stop the service with the job mid-flight and start it again. On activation, the jobs manager rehydrates every
job that had not finished - each step continues from its persisted state. That is the point of the job system:
work that outlives a process.

## Where to go next

- [The job system](../concepts/jobs.md) explains the lifecycle you just exercised.
- [Configure storage](../how-to/configure-storage.md) shows the SQL provider and custom database naming.
- The `Integration/` folder in the repository contains end-to-end specs doing exactly what you just did,
  against a real MongoDB.
