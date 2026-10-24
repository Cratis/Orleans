---
title: Host the silo
description: Co-host an Orleans silo with the job system in your application.
---

# Host the silo

You have a service that needs to run jobs. Co-host the silo next to your application and the job system comes
with it.

## Prerequisites

- The .NET 10 SDK
- A MongoDB server, if you use the MongoDB storage or clustering

## Host the silo

```csharp
using Cratis.Orleans.Hosting;

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

`AddCratisOrleans` brings the whole job system: the job, job step and jobs manager grains, their storage
providers, the Cratis serializers, and the silo configuration below. All options are listed in
[Options](../reference/options.md).

`AddCratisOrleans` already enables Orleans activity propagation, so trace context follows grain calls. You do
not need to call `AddActivityPropagation()` in your own `UseOrleans` delegate. If you keep an explicit call,
call it **before** `AddCratisOrleans`; Cratis then keeps the existing propagation filters without adding duplicates.
Do not call `AddActivityPropagation()` **after** `AddCratisOrleans`: Orleans 10.3.1 registers both propagation
filters again, producing duplicate spans. Tenant context stays in Orleans request context, not in activity baggage.

## Clustering

Locally the silo uses localhost clustering - one instance, no membership table. For running more than one
instance, switch to durable MongoDB membership:

```csharp
builder.AddCratisOrleans(new CratisOrleansOptions
{
    ClusterId = "my-service",
    ServiceId = "my-service",
    Clustering = ClusteringMode.MongoDB,
    DatabaseName = "my-service-orleans",
    ConnectionString = builder.Configuration["Cratis:MongoDB:Server"]!
});
```

## Place job controllers and step workers separately

By default Orleans places jobs and steps using resource-optimized placement. To dedicate silos to controlling
jobs or to performing steps, opt in on the silo builder:

```csharp
builder.UseOrleans(silo =>
{
    silo.AddJobPlacement<JobControllerDirector>();
    silo.AddJobStepPlacement<JobWorkerDirector>();
    silo.UseJobStepParallelismFromSiloMetadata();
});
```

`AddJobPlacement()` and `AddJobStepPlacement()` without a director type keep the cluster's default placement
for that kind of grain, and `AddJobPlacement<TDirector>()` and `AddJobStepPlacement<TDirector>()` replace it
with your own `IPlacementDirector`. Directors choose among the compatible silos; this one reads silo
metadata published with `UseSiloMetadata`:

```csharp
using Cratis.Orleans.Jobs.Placement;
using Orleans.Runtime.MembershipService.SiloMetadata;
using Orleans.Runtime.Placement;

public sealed class JobWorkerDirector(ISiloMetadataCache metadata) : IPlacementDirector
{
    public Task<SiloAddress> OnAddActivation(PlacementStrategy strategy, PlacementTarget target, IPlacementContext context)
    {
        var workers = context.GetCompatibleSilos(target)
            .Where(silo => metadata.GetSiloMetadata(silo).Metadata.TryGetValue("role", out var role) && role == "worker")
            .ToList();

        return workers.Count == 0
            ? throw new NoEligibleJobWorker(target.GrainIdentity, "no silo has the worker role")
            : Task.FromResult(workers[Random.Shared.Next(workers.Count)]);
    }
}
```

Enable the job or job-step placement strategy only once every silo in the cluster runs this version, and register it identically on every silo.

> [!WARNING]
> Opting in overrides any placement attribute on your job and step classes. Directors run on the activation
> path, so they must answer promptly and must not call grains or storage.

`UseJobStepParallelismFromSiloMetadata()` makes each silo read its step limit from the silo metadata key
`cratis.jobs.max-parallel-steps` (pass another key as the argument). A missing, unparseable or non-positive value
falls back to the `JobsOptions` limit. Call it after `AddCratisOrleans`, which registers the default throttle.

## Reminders stay in-memory - deliberately

`Orleans.Providers.MongoDB` has no Orleans 10 release; 9.5.0 is a 9.x binary. Only its cluster membership and
grain storage are safe on the 10.x runtime. Pointing the 10.x reminder service at its 9.x reminder table
hangs the first `RegisterOrUpdateReminder` and times out the calling grain - so `AddCratisOrleans` keeps
reminders on Orleans' own in-memory service in both clustering modes. The job system does not use reminders.
`AddCratisOrleans` requests recovery after the silo becomes Active and when cluster membership reports a dead silo.
Supply a singleton `IJobsManagerKeys` provider on every silo with all your storage scopes and namespaces: the default
knows only managers activated on that silo and warns at startup when no host provider is present. Follow the
[event-store × tenant-namespace example](../concepts/jobs.md#tell-recovery-which-namespaces-exist) for complete
recovery, including namespaces a surviving silo has never served.

If you configure Orleans directly instead, register the jobs collaborators and storage, then call
`siloBuilder.AddCratisOrleansJobsRecovery()` from `Cratis.Orleans.Hosting`. Interrupted steps may execute again;
automatic recovery preserves durable stops rather than treating them as interrupted work.

## Disabling the silo

Pass `Enabled = false` (or wire it to configuration) and the call becomes a no-op - useful for spec hosts that
want the registrations without a silo.

## The alignment guard

Every project referencing `Cratis.Orleans` gets a Release-build check that fails the moment
`Microsoft.Orleans.Reminders` resolves to a different version than `Microsoft.Orleans.Server` - the exact
mismatch that ships silently in Debug and throws `MissingMethodException` in production. Keep the Orleans
package versions pinned together in `Directory.Packages.props`.
