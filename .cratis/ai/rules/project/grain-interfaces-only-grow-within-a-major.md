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
including the generic ones consumers derive from. `Source/Orleans/OrleansContracts.txt` and
`Source/Orleans.Chronicle/OrleansContracts.txt` list them all, with each one's current version.

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

### How this is checked

Each `OrleansContracts.txt` records every grain interface in its project, its version, each method's wire identity
and every grain class. It is generated, never hand-edited. After changing a grain interface, regenerate it and review
the diff:

```bash
dotnet format Source/Orleans/Orleans.csproj analyzers --severity info --diagnostics ORLEANS0016 ORLEANS0017 ORLEANS0018 ORLEANS0019 ORLEANS0020 ORLEANS0022 ORLEANS0023 ORLEANS0024
```

Use `Source/Orleans.Chronicle/Orleans.Chronicle.csproj` for the state machine. A project that gains its first grain
interface enables the analyzer the same way and is added to `CONTRACT_PROJECTS` in `grain-compatibility.yml`.

- The Orleans contract analyzer fails the build when the source and the manifest disagree.
- The **Grain Compatibility** workflow, run before every build and every publish, fails when a manifest is not purely
  additive against the latest release. A pull request labeled `major` is exempt.
- Neither looks inside request, result, error or state types, so review those by hand.
- Name every grain interface change in the pull request description, as an `## Added` bullet naming the interface and
  its new `[Version]`, so products built on this package can see it in the release notes.
