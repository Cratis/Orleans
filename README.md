<div align="center">
  <a href="https://cratis.io">
    <img src="full-logo.png" alt="Cratis Orleans" width="480">
  </a>

  <h3 align="center">Cratis Orleans</h3>

  <p align="center">
    The shared Orleans building blocks for the Cratis platform — a durable job system, a pluggable jobs storage API, and the silo hosting tooling every Cratis service that co-hosts Orleans needs.
    <br />
    <a href="https://www.cratis.io/orleans/"><strong>Explore the docs »</strong></a>
    <br />
    <br />
    <a href="#-getting-started">Getting Started</a>
    &nbsp;·&nbsp;
    <a href="https://github.com/cratis/Orleans/issues/new?labels=bug">Report a Bug</a>
    &nbsp;·&nbsp;
    <a href="https://github.com/cratis/Orleans/issues/new?labels=enhancement">Request a Feature</a>
    &nbsp;·&nbsp;
    <a href="https://discord.gg/kt4AMpV8WV">Join the Discord</a>
  </p>

  <p align="center">
    <a href="https://discord.gg/kt4AMpV8WV">
      <img src="https://img.shields.io/discord/1182595891576717413?label=Discord&logo=discord&color=7289da" alt="Discord">
    </a>
    <a href="http://nuget.org/packages/cratis.orleans">
      <img src="https://img.shields.io/nuget/v/Cratis.Orleans?logo=nuget" alt="NuGet">
    </a>
    <a href="https://github.com/Cratis/Orleans/actions/workflows/dotnet-build.yml">
      <img src="https://github.com/Cratis/Orleans/actions/workflows/dotnet-build.yml/badge.svg" alt=".NET Build">
    </a>
    <a href="https://github.com/Cratis/Orleans/actions/workflows/publish.yml">
      <img src="https://github.com/Cratis/Orleans/actions/workflows/publish.yml/badge.svg" alt="Publish">
    </a>
    <a href="./LICENSE">
      <img src="https://img.shields.io/badge/license-MIT-blue" alt="License">
    </a>
  </p>
</div>

---

Cratis Orleans holds the Orleans building blocks every Cratis service needs: a durable **job system** for work that outlives a request, a **storage API** the job state persists through — with MongoDB, SQL and in-memory providers — and the **silo hosting tooling** that co-hosts Orleans next to an application in one call.

It is consolidated here from [Chronicle](https://github.com/Cratis/Chronicle) (the job engine, the workers pattern, the serializers), [Direct](https://github.com/Cratis/Direct) (the hosting knowledge) and [Studio](https://github.com/Cratis/Studio) (the package-alignment guard) — so all three consume one canonical implementation instead of three diverging copies. The repository has **no dependency on Chronicle**; Chronicle is a consumer, like every other Cratis service.

> For core values and principles, read our [core values and principles](https://github.com/Cratis/.github/blob/main/profile/README.md).

## ✨ Key Features

### ⚙️ Job System

| | |
|---|---|
| **Durable Jobs** | Work that outlives a request — replay, migration, provisioning — persisted through the storage API and rehydrated when the silo restarts |
| **Steps as Grains** | Each step is its own Orleans grain, so slow or hanging steps never stall each other and run in parallel, bounded by a throttle |
| **Plan then Perform** | `PrepareSteps` says what will happen before any of it happens — watchable progress rather than a spinner |
| **At-least-once** | A step in flight when the host went down is performed again on resume; progress checkpoints are debounced and time-flushed |
| **Fail-open** | One failed step fails the job's completion status — never the application; the history says which step was which |
| **Lifecycle Control** | Stop, resume and delete through the jobs manager grain, or the client-facing commands consumers build on it |

### 🗄️ Pluggable Storage

| | |
|---|---|
| **Scope + Namespace Seam** | `IJobsStorage.GetFor(scope, namespace)` — opaque values every consumer maps its identity onto (Chronicle: event store and namespace; an application: tenant or service) |
| **MongoDB Provider** | Polymorphic request BSON serialization, indexes on status and type, conventions, configurable database naming |
| **SQL Provider** | EF Core — SQL Server, PostgreSQL, SQLite — with full provider and connection flexibility per scope |
| **In-memory Provider** | For spec hosts and services that do not need jobs to survive a restart |

### 🖥️ Silo Hosting

| | |
|---|---|
| **One Call** | `AddCratisOrleans` brings the whole job system: grains, storage providers, serializers, type discovery, throttle and options |
| **Clustering** | Localhost for development, durable MongoDB membership for running more than one instance |
| **Reminder Safety** | The 9.x MongoDB provider's reminder table hangs the 10.x reminder service — the hosting keeps reminders on the in-memory service and the knowledge lives here once |
| **Alignment Guard** | Every consumer fails its Release build the moment `Microsoft.Orleans.Reminders` drifts from `Microsoft.Orleans.Server` — shipped as `buildTransitive` |

### 🔧 Building Blocks

| | |
|---|---|
| **Workers** | The background-task pattern the job steps run through — also usable directly |
| **Typed Grain Keys** | `KeyHelper` combines and parses compound grain keys (`scope#namespace`) |
| **Cratis Serializers** | Orleans codecs for concepts (`ConceptAs<T>`), OneOf, expando objects and LINQ collections, plus a JSON fallback carrying the job state converter |
| **Public Logging Surface** | `BeginJobScope`/`BeginJobStepScope` and the `[LoggerMessage]` partials a consumer's own jobs read through |

## 📦 Packages

| Package | What it holds |
|---|---|
| [`Cratis.Orleans`](http://nuget.org/packages/cratis.orleans) | The job engine, Workers, typed grain keys, serializers, silo hosting |
| [`Cratis.Orleans.Storage`](http://nuget.org/packages/cratis.orleans.storage) | The jobs storage API, the scope resolver, the in-memory provider |
| [`Cratis.Orleans.Storage.MongoDB`](http://nuget.org/packages/cratis.orleans.storage.mongodb) | The MongoDB storage provider |
| [`Cratis.Orleans.Storage.Sql`](http://nuget.org/packages/cratis.orleans.storage.sql) | The SQL storage provider (EF Core; net10, matching EF Core 10) |

## 🚀 Getting Started

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and, for anything but the in-memory storage, a MongoDB server.

```bash
dotnet add package Cratis.Orleans
dotnet add package Cratis.Orleans.Storage.MongoDB
```

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

Define a job and its steps — grains, discovered automatically, nothing to register:

```csharp
public record ProvisionTenant(string TenantId, string[] Features) : IJobRequest;

public interface IProvisionTenant : IJob<ProvisionTenant>;

public class ProvisionTenantJob : Job<ProvisionTenant, JobState>, IProvisionTenant
{
    // Completed jobs remove themselves by default; keep this one queryable.
    protected override bool KeepAfterCompleted => true;

    protected override Task<IImmutableList<JobStepDetails>> PrepareSteps(ProvisionTenant request) =>
        Task.FromResult<IImmutableList<JobStepDetails>>(
            request.Features.Select(feature => new JobStepDetails(
                Type: typeof(IProvisionFeatureStep),
                Id: JobStepId.New(),
                Key: new JobStepKey(JobId, string.Empty, string.Empty),
                Request: feature,
                ResultType: typeof(string))).ToImmutableList());
}
```

Start it through the jobs manager — the two strings are the storage scope and namespace:

```csharp
var manager = grainFactory.GetJobsManager("my-service", string.Empty);
var result = await manager.Start<IProvisionTenant, ProvisionTenant>(new(id, ["billing"]));
```

Starting returns as soon as the job exists. Stop the service mid-flight and start it again — the job picks itself back up. That is the point.

Read the full walkthrough in the [getting-started guide](./Documentation/getting-started/index.md).

## 🧪 Testing

The repository carries three spec tiers:

- **Unit specs** (`Source/*/.Specs`) — 76+ Orleans TestKit specs covering the job grain lifecycle, the throttle, the checkpoint debouncer, the serializers and the storage scope resolution.
- **Integration specs** (`Integration/Jobs.Integration`) — end-to-end against a real co-hosted silo and a real MongoDB: start a job, run its steps as real grains, persist the state. CI runs a MongoDB service container so these gate every pull request.
- **Consumer suites** — Chronicle's kernel spec suite (3,496 specs) runs against the package engine through its rip-out integration.

## 🤝 Support

Cratis is an open community, and we are glad to help users, teams evaluating the stack, and contributors.

| Channel | Details |
|---|---|
| Discord | Join the community on [Discord](https://discord.gg/kt4AMpV8WV) for questions and discussions |
| GitHub Issues | [Report bugs or request features](https://github.com/Cratis/Orleans/issues) |
| Documentation | Read the docs at [cratis.io/orleans](https://www.cratis.io/orleans/) |

## 👷 Contributing

If you want to jump into building this repository and possibly contributing, refer to [contributing](https://github.com/Cratis/.github/blob/main/CONTRIBUTING.md).

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://www.docker.com/) — the integration specs and the MongoDB spec suites run against containers

### Build and test

```bash
dotnet build
dotnet test
```

The integration specs expect a MongoDB on `localhost:27017` (or a running Docker daemon).

## 📄 License

Cratis Orleans is free software distributed under the [MIT License](./LICENSE).
