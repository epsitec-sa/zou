---
author: Roger VUISTINER
co-author: Claude Sonnet 5 Medium
---

# Compatibilité entre Target Framework Monikers (TFM)

## Objectif

Ce document décrit les règles de compatibilité entre les différents Target
Framework Monikers (TFM) utilisés dans le dépôt (`net48`, `netstandard2.x`,
`net8.0[-windows]`, `net10.0[-windows]`), et comment ces règles s'appliquent
aux `ProjectReference` et `PackageReference`.

## Principe général

La compatibilité entre TFM ne suit **pas** une simple relation
"plus récent > plus ancien" — elle suit les règles de précédence NuGet, qui
sont **à sens unique** : un projet qui cible un TFM "restreint" (moins
d'API) peut être consommé par un projet ciblant un TFM "plus large" (qui a
accès à ces API, plus d'autres), mais pas l'inverse.

Points clés :

- **`netstandard2.x`** n'est pas un runtime, c'est un contrat d'API. `net8.0`
  et `net10.0` **implémentent** `netstandard2.1` et toutes les versions
  antérieures. Un projet `net8.0` peut donc référencer un projet
  `netstandard2.0`/`netstandard2.1`, mais l'inverse est impossible : un
  projet `netstandard2.0` ne connaît pas les API de `net8.0`.
- Les **TFM spécifiques à un OS** (`net8.0-windows`, `net8.0-ios`,
  `net8.0-android`, `net10.0-windows`, ...) héritent de tout ce que propose
  leur TFM de base (`net8.0`, `net10.0`), plus les API spécifiques à la
  plateforme. `net8.0-windows` peut donc référencer un projet `net8.0` (ou
  `netstandard2.x`), mais un projet `net8.0` (base, cross-plateforme) ne
  peut pas référencer un projet `net8.0-windows`, puisqu'il n'a par
  définition aucune garantie de tourner sur Windows.
- `netstandard2.1` n'est **pas** implémenté par .NET Framework (`net48`
  inclus) — seul `netstandard2.0` l'est. C'est une source fréquente
  d'erreur `NU1201` dans les solutions mixtes legacy/moderne.
- Il n'existe pas de TFM `net8.0-browser`/`net10.0-browser` "officiel" au
  même titre que `-windows`/`-ios`/`-android` dans le SDK .NET standard.
  Blazor WebAssembly cible simplement `net8.0`/`net10.0` (base), le
  ciblage navigateur passant par le workload `wasm-tools`, pas par un TFM
  OS-spécifique dédié. (Certains frameworks tiers, comme Uno Platform,
  définissent leurs propres TFM custom du type `net8.0-browserwasm`, mais
  ce n'est pas un standard SDK.)

## Tableau de compatibilité (`ProjectReference` / `PackageReference`)

Lignes = TFM du projet qui **référence**, colonnes = TFM du projet
**référencé**. ✅ = référence autorisée, ❌ = incompatible.

| Référençant \ Référencé | net48 | netstandard2.0 | netstandard2.1 | net8.0 | net8.0-windows | net10.0 | net10.0-windows |
|---|---|---|---|---|---|---|---|
| **net48** | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| **netstandard2.0** | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| **netstandard2.1** | ❌ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| **net8.0** | ❌ | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| **net8.0-windows** | ❌ | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| **net10.0** | ❌ | ✅ | ✅ | ✅¹ | ❌ | ✅ | ❌ |
| **net10.0-windows** | ❌ | ✅ | ✅ | ✅¹ | ✅¹ | ✅ | ✅ |

¹ Un projet `net10.0(-windows)` peut référencer un projet `net8.0(-windows)`
via une **référence de package** (NuGet) — la compatibilité "ascendante"
entre versions `net5.0+` fonctionne via la précédence NuGet standard. Pour
une **`ProjectReference`** directe (compilation source-à-source au sein de
la même solution), MSBuild est en réalité plus permissif que la matrice
NuGet stricte et autorise aussi ce sens (`net10.0` référençant un `.csproj`
en `net8.0`), car il traite alors le projet référencé un peu comme une
dépendance binaire compatible. Ce n'est cependant pas un pattern
recommandé : en pratique, on aligne les projets d'une même solution sur le
même TFM "moderne" le plus élevé plutôt que de mélanger `net8.0` et
`net10.0` entre eux.

## Implications pour un projet multi-cible

Un projet qui multi-cible (ex. `TargetFrameworks=net8.0;net48`) doit avoir,
**pour chaque TFM qu'il cible**, un chemin de compatibilité vers chaque
`ProjectReference`. Cette vérification est effectuée séparément par inner
build (donc une fois par TFM — voir le document *Le split outer/inner
build*) :

- Si `ProjetA` cible `net8.0;net48` et référence `ProjetB` qui ne cible que
  `netstandard2.0`, ça fonctionne pour les deux TFM de `ProjetA`
  (`netstandard2.0` est compatible avec `net48` et `net8.0`).
- Si `ProjetB` cible `net8.0` uniquement (pas de `netstandard2.x`, pas de
  `net48`), alors le restore de `ProjetA` échoue **côté inner build
  `net48`** avec une erreur du type "project X is not compatible with
  net48 (.NETFramework,Version=v4.8)", même si l'inner build `net8.0`
  compile sans problème.

C'est un cas très courant dans une solution mixte legacy/moderne : chaque
bibliothèque partagée référencée par des projets `net48` doit rester en
`netstandard2.0`, ou multi-cibler elle-même
(`TargetFrameworks=netstandard2.0;net8.0`), pour rester consommable des
deux côtés.

## Points à retenir

- La compatibilité est asymétrique : un TFM "large" peut consommer un TFM
  "restreint", jamais l'inverse.
- `netstandard2.0` reste le plus petit dénominateur commun pour une
  bibliothèque devant être consommée à la fois par `net48` et par
  `net8.0`/`net10.0`.
- `netstandard2.1` exclut .NET Framework — à réserver aux bibliothèques
  qui n'ont pas besoin d'être consommées par des projets `net4x`.
- Les TFM OS-spécifiques (`-windows`, `-ios`, `-android`) ne sont
  compatibles que "vers le bas" (ils consomment le TFM de base), jamais
  "vers le haut".
- Pour une solution multi-cible, chaque `ProjectReference` doit être
  compatible avec **tous** les TFM du projet référençant, pas seulement le
  TFM le plus récent.
