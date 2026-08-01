---
author: Roger VUISTINER
co-author: Claude Sonnet 5 Medium
---

# Ordre d'import MSBuild et résolution de TargetFramework

## Objectif

Ce document explique l'ordre d'import MSBuild exact pour les projets de
style SDK (`<Project Sdk="Microsoft.NET.Sdk">`), pourquoi centraliser les
valeurs par défaut de `TargetFramework(s)` dans `Directory.Build.props`
entre en conflit avec les surcharges au niveau projet, et le pattern utilisé
dans ce dépôt pour résoudre ce conflit.

## Contexte : le problème

Nous centralisons le(s) target framework(s) par défaut dans une feuille de
propriétés partagée (`TargetFramework.props`, importée depuis le
`Directory.Build.props` racine du dépôt) afin que la plupart des projets
n'aient pas à répéter `net8.0;net48` partout.

Le problème : `Directory.Build.props` est importé **avant** que le corps du
`.csproj` ne soit évalué. Si un projet définit `TargetFramework` (singulier)
dans son propre corps pour forcer un ciblage simple, cette valeur n'existe
pas encore au moment où `Directory.Build.props` s'exécute. Une garde naïve
du type :

```xml
<PropertyGroup Condition="'$(TargetFramework)' == '' and '$(TargetFrameworks)' == ''">
  <TargetFrameworks>net8.0;net48</TargetFrameworks>
</PropertyGroup>
```

verra toujours les deux propriétés comme vides à ce stade, donc la valeur
par défaut centralisée l'emporte toujours — même pour un projet qui veut un
ciblage simple. Résultat : `TargetFramework` (singulier, corps du projet) et
`TargetFrameworks` (pluriel, depuis `Directory.Build.props`) se retrouvent
tous les deux définis simultanément, ce qui déclenche le split outer/inner
build du SDK (comportements type `NETSDK1013`, chemins de sortie inattendus,
tâches se comportant comme si le projet était multi-cible).

Déplacer la logique dans un fichier `.targets` (importé en fin d'évaluation)
ne résout pas non plus le problème : au moment où `Directory.Build.targets`
s'exécute, `Sdk.props` a déjà pris ses décisions dépendantes du TFM (imports
spécifiques au framework, split outer/inner build). Il est trop tard pour
influencer cela.

## L'ordre d'import réel

Pour `dotnet build PROJ.csproj` ou `msbuild PROJ.csproj` sur un projet de
style SDK, MSBuild ne laisse **pas** le corps du `.csproj` ni la ligne de
commande importer `Directory.Build.props` — c'est `Sdk.props` qui s'en
charge, dans le cadre de la chaîne d'import implicite du SDK :

```
1. Sdk.props                         (import implicite, injecté par MSBuild
                                       à cause de Sdk="Microsoft.NET.Sdk")
   └── Directory.Build.props         (recherché en remontant depuis le
                                       dossier du projet, importé s'il est
                                       trouvé — la recherche S'ARRÊTE au
                                       premier fichier trouvé)
   └── (suite de Sdk.props : valeurs par défaut du TFM, décision du split
        outer/inner build, imports spécifiques au framework)

2. Corps du PROJ.csproj               (PropertyGroup/ItemGroup explicites
                                       écrits dans le fichier projet)

3. Sdk.targets                       (import implicite, en fin de fichier)
   └── Directory.Build.targets       (recherche ascendante, même règle
                                       d'arrêt au premier fichier trouvé)
```

Points clés :

- Les imports implicites de `Sdk.props` (en haut) et `Sdk.targets` (en bas)
  sont injectés par le parseur XML de MSBuild à cause de l'attribut
  `Sdk="..."` — avant même que le reste du fichier ne soit lu.
- La découverte de `Directory.Build.props` / `Directory.Build.targets`
  n'est pas effectuée par MSBuild lui-même ni par la ligne de commande —
  elle a lieu à l'intérieur de `Sdk.props` / `Sdk.targets` (via
  `Microsoft.Common.props` / `Microsoft.Common.targets`), contrôlée par
  `$(ImportDirectoryBuildProps)` / `$(ImportDirectoryBuildTargets)` (`true`
  par défaut).
- La recherche ascendante **s'arrête au premier `Directory.Build.props`
  trouvé**, en partant de `$(MSBuildProjectDirectory)`. Elle ne continue pas
  automatiquement au-delà de ce fichier — le chaînage vers un parent doit
  être explicite.
- À cause de cela, le corps du `.csproj` (étape 2) ne peut jamais influencer
  ce que `Sdk.props` (étape 1) a déjà décidé, y compris le split outer/inner
  build dépendant du TFM.

## Le pattern : `Directory.Build.props` par dossier

Puisque le corps du projet est évalué trop tard, mais qu'un
`Directory.Build.props` est évalué suffisamment tôt (étape 1), on remonte la
surcharge à ce niveau-là. Pour un projet (ou un groupe de projets) qui doit
sortir de la valeur par défaut centralisée, on ajoute un
`Directory.Build.props` **directement dans le dossier de ce projet** (ou
dans un dossier couvrant tout le groupe), en chaînant explicitement vers le
parent :

```xml
<!-- MonProjet/Directory.Build.props -->
<Project>
  <PropertyGroup>
    <TargetFrameworks>net8.0</TargetFrameworks>
  </PropertyGroup>

  <!-- Chaînage explicite vers le Directory.Build.props racine du dépôt,
       qui doit toujours s'exécuter pour tout ce qu'il configure par
       ailleurs (pas seulement le TFM). -->
  <Import Project="$(MSBuildThisFileDirectory)..\Directory.Build.props" />
</Project>
```

Dans le `TargetFramework.props` racine, la condition de garde fonctionne
alors correctement, car au moment où elle s'exécute, le fichier local a déjà
défini `TargetFrameworks` :

```xml
<!-- Directory.Build.props (racine du dépôt) -->
<Import Project="TargetFramework.props" />
```

```xml
<!-- TargetFramework.props -->
<PropertyGroup Condition="'$(TargetFramework)' == '' and '$(TargetFrameworks)' == ''">
  <TargetFrameworks>net8.0;net48</TargetFrameworks>
</PropertyGroup>
```

Pourquoi ça fonctionne, contrairement à une approche dans le corps du
`.csproj` : MSBuild ne découvre automatiquement qu'**un seul**
`Directory.Build.props` par projet (le plus proche en remontant). En posant
le nôtre au niveau du projet et en important explicitement le parent depuis
ce fichier, on contrôle l'ordre nous-mêmes — le `PropertyGroup` du fichier
local s'exécute, puis la garde du parent voit un `TargetFrameworks` non-vide
et se désactive. Rien dans le corps du `.csproj` ne pourrait jamais obtenir
ce résultat, puisque le corps est évalué strictement après `Sdk.props` (et
donc après `Directory.Build.props`).

### Quand utiliser ce pattern

- Un **groupe de projets** partageant le(s) même(s) TFM(s) non standard
  (ex. un ensemble de projets legacy net48 uniquement) : placer un seul
  `Directory.Build.props` dans le dossier qui les contient tous.
- Un **projet isolé** ayant besoin d'un ciblage simple alors que le reste
  du dépôt multi-cible : même pattern, limité au dossier de ce projet.

### Ce qu'il faut éviter

- Ne pas essayer de gérer le conflit `TargetFramework`/`TargetFrameworks`
  depuis le corps du `.csproj` ou depuis un fichier `.targets` — les deux
  s'exécutent trop tard pour empêcher le split outer/inner build, déjà
  décidé dans `Sdk.props`.
- Ne pas compter sur un chaînage ascendant automatique de
  `Directory.Build.props` — la recherche s'arrête au premier fichier
  trouvé, donc un fichier local qui n'importe pas explicitement son parent
  ignorera silencieusement toute la configuration du dépôt racine (pas
  seulement les valeurs par défaut du TFM).

## Tableau récapitulatif

| Étape | Ce qui s'exécute | Peut encore influencer le split TFM ? |
|---|---|---|
| Import implicite de `Sdk.props` | Injecté par le parseur MSBuild | — |
| → `Directory.Build.props` (le plus proche, recherche ascendante) | Nos valeurs par défaut/surcharges TFM vivent ici | **Oui** — le seul endroit sûr |
| → suite de `Sdk.props` | Résolution du TFM, décision du split outer/inner build | Trop tard après ce point |
| Corps du `.csproj` | `PropertyGroup`/`ItemGroup` explicites | Non |
| Import implicite de `Sdk.targets` + `Directory.Build.targets` | Logique post-graphe de build | Non |
