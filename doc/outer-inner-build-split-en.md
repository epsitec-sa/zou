---
author: Roger VUISTINER
co-author: Claude Sonnet 5 Medium
---

# The Outer/Inner Build Split

## Purpose

This document explains what the .NET SDK's outer/inner build split is, when
and how the decision to trigger it is made, and what each of the two builds
actually does.

## Why this mechanism exists

MSBuild, at its core, has no notion of "one project, multiple outputs".
Multi-targeting (`TargetFrameworks`, plural — compiling the same project for
`net8.0` **and** `net48`, for example) is entirely a construct of the .NET
SDK (`Microsoft.NET.Sdk`), not a native MSBuild feature.

The SDK's solution: **re-invoke the same `.csproj` file multiple times**,
once per TFM, via the recursive MSBuild task (`<MSBuild>` task), with each
invocation receiving `TargetFramework` (singular) as a global property on
its command line. One "umbrella" invocation orchestrates the whole thing:
that's the outer/inner build split.

## How the decision is made

The check happens very early in `Sdk.props`, before the body of the
`.csproj` is even read. The logic, simplified:

```
If TargetFramework (singular) is set and TargetFrameworks (plural) is empty
  → simple build, no split.

If TargetFrameworks (plural) is set and TargetFramework (singular) is empty
  → activates the outer/inner build split.

If both TargetFramework AND TargetFrameworks are non-empty
  → special case: interpreted as a signal that we're already inside an
    inner build (see below); the SDK does NOT trigger a new split — but
    if this was NOT actually supposed to be an inner build, the overlap
    produces inconsistent behavior (often NETSDK1013, or unexpected TFM
    resolution).
```

This is exactly why a conflict between a `TargetFramework` set in the
project body and a `TargetFrameworks` set by `Directory.Build.props` is
dangerous: the SDK sees both properties non-empty and interprets that as
"we're inside an inner build", which silently short-circuits the normal
resolution logic, producing side effects that are hard to diagnose (see the
*MSBuild Import Order and TargetFramework Resolution* document for the full
scenario).

The exact check lives in internal SDK files
(`Microsoft.NET.DefaultOutputPaths.props` / related files), in a form
roughly like:

```xml
<PropertyGroup Condition="'$(TargetFrameworks)' != '' and '$(TargetFramework)' == ''">
  <_IsOuterBuild>true</_IsOuterBuild>
  <ShouldDoBuildAsOuterBuild>true</ShouldDoBuildAsOuterBuild>
</PropertyGroup>
```

The exact internal property names vary across SDK versions, but the
principle stays the same: **the split is decided by a simple text-based
presence/absence check on these two properties**, evaluated at the very
start of `Sdk.props` — which is why the import order documented elsewhere
matters so much.

## What the outer build does

The outer build is the "original" MSBuild invocation — the one you launch
directly (`dotnet build PROJ.csproj`). As soon as it detects
`TargetFrameworks` without `TargetFramework`, it compiles **nothing**
itself. Its role:

1. Parse the `TargetFrameworks` list (e.g. `net8.0;net48` →
   `["net8.0", "net48"]`).
2. For each TFM, invoke a recursive MSBuild task that re-runs **the same
   `.csproj` file**, this time with the global property
   `TargetFramework=net8.0` (singular) injected explicitly on the child
   invocation's command line.
3. Aggregate the results from the child invocations — for example, for the
   `Pack` target, combine the artifacts from each TFM into a single
   multi-targeted `.nupkg`.
4. Expose aggregation or no-op targets for operations that only make sense
   once per project, not once per TFM (certain packaging steps, for
   instance).

The outer build produces **no DLL** and has no `bin/Debug/<TFM>/` output
folder of its own.

## What the inner build does

Each inner build is an independent, complete MSBuild invocation of the same
`.csproj`, with `TargetFramework` (singular) fixed on the command line by
the outer build. This is exactly the normal "single-targeting" code path:

1. `Sdk.props` is re-evaluated for this invocation — but this time
   `TargetFramework` is non-empty, so **no new split** is triggered
   (otherwise you'd get infinite recursion).
2. Resolution of framework references, NuGet packages compatible with that
   specific TFM, and conditional imports/items
   (`Condition="'$(TargetFramework)'=='net48'"`).
3. Actual compilation (`Csc`/`Vbc`/`Fsc`), producing the DLL/EXE in
   `bin/Debug/net8.0/` or `bin/Debug/net48/` respectively.
4. Returns its artifacts (output paths, references) to the outer build that
   invoked it, via the `<MSBuild>` task's return metadata.

## Why a wrong detection breaks the build

If, due to a property conflict, a project meant to be single-targeting ends
up with `TargetFrameworks` set (even with only one value in it), it
switches into outer/inner mode when it shouldn't:

- Output paths change shape (`bin/Debug/net8.0/` instead of `bin/Debug/`),
  breaking hardcoded references elsewhere in the solution or in post-build
  scripts.
- Tasks meant to run only once (file generation, copies, signing) behave
  differently depending on whether they run in the outer or inner context —
  this can duplicate work, or conversely produce nothing if the task is
  wired to only run in a "true" single-targeting context.
- MSBuild spawns extra evaluations/processes (one per TFM), slowing down the
  build even when only one TFM is actually relevant.

## Restore and the outer/inner split

`Restore` does not follow the same mechanism as `Build`, `Pack`, or
`Publish`. Those targets use the recursive `<MSBuild>` task to re-invoke the
`.csproj` once per TFM (the classic outer/inner split described above).
`Restore` instead relies on a *dependency graph spec* (a temporary `.dg`
file generated by `dotnet restore` / `msbuild -t:Restore`), built as
follows:

1. **A single "static" evaluation of the project runs first** (not a real
   build — just an MSBuild evaluation) to discover `TargetFrameworks`. This
   is where `RestoreUseStaticGraphEvaluation` comes in (enabled by default
   since SDK 5+), an optimized evaluation mode that reads properties/items
   without executing targets.
2. From that initial evaluation, MSBuild knows the project is
   multi-targeted. It then generates, **one evaluation per TFM**, of the
   same file with `TargetFramework=<tfm>` fixed — exactly like classic inner
   builds, except here the goal isn't to compile, it's to resolve the
   `PackageReference` list (versions, `Condition`s included) **specific to
   that TFM**.
3. These N evaluations (one per TFM) are assembled into a single dependency
   graph (the `.dg` file), passed to the NuGet resolution engine
   (`NuGet.Build.Tasks` / `RestoreTask`). NuGet then resolves package
   versions **per TFM**, handles conflicts, applies Central Package
   Management if active, etc.
4. The result is a **single `obj/project.assets.json`** per project (not one
   per TFM), but its internal content is structured by "target" — each TFM
   has its own section listing its resolved packages, reference assemblies,
   etc. `obj/<proj>.csproj.nuget.g.props` and `.g.targets` are generated
   accordingly.

### Practical implications

Restore reads `TargetFramework`/`TargetFrameworks` at exactly the same
critical point as a normal build's `Sdk.props`, so:

- If a `TargetFramework` (project body) vs `TargetFrameworks`
  (`Directory.Build.props`) conflict exists, it pollutes **restore too**,
  not just build. This can produce a `project.assets.json` with only one
  TFM resolved (or the wrong one), or an `NU1201`/`NU1202` error (package
  incompatible with the TFM) that looks unrelated to the build issue —
  while the root cause is the same faulty property detection upstream.
- The per-folder `Directory.Build.props` pattern (documented separately)
  fixes the problem **at the source**, before either restore or build sees
  the properties — which is why a single fix resolves both symptoms.

### A caveat: `RestoreUseStaticGraphEvaluation`

With this mode (default today), the initial evaluation that discovers
`TargetFrameworks` uses a lightweight MSBuild evaluator
(`Microsoft.Build.Evaluation` in static mode), without executing a real
build's targets. This means that if TFM-determination logic depends on a
custom `<Target Name="...">` rather than a top-level `PropertyGroup`,
**restore may never see that logic**, since targets don't execute in static
evaluation mode — only imports and "cold"-evaluated
`PropertyGroup`/`ItemGroup` count. This is one more reason to keep TFM logic
strictly declarative (in imported files, not inside a `Target`), consistent
with the pattern documented elsewhere.

### Summary

| Aspect | Build (outer/inner) | Restore |
|---|---|---|
| Mechanism | Recursive re-invocation via `<MSBuild>` task | Dependency graph spec (`.dg`) + separate per-TFM evaluations |
| Number of outputs | One per TFM (`bin/<tfm>/...`) | A single one (`project.assets.json`), internally structured by TFM |
| Sensitive to the same `TargetFramework`/`TargetFrameworks` conflict? | Yes | Yes, at the same early stage in `Sdk.props` |
| Sensitive to logic inside a custom `Target`? | Yes (targets execute) | Not reliable if `RestoreUseStaticGraphEvaluation` is active (default) |

## Key takeaways

- The outer/inner build split is a .NET SDK construct, not a native MSBuild
  feature.
- The decision is made by a simple property presence check
  (`TargetFramework` vs `TargetFrameworks`), evaluated at the very start of
  `Sdk.props` — before the `.csproj` body.
- The outer build orchestrates and aggregates; it never compiles directly.
- The inner build is a complete, independent rebuild of the same project
  file, once per TFM, with `TargetFramework` explicitly fixed.
- An accidental overlap of `TargetFramework` and `TargetFrameworks` (often
  caused by a poorly controlled import order) throws off this detection and
  produces hard-to-diagnose symptoms (unexpected output paths, `NETSDK1013`,
  duplicated or missing tasks).
