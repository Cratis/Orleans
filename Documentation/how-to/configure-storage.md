---
title: Configure storage
description: Give the job system its database - MongoDB, SQL or in-memory.
---

# Configure storage

The job system persists through `IJobsStorage`, resolved per scope and namespace (see
[Storage scopes](../concepts/storage-scopes)). Pick a provider and register it - that single registration is
everything the engine needs.

## MongoDB

```csharp
using Cratis.Orleans.Setup;

builder.Services.AddCratisOrleansMongoDBJobsStorage(
    new MongoClient("mongodb://localhost:27017"));
```

Job state lands in database `{scope}+jobs` (default namespace) or `{scope}+jobs+{namespace}`. To match an
existing naming scheme - Chronicle, for instance, maps its event store and namespace onto the same database
names its own storage has always used - pass a resolver:

```csharp
builder.Services.AddCratisOrleansMongoDBJobsStorage(
    new MongoClient("mongodb://localhost:27017"),
    options => options.DatabaseNameResolver =
        (scope, @namespace) => $"{scope}+es+{@namespace}");
```

Indexes on job status and type are ensured on first use; concepts serialize as their underlying values and
Guids use the standard representation.

## SQL (EF Core)

The SQL provider is provider-agnostic: you decide, per scope and namespace, which EF Core provider and
connection serve the jobs - SQL Server, PostgreSQL or SQLite:

```csharp
using Cratis.Orleans.Setup;
using Cratis.Orleans.Storage.Sql;
using Microsoft.EntityFrameworkCore;

builder.Services.AddCratisOrleansSqlJobsStorage(options =>
    options.OptionsResolver = (scope, @namespace) =>
        new DbContextOptionsBuilder<JobsDbContext>()
            .UseSqlServer($"Server=.;Database={scope}_jobs;Trusted_Connection=True;TrustServerCertificate=true")
            .Options);
```

Job and job step state are stored as JSON columns (`StateJson`) alongside indexed status, type and name
columns. Create the schema before the first run - `Database.EnsureCreated()` on a `JobsDbContext` built from
your resolver is enough to get going; manage migrations yourself for production.

## In-memory

For specs and for hosts that do not need jobs to survive a restart:

```csharp
using Cratis.Orleans.Storage;

builder.Services.AddCratisOrleansInMemoryJobsStorage();
```

## What the engine does with the choice

The job and job step grains persist through the registered provider - resolved by their grain key's scope and
namespace - and every query (`GetJobs`, `ObserveJobs`, step counting) reads the same place. Two silos sharing
one storage see each other's jobs; a silo with its own storage does not.
