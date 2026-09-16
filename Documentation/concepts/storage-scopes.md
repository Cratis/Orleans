---
title: Storage scopes
description: How job state finds its database - the scope and namespace seam.
---

# Storage scopes

The engine never decides where job state lives. Every storage touch - reading a job, saving a step, counting
progress - goes through one seam:

```csharp
JobsStorage GetFor(string scope, string @namespace);
```

The two values are **opaque to the engine**. A storage provider decides what the pair resolves to:

- **MongoDB** (default): database `{scope}+jobs` for the default namespace, `{scope}+jobs+{namespace}`
  otherwise - overridable through `MongoDBJobsStorageOptions.DatabaseNameResolver`.
- **SQL** (EF Core): whatever `DbContextOptions` your resolver returns for the pair - you pick the provider
  (SQL Server, PostgreSQL, SQLite) and the connection per scope and namespace.

## What the pair means is yours

The scope is the first part of every job grain's key; the namespace is the second:

| Consumer | Scope | Namespace |
| --- | --- | --- |
| Chronicle's kernel | event store name | event store namespace (the tenant, when Arc resolves it) |
| An application | the service, the tenant, or anything storage should hang off | a subdivision of it, or empty |

Because the pair travels inside the grain key, it survives everything the grain survives - a job activated on
another silo lands in the same database as the one that started it.

## One accessor, every provider

`IJobsStorage` is the single registration the engine needs. `AddCratisOrleansMongoDBJobsStorage` (or the SQL
or in-memory equivalents) registers it, and the silo hosting picks it up for the grain storage providers.
Chronicle's own storage tree exposes the same interface, so its kernel and the job system resolve identical
storage for the same event store and namespace.
