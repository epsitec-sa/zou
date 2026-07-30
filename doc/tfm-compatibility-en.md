---
author: Roger VUISTINER
co-author: Claude Sonnet 5 Medium
---

# Target Framework Moniker (TFM) Compatibility

## Purpose

This document describes the compatibility rules between the different
Target Framework Monikers (TFMs) used in the repository (`net48`,
`netstandard2.x`, `net8.0[-windows]`, `net10.0[-windows]`), and how these
rules apply to `ProjectReference` and `PackageReference`.

## General principle

TFM compatibility does **not** follow a simple "newer > older" relation —
it follows NuGet's framework precedence rules, which are **one-directional**:
a project targeting a "narrower" TFM (fewer APIs) can be consumed by a
project targeting a "wider" TFM (which has access to those APIs plus more),
but not the other way around.

Key points:

- **`netstandard2.x`** is not a runtime, it's an API contract. `net8.0` and
  `net10.0` **implement** `netstandard2.1` and all earlier versions. A
  `net8.0` project can therefore reference a `netstandard2.0`/
  `netstandard2.1` project, but not the reverse: a `netstandard2.0` project
  has no knowledge of `net8.0` APIs.
- **OS-specific TFMs** (`net8.0-windows`, `net8.0-ios`, `net8.0-android`,
  `net10.0-windows`, ...) inherit everything their base TFM offers
  (`net8.0`, `net10.0`), plus platform-specific APIs. `net8.0-windows` can
  therefore reference a `net8.0` (or `netstandard2.x`) project, but a
  `net8.0` (base, cross-platform) project cannot reference a
  `net8.0-windows` project, since it has no guarantee by definition of
  running on Windows.
- `netstandard2.1` is **not** implemented by .NET Framework (`net48`
  included) — only `netstandard2.0` is. This is a frequent source of
  `NU1201` errors in mixed legacy/modern solutions.
- There is no "official" `net8.0-browser`/`net10.0-browser` TFM in the
  standard .NET SDK, unlike `-windows`/`-ios`/`-android`. Blazor
  WebAssembly simply targets `net8.0`/`net10.0` (base); browser targeting
  goes through the `wasm-tools` workload, not through a dedicated
  OS-specific TFM. (Some third-party frameworks, such as Uno Platform,
  define their own custom TFMs like `net8.0-browserwasm`, but this is not
  an SDK standard.)

## Compatibility table (`ProjectReference` / `PackageReference`)

Rows = TFM of the **referencing** project, columns = TFM of the
**referenced** project. ✅ = reference allowed, ❌ = incompatible.

| Referencing \ Referenced | net48 | netstandard2.0 | netstandard2.1 | net8.0 | net8.0-windows | net10.0 | net10.0-windows |
|---|---|---|---|---|---|---|---|
| **net48** | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| **netstandard2.0** | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| **netstandard2.1** | ❌ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| **net8.0** | ❌ | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| **net8.0-windows** | ❌ | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| **net10.0** | ❌ | ✅ | ✅ | ✅¹ | ❌ | ✅ | ❌ |
| **net10.0-windows** | ❌ | ✅ | ✅ | ✅¹ | ✅¹ | ✅ | ✅ |

¹ A `net10.0(-windows)` project can reference a `net8.0(-windows)` project
via a **package reference** (NuGet) — the "upward" compatibility between
`net5.0+` versions works through standard NuGet precedence. For a direct
**`ProjectReference`** (source-to-source compilation within the same
solution), MSBuild is actually more permissive than the strict NuGet matrix
and also allows this direction (`net10.0` referencing a `net8.0` `.csproj`),
treating the referenced project somewhat like a compatible binary
dependency. This is nonetheless not a recommended pattern: in practice,
projects within a solution should be aligned on the same, highest "modern"
TFM rather than mixing `net8.0` and `net10.0`.

## Implications for a multi-targeted project

A project that multi-targets (e.g. `TargetFrameworks=net8.0;net48`) needs,
**for every TFM it targets**, a compatibility path to each
`ProjectReference`. This check is performed separately per inner build (so
once per TFM — see the *Outer/Inner Build Split* document):

- If `ProjectA` targets `net8.0;net48` and references `ProjectB`, which
  only targets `netstandard2.0`, this works for both of `ProjectA`'s TFMs
  (`netstandard2.0` is compatible with both `net48` and `net8.0`).
- If `ProjectB` targets `net8.0` only (no `netstandard2.x`, no `net48`),
  then `ProjectA`'s restore fails **on the `net48` inner build**, with an
  error such as "project X is not compatible with net48
  (.NETFramework,Version=v4.8)", even though the `net8.0` inner build
  compiles fine.

This is a very common situation in a mixed legacy/modern solution: any
shared library referenced by `net48` projects must stay on
`netstandard2.0`, or multi-target itself
(`TargetFrameworks=netstandard2.0;net8.0`), to remain consumable from both
sides.

## Key takeaways

- Compatibility is asymmetric: a "wide" TFM can consume a "narrow" TFM,
  never the reverse.
- `netstandard2.0` remains the lowest common denominator for a library that
  needs to be consumed by both `net48` and `net8.0`/`net10.0`.
- `netstandard2.1` excludes .NET Framework — reserve it for libraries that
  don't need to be consumed by `net4x` projects.
- OS-specific TFMs (`-windows`, `-ios`, `-android`) are only compatible
  "downward" (they consume the base TFM), never "upward".
- For a multi-targeted solution, every `ProjectReference` must be
  compatible with **all** TFMs of the referencing project, not just the
  most recent one.
