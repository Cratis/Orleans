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

## Reminders stay in-memory - deliberately

`Orleans.Providers.MongoDB` has no Orleans 10 release; 9.5.0 is a 9.x binary. Only its cluster membership and
grain storage are safe on the 10.x runtime. Pointing the 10.x reminder service at its 9.x reminder table
hangs the first `RegisterOrUpdateReminder` and times out the calling grain - so `AddCratisOrleans` keeps
reminders on Orleans' own in-memory service in both clustering modes. The job system does not use reminders:
jobs rehydrate themselves from durable storage at startup, so nothing is lost.

## Disabling the silo

Pass `Enabled = false` (or wire it to configuration) and the call becomes a no-op - useful for spec hosts that
want the registrations without a silo.

## The alignment guard

Every project referencing `Cratis.Orleans` gets a Release-build check that fails the moment
`Microsoft.Orleans.Reminders` resolves to a different version than `Microsoft.Orleans.Server` - the exact
mismatch that ships silently in Debug and throws `MissingMethodException` in production. Keep the Orleans
package versions pinned together in `Directory.Packages.props`.
