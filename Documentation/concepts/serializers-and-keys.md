---
title: Serializers and grain keys
description: The Orleans codecs for Cratis types, the JSON fallback, and typed compound grain keys.
---

# Serializers and grain keys

## Serializers

Orleans generates codecs for types it knows about. Cratis types — concepts (`ConceptAs<T>`), OneOf values, expando objects, LINQ iterator internals — need their own, and `AddCratisOrleans` registers them all through `AddCratisOrleansSerializers`:

| Serializer | Owns |
| --- | --- |
| `ConceptSerializer` | Every `ConceptAs<T>` crossing a grain boundary — job ids, names, statuses — serialized as the underlying primitive |
| `OneOfSerializer` | `OneOf<T0, T1, …>` values and `OneOf.Types.None` |
| `ExpandoObjectSerializer` | `ExpandoObject` payloads |
| `LinqCollectionCopier` | Deep-copying the internal iterator types LINQ produces (`Select`/`Where` iterators), which Orleans cannot copy itself |
| JSON fallback | Everything else Cratis-owned with no generated codec — carried by System.Text.Json with the concept converters and the job state converter |

The JSON fallback serializes with camelCase naming, concepts as their underlying values, and the polymorphic `JobState.Request` through `JobStateConverter`, which resolves the concrete request type through the job type registry — the same resolution the MongoDB provider performs in BSON.

A host that registered its own `JsonSerializerOptions` before calling the setup keeps it; only the converters the job state needs are ensured.

## Typed grain keys

Orleans compound keys are strings. `KeyHelper` combines parts with `#` and parses them back into your record — concepts included:

```csharp
public record JobKey(string Scope, string Namespace)
{
    public override string ToString() => KeyHelper.Combine(Scope, Namespace);
    public static JobKey Parse(string key) => KeyHelper.Parse<JobKey>(key);
}
```

`Parse` reflects over the record's constructor once and memoizes it — parsing the same key type repeatedly is a dictionary lookup. Concept-typed parameters are reconstructed through the concept factory; a missing trailing part becomes `null`, which is how the default (empty) namespace travels.

The job system's `JobKey`, `JobStepKey` and `JobsManagerKey` are built on exactly this.
