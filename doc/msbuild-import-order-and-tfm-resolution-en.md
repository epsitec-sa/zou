---
author: Roger VUISTINER
co-author: Claude Sonnet 5 Medium
---

# MSBuild Import Order and TargetFramework Resolution

## Purpose

This document explains the exact MSBuild import order for SDK-style projects
(`<Project Sdk="Microsoft.NET.Sdk">`), why centralizing `TargetFramework(s)`
defaults in `Directory.Build.props` conflicts with per-project overrides, and
the pattern we use in this repository to resolve that conflict.

## Background: the problem

We centralize default target framework(s) in a shared property sheet
(`TargetFramework.props`, imported from the repo-root `Directory.Build.props`)
so that most projects don't need to repeat `net8.0;net48` everywhere.

The issue: `Directory.Build.props` is imported **before** the body of the
`.csproj` is evaluated. If a project defines `TargetFramework` (singular) in
its own body to force single-targeting, that value does not exist yet when
`Directory.Build.props` runs. A naive guard like:

```xml
<PropertyGroup Condition="'$(TargetFramework)' == '' and '$(TargetFrameworks)' == ''">
  <TargetFrameworks>net8.0;net48</TargetFrameworks>
</PropertyGroup>
```

will always see both properties as empty at that point, so the centralized
default always wins — even for a project that wants single-targeting. The
result is that `TargetFramework` (singular, project body) and
`TargetFrameworks` (plural, from `Directory.Build.props`) end up both set,
which triggers the SDK's outer/inner build split (`NETSDK1013`-class issues,
unexpected output paths, tasks behaving as if multi-targeted).

Moving the logic into a `.targets` file (imported at the bottom of the
evaluation) does not fix this either: by the time `Directory.Build.targets`
runs, `Sdk.props` has already made its TFM-dependent decisions (framework
imports, outer/inner build split). It's too late to influence that.

## The real import order

For `dotnet build PROJ.csproj` or `msbuild PROJ.csproj` on an SDK-style
project, MSBuild does **not** let the `.csproj` body or the CLI import
`Directory.Build.props` — it's `Sdk.props` that does it, as part of the
implicit SDK import chain:

```
1. Sdk.props                         (implicit import, injected by MSBuild
                                       because of Sdk="Microsoft.NET.Sdk")
   └── Directory.Build.props         (searched upward from the project's
                                       directory, imported if found —
                                       search STOPS at the first match)
   └── (rest of Sdk.props: TFM defaults, outer/inner build split decision,
        framework-specific imports)

2. PROJ.csproj body                  (explicit <PropertyGroup>/<ItemGroup>
                                       written in the project file)

3. Sdk.targets                       (implicit import, bottom of the file)
   └── Directory.Build.targets       (searched upward, same stop-at-first-
                                       match rule)
```

Key points:

- The implicit imports of `Sdk.props` (top) and `Sdk.targets` (bottom) are
  injected by MSBuild's XML parser because of the `Sdk="..."` attribute —
  before the rest of the file is even read.
- `Directory.Build.props` / `Directory.Build.targets` discovery is not done
  by MSBuild itself or by the CLI — it happens inside `Sdk.props` /
  `Sdk.targets` (via `Microsoft.Common.props` / `Microsoft.Common.targets`),
  controlled by `$(ImportDirectoryBuildProps)` / `$(ImportDirectoryBuildTargets)`
  (`true` by default).
- The upward search **stops at the first `Directory.Build.props` found**,
  starting from `$(MSBuildProjectDirectory)`. It does not keep climbing past
  that file automatically — chaining to a parent must be explicit.
- Because of this, the `.csproj` body (step 2) can never influence anything
  that `Sdk.props` (step 1) has already decided, including the TFM-dependent
  outer/inner build split.

## The pattern: per-folder `Directory.Build.props`

Since the project body is evaluated too late, but a `Directory.Build.props`
file is evaluated early enough (step 1), we push the override up to that
level instead. For a project (or group of projects) that needs to opt out of
the centralized default, add a `Directory.Build.props` **directly in that
project's folder** (or in a folder covering the whole group), and explicitly
chain to the parent:

```xml
<!-- MyProject/Directory.Build.props -->
<Project>
  <PropertyGroup>
    <TargetFrameworks>net8.0</TargetFrameworks>
  </PropertyGroup>

  <!-- Explicit chain to the repo-root Directory.Build.props, which still
       needs to run for everything else it configures (not just TFM). -->
  <Import Project="$(MSBuildThisFileDirectory)..\Directory.Build.props" />
</Project>
```

In the root `TargetFramework.props`, the guard condition then works
correctly, because by the time it runs, the local file has already set
`TargetFrameworks`:

```xml
<!-- Directory.Build.props (repo root) -->
<Import Project="TargetFramework.props" />
```

```xml
<!-- TargetFramework.props -->
<PropertyGroup Condition="'$(TargetFramework)' == '' and '$(TargetFrameworks)' == ''">
  <TargetFrameworks>net8.0;net48</TargetFrameworks>
</PropertyGroup>
```

Why this works and the `.csproj`-body approach doesn't: MSBuild only
auto-discovers **one** `Directory.Build.props` per project (the nearest one
going up). By placing our own at the project level and explicitly importing
the parent from inside it, we control the order ourselves — the local file's
`PropertyGroup` runs, then the parent's guard sees a non-empty
`TargetFrameworks` and backs off. Nothing in the `.csproj` body could ever
achieve this, because the body is evaluated strictly after `Sdk.props` (and
therefore after `Directory.Build.props`) has already run.

### When to use this pattern

- A **group of projects** sharing the same non-default TFM(s) (e.g. a
  cluster of net48-only legacy projects): put one `Directory.Build.props` in
  the folder that contains all of them.
- A **single project** needing single-targeting while the rest of the repo
  multi-targets: same pattern, scoped to that project's own folder.

### What to avoid

- Don't try to guard against `TargetFramework`/`TargetFrameworks` conflicts
  from inside the `.csproj` body or from a `.targets` file — both run too
  late to prevent the SDK's outer/inner build split from having already been
  decided in `Sdk.props`.
- Don't rely on `Directory.Build.props` auto-chaining upward on its own —
  the search stops at the first file found, so a local file that doesn't
  explicitly import its parent will silently skip all repo-root
  configuration (not just TFM defaults).

## Summary table

| Stage | What runs | Can it still influence TFM split? |
|---|---|---|
| `Sdk.props` implicit import | Injected by MSBuild parser | — |
| → `Directory.Build.props` (nearest, upward search) | Our TFM defaults / overrides live here | **Yes** — this is the only safe place |
| → rest of `Sdk.props` | TFM resolution, outer/inner build split decision | Too late after this point |
| `.csproj` body | Explicit `PropertyGroup`/`ItemGroup` | No |
| `Sdk.targets` implicit import + `Directory.Build.targets` | Post-build-graph logic | No |
