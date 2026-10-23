---
applyTo: "Source/**"
paths:
  - "Source/**"
---

## Grain interfaces only grow within a major

Every product built on Cratis.Orleans (Chronicle first) runs a rolling upgrade by letting silos of two releases share
one cluster. Both releases then carry *this* package, at two different versions, and call each other's job, job-step,
worker and state-machine grains. Orleans identifies a grain method on the wire by a hash of its declaring interface,
its name and its **parameter types**, so a change that every caller still compiles against - an extra optional
parameter, a widened type - is a different method. The older silo cannot decode the call, and the newer one fails with
`TypeLoadException: Unable to resolve type alias ("inv", ..., "<hash>")`. That is how Chronicle 19.70 to 19.106 broke
(Cratis/Chronicle#5272).

The grain interfaces this rule covers are every interface deriving from `IGrainWith...Key` or `IGrainObserver`,
including the generic ones consumers derive from: `IJob`, `IJob<TRequest>`, `IJobStep`,
`IJobStep<TRequest, TResult, TState>`, `IJobsManager`, `IJobObserver`, `IGrainWithBackgroundTask`,
`IGrainWithBackgroundTask<TRequest, TResult>` and `IEventSourcedStateMachine`.

### When you change a grain interface, within a major

| You want to | Do this |
| --- | --- |
| Add a method | Add it, and put `[Version(n)]` on that interface, one higher than its current version (no attribute means 0, so the first one is `[Version(1)]`). |
| Add a parameter, or change a parameter or return type | Do **not** edit the method. Add a new overload beside it, raise `[Version]`, and have the old method forward to the new one. `IJobStep.Prepare(object)` and `Prepare(object, JobStepStage)` are this pattern. |
| Remove or rename a method | Not within a major. Keep it serving and remove it in the next major. |
| Rename or move an interface or a grain class | Not within a major. The full name is on the wire. |
| Change only a method's body or XML docs | Nothing; the wire identity is unchanged. |

`[Version]` routes a call that needs the new method to a silo that has it, instead of to an activation on an older
silo. Raise it only on the interface that gained a method, never on every interface at once: raising it everywhere
makes every call from a new silo seek a new silo and moves every grain during a rollout.

Write it with a `using`, not qualified in place, because inside a `Cratis.*` namespace `Orleans` resolves to
`Cratis.Orleans`:

```csharp
using Orleans.CodeGeneration;

namespace Cratis.Orleans.Jobs;

[Version(1)]
public interface IJob : IGrainWithGuidCompoundKey
{
    // existing methods unchanged

    Task<Result<JobError>> Pause();
}
```

### Types that travel between silos

- **Never move or rename a codec, or a type a codec writes the name of.** `ConceptSerializer`, `OneOfSerializer` and
  the other `Cratis.Orleans.Serialization` codecs write type names into every value. Moving `ConceptSerializer` out of
  Chronicle into this package broke Chronicle 19.4 to 19.13 (Cratis/Chronicle#4325).
- **Request, result, error and state types only gain members.** Renaming, retyping or removing a property, or changing a
  `[GenerateSerializer]` type's `[Id]` numbers, breaks a silo of the other release. Nothing checks this automatically.

### No gate covers this repository yet

Chronicle's **Grain Compatibility** workflow and Orleans contract analyzer cover only Chronicle's own grain interfaces;
the interfaces here are outside them. Until this repository has its own check, this rule and review are the only
protection, so:

- State every grain interface change in the pull request description, as an `## Added` bullet naming the interface and
  its new `[Version]`.
- A change this rule forbids within a major goes out only in a pull request labeled `major`.
