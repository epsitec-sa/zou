---
author: Roger VUISTINER
co-author: GitHub Copilot
---

# ZOU MSBuild Import Architecture (SDK + TFM Babouchka)

## Purpose

This document describes the complete MSBuild import chain architecture used in ZOU-based projects. It explains how two nested import chains (the "babouchkas" or Russian dolls) work together to provide centralized configuration with granular override capabilities at multiple levels.

## Glossary

### Terminology

- **Babouchka** (or Matryoshka / Russian doll): Metaphor for a nested MSBuild import chain where each file imports the next in sequence, allowing configuration at different hierarchy levels
- **SDK babouchka**: The `Directory.Build.props` import chain managed by MSBuild's SDK
- **TFM babouchka**: The `zou.build.tfm.props` import chain managed by ZOU, nested within the SDK babouchka
- **BUNDLE**: The root repository of a solution or product line
- **MODULE**: A sub-folder or sub-module within the BUNDLE
- **PROJ.csproj**: An individual C# project file

### ZOU

ZOU is an extension layer on top of MSBuild's standard SDKs. It:
- Centralizes build scripts for different languages and frameworks
- Controls import chains (babouchkas) to allow configuration inheritance
- Provides shared defaults while enabling overrides at BUNDLE, MODULE, or project levels
- Is distributed as a shared sub-module across multiple repositories

## Architecture Overview

The ZOU import system consists of two nested babouchkas:

```
MODULE/PROJ.csproj
└── Sdk.props (implicit, injected by MSBuild)
    └── ┌─────────────────────────────────────────────────────────────────────────┐
        │ SDK Babouchka (Directory.Build.props chain)                             │
        │ ┌───────────────────────────────────────────────────────────────────────┤
        │ │ MODULE(s)/Directory.Build.props                                       │
        │ │ └── BUNDLE/Directory.Build.props                                      │
        │ │     └── zou/Directory.Build.default.props                             │
        │ │         └── zou/TargetFramework.props                                 │
        │ │             └── ┌─────────────────────────────────────────────────────┤
        │ │                 │ TFM Babouchka (zou.build.tfm.props)                 │
        │ │                 │ ┌───────────────────────────────────────────────────┤
        │ │                 │ │ zou/hook.zou.build.tfm.props                      │
        │ │                 │ │ └── MODULE(s)/zou.build.tfm.props                 │
        │ │                 │ │     └── BUNDLE/zou.build.tfm.props                │
        │ │                 │ │         └── zou/zou.build.tfm...                  │
        │ │                 │ │             └── zou/TargetFramework.build.props   │
        │ │                 │ └───────────────────────────────────────────────────┤
        │ │                 └─────────────────────────────────────────────────────┤
        │ └───────────────────────────────────────────────────────────────────────┤
        └─────────────────────────────────────────────────────────────────────────┘
    (rest of Sdk.props)
PROJ.csproj body (PropertyGroups, ItemGroups, etc.)
Sdk.targets (implicit)
└── Directory.Build.targets (not covered in this document)
```

### Complete Import Sequence

When building `PROJ.csproj`, MSBuild executes imports in this order:

1. **`MODULE/PROJ.csproj`** — Project file declaration
2. **`Sdk.props`** — Microsoft.NET.Sdk implicit import (injected by MSBuild)
3. **`MODULE(s)/Directory.Build.props`** — Start of SDK babouchka
4. **`BUNDLE/Directory.Build.props`** — Bundle-level defaults
5. **`zou/Directory.Build.default.props`** — ZOU-provided defaults
6. **`zou/TargetFramework.props`** — TFM defaults initialization + TFM babouchka start
7. **`zou/hook.zou.build.tfm.props`** — Start of TFM babouchka
8. **`MODULE(s)/zou.build.tfm.props`** — MODULE-level TFM overrides
9. **`BUNDLE/zou.build.tfm.props`** — BUNDLE-level TFM redirection
10. **`zou/zou.build.tfm.default.props`** — TFM babouchka processing
11. **`zou/TargetFramework.build.props`** — Final TFM resolution (single vs multi-targeting)
12. *(rest of `Sdk.props` continues...)*
13. **`PROJ.csproj` body** — Explicit PropertyGroups/ItemGroups in project file
14. **`Sdk.targets`** — Microsoft.NET.Sdk implicit import
15. **`Directory.Build.targets`** — (not detailed here)

## Part 1: SDK Babouchka (`Directory.Build.props` Chain)

### Purpose

The SDK babouchka establishes the foundation for centralized project configuration. It runs early in the build process (before the project file body is evaluated) and sets defaults that affect the entire build.

### Discovery Mechanism

MSBuild's `Sdk.props` (from `Microsoft.NET.Sdk`) automatically searches for `Directory.Build.props` starting from the project's directory and walking upward. **The search stops at the first file found** (stop-at-first-match rule).

This means:
- If `MODULE/Directory.Build.props` exists, it's imported and the search stops
- To inherit from parent levels, the MODULE file must explicitly chain using `<Import Project="..." />`
- Without explicit chaining, parent configurations are skipped

### Chain Flow

**Step 3**: `MODULE(s)/Directory.Build.props`
```xml
<Project>
  <PropertyGroup>
    <!-- MODULE-specific overrides -->
  </PropertyGroup>

  <!-- Explicit chain to parent -->
  <Import Project="$([MSBuild]::GetPathOfFileAbove('$(MSBuildThisFile)', '$(MSBuildThisFileDirectory)../'))" />
</Project>
```

**Step 4**: `BUNDLE/Directory.Build.props`

This BUNDLE-level file can define defaults for the whole BUNDLE before the ZOU standard defaults are imported. A common pattern is to define `CoreTargetFramework` for the entire BUNDLE while still delegating the rest of the shared build logic to ZOU.

```xml
<Project>
  <PropertyGroup>
    <CoreTargetFramework Condition="'$(CoreTargetFramework)' == ''">net8.0</CoreTargetFramework>
  </PropertyGroup>

  <Import Project="zou\Directory.Build.Default.props" />

  <PropertyGroup>
    <PackageTags>bundle</PackageTags>
    <PackageProjectUrl>https://example.invalid/bundle</PackageProjectUrl>
  </PropertyGroup>
</Project>
```

**Step 5**: `zou/Directory.Build.default.props`

This is the entry point to ZOU's standardized configuration. It:
- Imports `zou/zou.props` (defines ZOU infrastructure variables)
- Imports `zou/TargetFramework.props` (starts TFM configuration + TFM babouchka)
- Sets default build properties (`LangVersion`, `Deterministic`, `ProduceReferenceAssembly`, etc.)
- Configures code analysis defaults
- Sets up versioning conventions

**Step 6**: `zou/TargetFramework.props`

Defines default target framework values:
```xml
<PropertyGroup>
  <CoreTargetFramework>net10.0</CoreTargetFramework>
  <FullTargetFramework>net48</FullTargetFramework>
  <WindowsTargetFramework>$(CoreTargetFramework)-windows</WindowsTargetFramework>
  <CoreTargetFrameworks>net8.0;net10.0</CoreTargetFrameworks>
  <!-- etc. -->
</PropertyGroup>

<!-- Start TFM babouchka -->
<Import Project="hook.zou.build.tfm.props" Condition="'$(ZouBuildTfmProps)' == ''"/>
```

### What Gets Configured at Each Level

| Level | Typical Configuration |
|-------|----------------------|
| **MODULE** | MODULE-specific TFM overrides, project type settings, shared MODULE dependencies |
| **BUNDLE** | Author, product name, versioning, company-wide defaults, analyzer configurations |
| **zou** | Language version based on TFM, compiler flags, build acceleration, code analysis defaults, versioning patterns |

## Part 2: TFM Babouchka (`zou.build.tfm.props` Chain)

### Purpose

The TFM babouchka handles target framework selection and resolution. It allows:
- Centralized defaults for `TargetFramework` / `TargetFrameworks`
- Per-MODULE overrides (e.g., legacy projects staying on `net48`)
- A default single-targeting mode for backward compatibility and faster builds
- An explicit multi-targeting mode enabled with `$(MultiTargeting)=true`
- Framework-specific build logic

### Discovery Mechanism

Unlike the SDK babouchka (controlled by MSBuild's standard search), the TFM babouchka uses a custom upward search implemented in `zou/hook.zou.build.tfm.props`:

```xml
<PropertyGroup>
  <ZouBuildTfmProps>$([MSBuild]::GetPathOfFileAbove('zou.build.tfm.props', '$(MSBuildProjectDirectory)'))</ZouBuildTfmProps>
</PropertyGroup>
<Import Project="$(ZouBuildTfmProps)" Condition="Exists('$(ZouBuildTfmProps)')" />
```

This searches for the nearest `zou.build.tfm.props` starting from the project directory.

### Chain Flow

**Step 7**: `zou/hook.zou.build.tfm.props`

Entry point for TFM babouchka. Uses `GetPathOfFileAbove()` to find the first `zou.build.tfm.props` in the project's hierarchy.

**Step 8**: `MODULE(s)/zou.build.tfm.props`

MODULE-level TFM configuration using defaults from `TargetFramework.props`:
```xml
<Project>
  <PropertyGroup>
    <!-- Example: Multi-target netstandard2.0 + modern frameworks + .NET Framework -->
    <TargetFrameworks Condition="'$(TargetFrameworks)' == ''">netstandard2.0;$(CoreTargetFrameworks);$(FullTargetFramework)</TargetFrameworks>
  </PropertyGroup>

  <!-- Chain to parent -->
  <Import Project="$([MSBuild]::GetPathOfFileAbove('$(MSBuildThisFile)', '$(MSBuildThisFileDirectory)../'))" />
</Project>
```

Key pattern: Uses `$(CoreTargetFrameworks)`, `$(FullTargetFramework)`, etc. defined by `zou/TargetFramework.props` rather than hardcoding versions.

**Step 9**: `BUNDLE/zou.build.tfm.props`

Typically a redirection to ZOU's default processing:
```xml
<Project>
  <Import Project="zou\$(MSBuildThisFileName).default.props" />
</Project>
```

**Step 10**: `zou/zou.build.tfm.default.props`

Imports the final resolution logic:
```xml
<Project>
  <Import Project="TargetFramework.build.props" Condition="'$(TargetFrameworkBuildImported)' == ''"/>
</Project>
```

**Step 11**: `zou/TargetFramework.build.props`

The resolution engine. Key responsibilities:

1. **Default assignment** (if neither `TargetFramework` nor `TargetFrameworks` is set):
   ```xml
   <PropertyGroup Condition="'$(TargetFramework)' == '' And '$(TargetFrameworks)' == ''">
     <TargetFramework>$(CoreTargetFramework)</TargetFramework>
   </PropertyGroup>
   ```

2. **Multi-targeting mode** (`$(MultiTargeting)` == `true`):
   - Leaves `TargetFrameworks` as-is (plural)
   - Clears `TargetFramework` (singular) to trigger MSBuild's outer/inner build split

3. **Single-targeting mode** (`$(MultiTargeting)` == `false`, **default**):
   - Collapses `TargetFrameworks` (plural) down to a single `TargetFramework` (singular)
   - Selection priority:
     1. Exact match on `$(CoreTargetFramework)` (e.g., `net10.0`)
     2. Exact match on `$(WindowsTargetFramework)` (e.g., `net10.0-windows`)
     3. Exact match on `$(BrowserTargetFramework)` (e.g., `net10.0-browser`)
     4. Exact match on `$(FullTargetFramework)` (e.g., `net48`)
     5. Fallback to `netstandard2.0` or `netstandard2.1` if listed
   - Clears `TargetFrameworks` (plural) to prevent outer/inner build split
   - **Validation**: If no candidate matches, the build fails with a clear error rather than silently multi-targeting

4. **Build type classification**:
   ```xml
   <PropertyGroup>
     <BuildType Condition="...">Multi.Outer</BuildType>
     <BuildType Condition="...">Multi.Inner</BuildType>
     <BuildType Condition="...">Standard</BuildType>
   </PropertyGroup>
   ```

### Single vs Multi-targeting Logic

The default mode is **single-targeting**. It is kept for backward compatibility and gives faster builds because MSBuild does not enter the outer/inner build split.

To enable multi-targeting explicitly, set `$(MultiTargeting)=true`.

| MultiTargeting | TargetFrameworks Set | Result |
|----------------|---------------------|--------|
| `true` | Yes | Multi-targeting: outer/inner build split, builds for all frameworks listed in `TargetFrameworks` |
| `true` | No | Single framework from `$(CoreTargetFramework)` |
| `false` (default) | Yes | Single-targeting: collapses to one framework, no outer/inner split |
| `false` (default) | No | Single framework from `$(CoreTargetFramework)` |

**Use cases:**
- **Default builds**: single-targeting, faster and backward compatible
- **CI/CD build servers**: single-targeting by default unless multi-targeting is required
- **Local development or validation**: set `MultiTargeting=true` to build all listed frameworks
- **Legacy MODULE**: keep a local `TargetFrameworks` override to force a specific target framework set

## Override Points

The babouchka architecture provides override points at three levels:

### 1. Project Level (PROJ.csproj body)

**Limited effectiveness** — the project body is evaluated **after** both babouchkas complete. It is too late to influence TFM resolution or SDK-level decisions.

Use cases:
- Project-specific dependencies
- Output paths
- Build events

**Warning**: Setting `TargetFramework`/`TargetFrameworks` in the project body does not prevent the TFM babouchka from running. If you need to override TFM behavior, use a MODULE-level `zou.build.tfm.props` or `Directory.Build.props` file instead.

### 2. MODULE Level

**`MODULE/zou.build.tfm.props`** — Multi-targeting MODULE override:
```xml
<Project>
  <PropertyGroup>
    <TargetFrameworks Condition="'$(TargetFrameworks)' == ''">netstandard2.0;$(CoreTargetFrameworks);$(FullTargetFramework)</TargetFrameworks>
  </PropertyGroup>

  <Import Project="$([MSBuild]::GetPathOfFileAbove('$(MSBuildThisFile)', '$(MSBuildThisFileDirectory)../'))" />
</Project>
```

This pattern keeps `netstandard2.0` for broad compatibility while still reusing the non-hardcoded TFMs provided by ZOU and the BUNDLE defaults.
It is a good fit for modules that are intended to build in multi-targeting mode.

**Test-project override** — exclude `netstandard2.0` when a project does not support it:
```xml
<Project>
  <PropertyGroup>
    <TargetFrameworks Condition="'$(TargetFrameworks)' == ''">$(CoreTargetFrameworks)</TargetFrameworks>
  </PropertyGroup>

  <Import Project="$([MSBuild]::GetPathOfFileAbove('$(MSBuildThisFile)', '$(MSBuildThisFileDirectory)../'))" />
</Project>
```

This is the local exception used by test projects or other projects that must not include `netstandard2.0`.

**When to use:**
- A MODULE shares the same non-default TFM configuration across multiple projects
- A MODULE needs multi-targeting with ZOU-provided defaults
- A test-project subtree needs a narrower TFM set than the rest of the MODULE

### 3. BUNDLE Level

**`BUNDLE/Directory.Build.props`** — Bundle-wide standards and default TFM parameters:
```xml
<Project>
  <PropertyGroup>
    <CoreTargetFramework>net8.0</CoreTargetFramework>
  </PropertyGroup>

  <Import Project="zou\Directory.Build.Default.props" />

  <PropertyGroup>
    <PackageTags>bundle</PackageTags>
    <PackageProjectUrl>https://example.invalid/bundle</PackageProjectUrl>
  </PropertyGroup>
</Project>
```

This kind of BUNDLE-level override changes the default `CoreTargetFramework` for the whole bundle. MODULEs can then reuse that value instead of hardcoding framework versions.

**`BUNDLE/zou.build.tfm.props`** — Usually a redirection, but can set bundle-wide TFM defaults.

## Common Patterns

### Pattern 1: Single-targeting by default

This is the default mode. It is used for backward compatibility and faster builds because MSBuild stays in a single-targeting path.

**`BUNDLE/Directory.Build.props`**:
```xml
<Project>
  <PropertyGroup>
    <CoreTargetFramework>net8.0</CoreTargetFramework>
  </PropertyGroup>

  <Import Project="zou\Directory.Build.Default.props" />
</Project>
```

**Result**:
- Projects that do not opt into multi-targeting build against a single framework
- The selected framework comes from the collapse logic in `zou/TargetFramework.build.props`
- No outer/inner build split occurs

### Pattern 2: Multi-targeting opt-in for a MODULE

Enable multi-targeting explicitly with `MultiTargeting=true` when a MODULE needs to build several frameworks.

**`MODULE/zou.build.tfm.props`**:
```xml
<Project>
  <PropertyGroup>
    <TargetFrameworks Condition="'$(TargetFrameworks)' == ''">netstandard2.0;$(CoreTargetFrameworks);$(FullTargetFramework)</TargetFrameworks>
  </PropertyGroup>

  <Import Project="$([MSBuild]::GetPathOfFileAbove('$(MSBuildThisFile)', '$(MSBuildThisFileDirectory)../'))" />
</Project>
```

**Build**:
- `dotnet build` → single-targeting by default
- `dotnet build /p:MultiTargeting=true` → builds all frameworks listed in `TargetFrameworks`

### Pattern 3: Test-project override without `netstandard2.0`

Some projects, such as tests, do not support `netstandard2.0`. They can keep the same ZOU defaults but narrow the framework set locally.

**`MODULE/test subtree/zou.build.tfm.props`**:
```xml
<Project>
  <PropertyGroup>
    <TargetFrameworks Condition="'$(TargetFrameworks)' == ''">$(CoreTargetFrameworks);$(FullTargetFramework)</TargetFrameworks>
  </PropertyGroup>

  <Import Project="$([MSBuild]::GetPathOfFileAbove('$(MSBuildThisFile)', '$(MSBuildThisFileDirectory)../'))" />
</Project>
```

**Effect**:
- `netstandard2.0` is excluded locally
- The rest of the bundle can still keep the broader multi-targeting pattern

## Key Rules and Best Practices

1. **Stop-at-first-match + Explicit Chaining**
   - MSBuild stops at the first `Directory.Build.props` found
   - Always chain to the parent with `<Import Project="..." />` to inherit bundle/ZOU defaults

2. **Use Conditional Assignments** in override files:
   ```xml
   <PropertyGroup>
     <TargetFrameworks Condition="'$(TargetFrameworks)' == ''">...</TargetFrameworks>
   </PropertyGroup>
   ```
   This prevents overriding values already set by more specific (closer) files.

3. **Leverage ZOU Variables**
   - Use `$(CoreTargetFramework)`, `$(CoreTargetFrameworks)`, `$(WindowsTargetFramework)`, etc.
   - Avoids hardcoding versions; updates centrally propagate automatically

4. **Override at the Right Level**
   - **Project body**: Too late for TFM decisions, use for project-specific settings only
   - **MODULE**: For groups of projects sharing behavior
   - **BUNDLE**: For organization-wide standards

5. **Single-targeting by Default for CI/CD**
   - Keep the default single-targeting mode for faster builds and backward compatibility
   - Use `MultiTargeting=true` only when you need to validate all listed frameworks

6. **Validation Fails Fast**
   - If `TargetFrameworks` is set but no framework matches the collapse logic, the build fails with `$(ErrorInvalidTfmId)`
   - Prevents silent fallback to unexpected multi-targeting behavior

## Appendix: Tracing with TfmDebug

ZOU provides a built-in tracing mechanism to diagnose import order and TFM resolution issues.

### Enabling Tracing

Set the `TfmDebug` MSBuild property to `true`:

```bash
dotnet build /p:TfmDebug=true
```

Or in a `Directory.Build.props` file (only for debugging, not for production):
```xml
<PropertyGroup>
  <TfmDebug>true</TfmDebug>
</PropertyGroup>
```

### What Gets Traced

When `TfmDebug=true`, ZOU's import chain emits high-importance MSBuild messages showing:

1. **Import sequence confirmation** — Each file in the TFM babouchka logs when it executes
2. **Variable values** — Shows computed values for:
   - `TargetFramework` / `TargetFrameworks`
   - `CoreTargetFramework`, `WindowsTargetFramework`, etc.
   - `BuildType` (`Standard`, `Multi.Outer`, `Multi.Inner`)
   - `MultiTargeting` flag

### Example Output

```
zou/TargetFramework.props (MyProject.csproj)
MultiTargeting          = false
CoreTargetFramework     = net10.0
FullTargetFramework     = net48
WindowsTargetFramework  = net10.0-windows
CoreTargetFrameworks    = net8.0;net10.0
WindowsTargetFrameworks = net8.0-windows;net10.0-windows

zou/hook.zou.build.tfm.props (MyProject.csproj)
ZouBuildTfmProps = C:\path\to\Module\zou.build.tfm.props

zou/TargetFramework.build.props (MyProject.csproj)
BuildType        = Standard
TargetFramework  = net10.0
```

### Additional Tracing Flags

- **`ZouTrace=true`** — Enables tracing across all ZOU build scripts (broader than TfmDebug)
- **`ZouTraceDirectoryBuild=true`** — Traces SDK babouchka imports specifically
- **`ZouTraceVersion=true`** — Traces versioning resolution

Combine flags for comprehensive diagnostics:
```bash
dotnet build /p:TfmDebug=true /p:ZouTrace=true
```

### Use Cases

- **Import chain verification**: Confirm which files are being loaded and in what order
- **TFM resolution debugging**: Understand why a project ended up with a specific `TargetFramework`
- **Multi-targeting issues**: Diagnose unexpected outer/inner build splits
- **Override troubleshooting**: Verify which level's configuration is taking precedence

### Performance Note

Tracing adds overhead to the build (message logging). Disable it in production builds and CI/CD pipelines unless actively debugging build issues.

## See Also

- [TFM Resolution Troubleshooting](msbuild-import-order-and-tfm-resolution-en.md) — Detailed guide for resolving conflicts between project-body `TargetFramework` overrides and centralized defaults
- **zou/README.md** — ZOU infrastructure overview
- **MSBuild documentation** — [Customize your build (Microsoft)](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-your-build)

---

*This document describes the ZOU import architecture as of August 2026. For the latest updates, refer to the repository's commit history.*
