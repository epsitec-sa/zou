---
author: Roger VUISTINER
co-author: GitHub Copilot
---

# Architecture d'importation MSBuild ZOU (SDK + TFM Babouchka)

## Objectif

Ce document décrit l'architecture complète de la chaîne d'importation MSBuild utilisée dans les projets basés sur ZOU. Il explique comment deux chaînes d'importation imbriquées (les "babouchkas" ou poupées russes) collaborent pour fournir une configuration centralisée avec des capacités de surcharge granulaires à plusieurs niveaux.

## Glossaire

### Terminologie

- **Babouchka** (ou Matriochka / poupée russe) : Métaphore pour une chaîne d'importation MSBuild imbriquée où chaque fichier importe le suivant dans la séquence, permettant une configuration à différents niveaux hiérarchiques
- **Babouchka SDK** : La chaîne d'importation des `Directory.Build.props` gérée par le SDK MSBuild
- **Babouchka TFM** : La chaîne d'importation des `zou.build.tfm.props` gérée par ZOU, imbriquée dans la babouchka SDK
- **BUNDLE** : Le dépôt racine d’une solution ou d’une ligne de produits
- **MODULE** : Un sous-dossier ou sous-module au sein du BUNDLE
- **PROJ.csproj** : Un fichier de projet C# individuel

### ZOU

ZOU est une couche d'extension au-dessus des SDKs standard de MSBuild. Il :
- Centralise les scripts de build pour différents langages et frameworks
- Contrôle les chaînes d'importation (babouchkas) pour permettre l'héritage de configuration
- Fournit des valeurs par défaut partagées tout en permettant des surcharges aux niveaux BUNDLE, MODULE ou projet
- Est distribué comme un sous-module partagé entre plusieurs dépôts

## Vue d'ensemble de l'architecture

Le système d'importation ZOU consiste en deux babouchkas imbriquées :

```
MODULE/PROJ.csproj
└── Sdk.props (implicite, injecté par MSBuild)
    └── ┌─────────────────────────────────────────────────────────────────────────┐
        │ Babouchka SDK (chaîne Directory.Build.props)                            │
        │ ┌───────────────────────────────────────────────────────────────────────┤
        │ │ MODULE(s)/Directory.Build.props                                       │
        │ │ └── BUNDLE/Directory.Build.props                                      │
        │ │     └── zou/Directory.Build.default.props                             │
        │ │         └── zou/TargetFramework.props                                 │
        │ │             └── ┌─────────────────────────────────────────────────────┤
        │ │                 │ Babouchka TFM (zou.build.tfm.props)                 │
        │ │                 │ ┌───────────────────────────────────────────────────┤
        │ │                 │ │ zou/hook.zou.build.tfm.props                      │
        │ │                 │ │ └── MODULE(s)/zou.build.tfm.props                 │
        │ │                 │ │     └── BUNDLE/zou.build.tfm.props                │
        │ │                 │ │         └── zou/zou.build.tfm.default.props       │
        │ │                 │ │             └── zou/TargetFramework.build.props   │
        │ │                 │ └───────────────────────────────────────────────────┤
        │ │                 └─────────────────────────────────────────────────────┤
        │ └───────────────────────────────────────────────────────────────────────┤
        └─────────────────────────────────────────────────────────────────────────┘
    (suite de Sdk.props)
Corps de PROJ.csproj (PropertyGroups, ItemGroups, etc.)
Sdk.targets (implicite)
└── Directory.Build.targets (non couvert dans ce document)
```

### Séquence d'importation complète

Lors de la construction de `PROJ.csproj`, MSBuild exécute les importations dans cet ordre :

1. **`MODULE/PROJ.csproj`** — Déclaration du fichier projet
2. **`Sdk.props`** — Import implicite Microsoft.NET.Sdk (injecté par MSBuild)
3. **`MODULE(s)/Directory.Build.props`** — Début de la babouchka SDK
4. **`BUNDLE/Directory.Build.props`** — Valeurs par défaut au niveau BUNDLE
5. **`zou/Directory.Build.default.props`** — Valeurs par défaut fournies par ZOU
6. **`zou/TargetFramework.props`** — Initialisation des défauts TFM + démarrage babouchka TFM
7. **`zou/hook.zou.build.tfm.props`** — Début de la babouchka TFM
8. **`MODULE(s)/zou.build.tfm.props`** — Surcharges TFM au niveau MODULE
9. **`BUNDLE/zou.build.tfm.props`** — Redirection TFM au niveau BUNDLE
10. **`zou/zou.build.tfm.default.props`** — Traitement de la babouchka TFM
11. **`zou/TargetFramework.build.props`** — Résolution TFM finale (ciblage simple vs multiple)
12. *(suite de `Sdk.props`...)*
13. **`Corps de PROJ.csproj`** — PropertyGroups/ItemGroups explicites dans le fichier projet
14. **`Sdk.targets`** — Import implicite Microsoft.NET.Sdk
15. **`Directory.Build.targets`** — (non détaillé ici)

## Partie 1 : Babouchka SDK (`Directory.Build.props` chain)

### Objectif

La babouchka SDK établit la fondation pour la configuration de projet centralisée. Elle s'exécute tôt dans le processus de build (avant que le corps du fichier projet ne soit évalué) et définit des valeurs par défaut qui affectent l'ensemble du build.

### Mécanisme de découverte

Le `Sdk.props` de MSBuild (depuis `Microsoft.NET.Sdk`) recherche automatiquement `Directory.Build.props` en partant du dossier du projet et en remontant. **La recherche s'arrête au premier fichier trouvé** (règle stop-at-first-match).

Cela signifie :
- Si `MODULE/Directory.Build.props` existe, il est importé et la recherche s'arrête
- Pour hériter des niveaux parents, le fichier MODULE doit chaîner explicitement avec `<Import Project="..." />`
- Sans chaînage explicite, les configurations parentes sont ignorées

### Flux de la chaîne

**Étape 3** : `MODULE(s)/Directory.Build.props`
```xml
<Project>
  <PropertyGroup>
    <!-- Surcharges spécifiques au MODULE -->
  </PropertyGroup>

  <!-- Chaînage explicite vers le parent -->
  <Import Project="$([MSBuild]::GetPathOfFileAbove('$(MSBuildThisFile)', '$(MSBuildThisFileDirectory)../'))" />
</Project>
```

**Étape 4** : `BUNDLE/Directory.Build.props`

Ce fichier au niveau BUNDLE peut définir les valeurs par défaut pour tout le BUNDLE avant l’import des valeurs standard de ZOU. Un pattern courant consiste à définir `CoreTargetFramework` pour tout le BUNDLE tout en déléguant le reste de la logique de build partagée à ZOU.

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

**Étape 5** : `zou/Directory.Build.default.props`

C'est le point d'entrée vers la configuration standardisée de ZOU. Il :
- Importe `zou/zou.props` (définit les variables d'infrastructure ZOU)
- Importe `zou/TargetFramework.props` (démarre la configuration TFM + babouchka TFM)
- Définit les propriétés de build par défaut (`LangVersion`, `Deterministic`, `ProduceReferenceAssembly`, etc.)
- Configure les valeurs par défaut d'analyse de code
- Met en place les conventions de versionnement

**Étape 6** : `zou/TargetFramework.props`

Définit les valeurs par défaut des target frameworks :
```xml
<PropertyGroup>
  <CoreTargetFramework>net10.0</CoreTargetFramework>
  <FullTargetFramework>net48</FullTargetFramework>
  <WindowsTargetFramework>$(CoreTargetFramework)-windows</WindowsTargetFramework>
  <CoreTargetFrameworks>net8.0;net10.0</CoreTargetFrameworks>
  <!-- etc. -->
</PropertyGroup>

<!-- Démarrage babouchka TFM -->
<Import Project="hook.zou.build.tfm.props" Condition="'$(ZouBuildTfmProps)' == ''"/>
```

### Ce qui est configuré à chaque niveau

| Niveau | Configuration typique |
|--------|----------------------|
| **MODULE** | Surcharges TFM spécifiques au MODULE, paramètres du type de projet, dépendances partagées du MODULE |
| **BUNDLE** | Auteur, nom du produit, versionnement, valeurs par défaut de l'organisation, configurations d'analyseurs |
| **zou** | Version du langage basée sur le TFM, drapeaux du compilateur, accélération du build, valeurs par défaut d'analyse de code, patterns de versionnement |

## Partie 2 : Babouchka TFM (`zou.build.tfm.props` chain)

### Objectif

La babouchka TFM gère la sélection et la résolution des target frameworks. Elle permet :
- Des valeurs par défaut centralisées pour `TargetFramework` / `TargetFrameworks`
- Des surcharges par MODULE (ex. projets legacy restant sur `net48`)
- Un mode par défaut en ciblage simple, conservé pour la compatibilité ascendante et des builds plus rapides
- Un mode multi-ciblage activé explicitement avec `$(MultiTargeting)=true`
- Une logique de build spécifique au framework

### Mécanisme de découverte

Contrairement à la babouchka SDK (contrôlée par la recherche standard de MSBuild), la babouchka TFM utilise une recherche ascendante personnalisée implémentée dans `zou/hook.zou.build.tfm.props` :

```xml
<PropertyGroup>
  <ZouBuildTfmProps>$([MSBuild]::GetPathOfFileAbove('zou.build.tfm.props', '$(MSBuildProjectDirectory)'))</ZouBuildTfmProps>
</PropertyGroup>
<Import Project="$(ZouBuildTfmProps)" Condition="Exists('$(ZouBuildTfmProps)')" />
```

Cela recherche le `zou.build.tfm.props` le plus proche en partant du dossier du projet.

### Flux de la chaîne

**Étape 7** : `zou/hook.zou.build.tfm.props`

Point d'entrée de la babouchka TFM. Utilise `GetPathOfFileAbove()` pour trouver le premier `zou.build.tfm.props` dans la hiérarchie du projet.

**Étape 8** : `MODULE(s)/zou.build.tfm.props`

Configuration TFM au niveau MODULE utilisant les défauts de `TargetFramework.props` :
```xml
<Project>
  <PropertyGroup>
    <!-- Exemple : Multi-ciblage netstandard2.0 + frameworks modernes + .NET Framework -->
    <TargetFrameworks Condition="'$(TargetFrameworks)' == ''">netstandard2.0;$(CoreTargetFrameworks);$(FullTargetFramework)</TargetFrameworks>
  </PropertyGroup>

  <!-- Chaînage vers le parent -->
  <Import Project="$([MSBuild]::GetPathOfFileAbove('$(MSBuildThisFile)', '$(MSBuildThisFileDirectory)../'))" />
</Project>
```

Pattern clé : Utilise `$(CoreTargetFrameworks)`, `$(FullTargetFramework)`, etc. définis par `zou/TargetFramework.props` plutôt que de coder en dur les versions.

**Étape 9** : `BUNDLE/zou.build.tfm.props`

Typiquement une redirection vers le traitement par défaut de ZOU :
```xml
<Project>
  <Import Project="zou\$(MSBuildThisFileName).default.props" />
</Project>
```

**Étape 10** : `zou/zou.build.tfm.default.props`

Importe la logique de résolution finale :
```xml
<Project>
  <Import Project="TargetFramework.build.props" Condition="'$(TargetFrameworkBuildImported)' == ''"/>
</Project>
```

**Étape 11** : `zou/TargetFramework.build.props`

Le moteur de résolution. Responsabilités clés :

1. **Affectation par défaut** (si ni `TargetFramework` ni `TargetFrameworks` n'est défini) :
   ```xml
   <PropertyGroup Condition="'$(TargetFramework)' == '' And '$(TargetFrameworks)' == ''">
     <TargetFramework>$(CoreTargetFramework)</TargetFramework>
   </PropertyGroup>
   ```

2. **Mode multi-ciblage** (`$(MultiTargeting)` == `true`) :
   - Laisse `TargetFrameworks` tel quel (pluriel)
   - Efface `TargetFramework` (singulier) pour déclencher le split outer/inner build de MSBuild

3. **Mode ciblage simple** (`$(MultiTargeting)` == `false`, **par défaut**) :
    - Réduit `TargetFrameworks` (pluriel) en un seul `TargetFramework` (singulier)
    - Priorité de sélection :
      1. Correspondance exacte sur `$(CoreTargetFramework)` (ex. `net10.0`)
      2. Correspondance exacte sur `$(WindowsTargetFramework)` (ex. `net10.0-windows`)
      3. Correspondance exacte sur `$(BrowserTargetFramework)` (ex. `net10.0-browser`)
      4. Correspondance exacte sur `$(FullTargetFramework)` (ex. `net48`)
      5. Repli sur `netstandard2.0` ou `netstandard2.1` si listé
    - Efface `TargetFrameworks` (pluriel) pour éviter le split outer/inner build
    - **Validation** : Si aucun candidat ne correspond, le build échoue avec une erreur claire plutôt que de passer silencieusement en multi-ciblage

4. **Classification du type de build** :
   ```xml
   <PropertyGroup>
     <BuildType Condition="...">Multi.Outer</BuildType>
     <BuildType Condition="...">Multi.Inner</BuildType>
     <BuildType Condition="...">Standard</BuildType>
   </PropertyGroup>
   ```

### Logique ciblage simple vs multiple

Le mode par défaut est le **ciblage simple**. Il est conservé pour la compatibilité ascendante et produit des builds plus rapides, car MSBuild ne passe pas par le split outer/inner build.

Pour activer le multi-ciblage, définissez explicitement `$(MultiTargeting)=true`.

| MultiTargeting | TargetFrameworks défini | Résultat |
|----------------|------------------------|----------|
| `true` | Oui | Multi-ciblage : split outer/inner build, construit pour tous les frameworks listés dans `TargetFrameworks` |
| `true` | Non | Framework unique depuis `$(CoreTargetFramework)` |
| `false` (défaut) | Oui | Ciblage simple : réduit à un seul framework, pas de split outer/inner |
| `false` (défaut) | Non | Framework unique depuis `$(CoreTargetFramework)` |

**Cas d'usage:**
- **Builds par défaut** : ciblage simple, plus rapide et compatible avec l'existant
- **Serveurs de build CI/CD** : ciblage simple par défaut, sauf besoin explicite de multi-ciblage
- **Développement local ou validation** : `MultiTargeting=true` pour construire tous les frameworks listés
- **MODULE legacy** : conserver une surcharge locale de `TargetFrameworks` pour imposer un ensemble précis de frameworks

## Points de surcharge

L'architecture babouchka fournit des points de surcharge à trois niveaux :

### 1. Niveau projet (corps PROJ.csproj)

**Efficacité limitée** — le corps du projet est évalué **après** que les deux babouchkas aient terminé. Ne peut pas influencer la résolution TFM ou les décisions au niveau SDK.

Cas d'usage :
- Dépendances spécifiques au projet
- Chemins de sortie
- Événements de build

**Avertissement** : Définir `TargetFramework`/`TargetFrameworks` dans le corps du projet n'empêche pas la babouchka TFM de s'exécuter. Si vous devez surcharger le comportement TFM, utilisez plutôt un `Directory.Build.props` au niveau MODULE (voir ci-dessous).

### 2. Niveau MODULE

**`MODULE/zou.build.tfm.props`** — Surcharge multi-ciblage au niveau MODULE :
```xml
<Project>
  <PropertyGroup>
    <TargetFrameworks Condition="'$(TargetFrameworks)' == ''">netstandard2.0;$(CoreTargetFrameworks);$(FullTargetFramework)</TargetFrameworks>
  </PropertyGroup>

  <Import Project="$([MSBuild]::GetPathOfFileAbove('$(MSBuildThisFile)', '$(MSBuildThisFileDirectory)../'))" />
</Project>
```

Ce pattern garde `netstandard2.0` pour la compatibilité large tout en réutilisant les TFMs non codés en dur fournis par ZOU et par les défauts du BUNDLE. Il convient bien aux modules destinés à fonctionner en multi-ciblage.

**Surcharge de projet de test** — exclure `netstandard2.0` lorsqu’un projet ne le supporte pas :
```xml
<Project>
  <PropertyGroup>
    <TargetFrameworks Condition="'$(TargetFrameworks)' == ''">$(CoreTargetFrameworks);$(FullTargetFramework)</TargetFrameworks>
  </PropertyGroup>

  <Import Project="$([MSBuild]::GetPathOfFileAbove('$(MSBuildThisFile)', '$(MSBuildThisFileDirectory)../'))" />
</Project>
```

C’est l’exception locale utilisée pour les projets de test ou d’autres projets qui ne doivent pas inclure `netstandard2.0`.

**Quand l’utiliser** :
- Un MODULE partage la même configuration TFM non standard sur plusieurs projets
- Un MODULE a besoin du multi-ciblage avec les défauts fournis par ZOU
- Un sous-arbre de tests a besoin d’un ensemble de TFMs plus restreint que le reste du MODULE

### 3. Niveau BUNDLE

**`BUNDLE/Directory.Build.props`** — Standards au niveau BUNDLE et paramètres TFM par défaut :
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

Ce type de surcharge au niveau BUNDLE modifie le `CoreTargetFramework` par défaut pour l’ensemble du bundle. Les MODULEs peuvent ensuite réutiliser cette valeur sans coder les frameworks en dur.

**`BUNDLE/zou.build.tfm.props`** — Généralement une redirection, mais peut définir des défauts TFM au niveau du bundle.

## Patterns courants

### Pattern 1 : Multi-ciblage par défaut (bibliothèques NuGet)

**`BUNDLE/Directory.Build.props`** :
```xml
<PropertyGroup>
  <CoreTargetFramework>net10.0</CoreTargetFramework>
</PropertyGroup>
```

**`MODULE/zou.build.tfm.props`** :
```xml
<PropertyGroup>
  <TargetFrameworks Condition="'$(TargetFrameworks)' == ''">netstandard2.0;$(CoreTargetFrameworks)</TargetFrameworks>
</PropertyGroup>
```

**Build** :
- `dotnet build` (défaut : `MultiTargeting=false`) → Construit pour `net10.0` uniquement
- `dotnet build /p:MultiTargeting=true` → Construit pour `netstandard2.0`, `net8.0`, `net10.0`

### Pattern 2 : Surcharge ciblage simple par MODULE

**Scénario** : Un MODULE doit rester sur `net48` tandis que le reste du bundle utilise des frameworks modernes.

**`LegacyModule/Directory.Build.props`** :
```xml
<Project>
  <PropertyGroup>
    <TargetFrameworks>net48</TargetFrameworks>
  </PropertyGroup>

  <Import Project="$(MSBuildThisFileDirectory)..\Directory.Build.props" />
</Project>
```

Résultat : Tous les projets dans `LegacyModule/` héritent du ciblage `net48`, indépendamment des défauts du bundle.

### Pattern 3 : MODULE spécifique Windows

**`WindowsModule/zou.build.tfm.props`** :
```xml
<Project>
  <PropertyGroup>
    <TargetFrameworks Condition="'$(TargetFrameworks)' == ''">$(WindowsTargetFrameworks)</TargetFrameworks>
  </PropertyGroup>

  <Import Project="$([MSBuild]::GetPathOfFileAbove('$(MSBuildThisFile)', '$(MSBuildThisFileDirectory)../'))" />
</Project>
```

Utilise `$(WindowsTargetFrameworks)` (ex. `net8.0-windows;net10.0-windows`) défini par `zou/TargetFramework.props`.

### Pattern 4 : Builds rapides CI/CD

**Commande de build** :
```bash
dotnet build /p:MultiTargeting=false  # Défaut
```

Effet : Même si `TargetFrameworks` liste plusieurs frameworks, `zou/TargetFramework.build.props` réduit à `$(CoreTargetFramework)` uniquement. Pas de split outer/inner build, builds plus rapides, un seul SDK requis.

## Règles clés et bonnes pratiques

1. **Stop-at-first-match + Chaînage explicite**
   - MSBuild s'arrête au premier `Directory.Build.props` trouvé
   - Toujours chaîner vers le parent avec `<Import Project="..." />` pour hériter des défauts bundle/ZOU

2. **Utiliser des affectations conditionnelles** dans les fichiers de surcharge :
   ```xml
   <PropertyGroup>
     <TargetFrameworks Condition="'$(TargetFrameworks)' == ''">...</TargetFrameworks>
   </PropertyGroup>
   ```
   Cela évite de surcharger les valeurs déjà définies par des fichiers plus spécifiques (plus proches).

3. **Exploiter les variables ZOU**
   - Utiliser `$(CoreTargetFramework)`, `$(CoreTargetFrameworks)`, `$(WindowsTargetFramework)`, etc.
   - Évite de coder les versions en dur ; les mises à jour centrales se propagent automatiquement

4. **Surcharger au bon niveau**
   - **Corps du projet** : Trop tard pour les décisions TFM, utiliser uniquement pour les paramètres spécifiques au projet
   - **MODULE** : Pour les groupes de projets partageant un comportement
   - **BUNDLE** : Pour les standards à l'échelle de l'organisation

5. **Ciblage simple par défaut pour CI/CD**
   - Garder `MultiTargeting=false` (défaut) dans les pipelines CI
   - Utiliser `MultiTargeting=true` localement lors de la validation de scénarios multi-ciblage

6. **La validation échoue rapidement**
   - Si `TargetFrameworks` est défini mais aucun framework ne correspond à la logique de réduction, le build échoue avec `$(ErrorInvalidTfmId)`
   - Empêche le repli silencieux sur un comportement multi-ciblage inattendu

## Annexe : Traçage avec TfmDebug

ZOU fournit un mécanisme de traçage intégré pour diagnostiquer les problèmes d'ordre d'importation et de résolution TFM.

### Activation du traçage

Définir la propriété MSBuild `TfmDebug` à `true` :

```bash
dotnet build /p:TfmDebug=true
```

Ou dans un fichier `Directory.Build.props` (uniquement pour le débogage, pas en production) :
```xml
<PropertyGroup>
  <TfmDebug>true</TfmDebug>
</PropertyGroup>
```

### Ce qui est tracé

Quand `TfmDebug=true`, la chaîne d'importation ZOU émet des messages MSBuild de haute importance montrant :

1. **Confirmation de la séquence d'importation** — Chaque fichier de la babouchka TFM enregistre son exécution
2. **Valeurs des variables** — Affiche les valeurs calculées pour :
   - `TargetFramework` / `TargetFrameworks`
   - `CoreTargetFramework`, `WindowsTargetFramework`, etc.
   - `BuildType` (`Standard`, `Multi.Outer`, `Multi.Inner`)
   - Drapeau `MultiTargeting`

### Exemple de sortie

```
zou/TargetFramework.props (MyProject.csproj)
MultiTargeting          = false
CoreTargetFramework     = net10.0
FullTargetFramework     = net48
WindowsTargetFramework  = net10.0-windows
CoreTargetFrameworks    = net8.0;net10.0
WindowsTargetFrameworks = net8.0-windows;net10.0-windows

zou/hook.zou.build.tfm.props (MyProject.csproj)
ZouBuildTfmProps = C:\chemin\vers\Module\zou.build.tfm.props

zou/TargetFramework.build.props (MyProject.csproj)
BuildType        = Standard
TargetFramework  = net10.0
```

### Drapeaux de traçage supplémentaires

- **`ZouTrace=true`** — Active le traçage sur tous les scripts de build ZOU (plus large que TfmDebug)
- **`ZouTraceDirectoryBuild=true`** — Trace spécifiquement les importations de la babouchka SDK
- **`ZouTraceVersion=true`** — Trace la résolution du versionnement

Combiner les drapeaux pour des diagnostics complets :
```bash
dotnet build /p:TfmDebug=true /p:ZouTrace=true
```

### Cas d'usage

- **Vérification de la chaîne d'importation** : Confirmer quels fichiers sont chargés et dans quel ordre
- **Débogage de la résolution TFM** : Comprendre pourquoi un projet s'est retrouvé avec un `TargetFramework` spécifique
- **Problèmes de multi-ciblage** : Diagnostiquer des splits outer/inner build inattendus
- **Dépannage des surcharges** : Vérifier quelle configuration de niveau prend le dessus

### Note de performance

Le traçage ajoute une surcharge au build (journalisation des messages). Le désactiver dans les builds de production et les pipelines CI/CD sauf en cas de débogage actif de problèmes de build.

## Voir aussi

- [Dépannage de la résolution TFM](msbuild-import-order-and-tfm-resolution-fr.md) — Guide détaillé pour résoudre les conflits entre les surcharges `TargetFramework` dans le corps du projet et les défauts centralisés
- **zou/README.md** — Vue d'ensemble de l'infrastructure ZOU
- **Documentation MSBuild** — [Personnaliser votre build (Microsoft)](https://learn.microsoft.com/fr-fr/visualstudio/msbuild/customize-your-build)

---

*Ce document décrit l'architecture d'importation ZOU en date d'août 2026. Pour les dernières mises à jour, consulter l'historique des commits du dépôt.*
