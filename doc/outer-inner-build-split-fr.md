---
author: Roger VUISTINER
co-author: Claude Sonnet 5 Medium
---

# Le split outer/inner build

## Objectif

Ce document explique ce qu'est le split outer/inner build du SDK .NET, à
quel moment et comment la décision de l'activer est prise, et ce que fait
chacun des deux builds.

## Pourquoi ce mécanisme existe

MSBuild, à sa base, ne connaît pas la notion "un projet, plusieurs sorties".
Le multi-ciblage (`TargetFrameworks` au pluriel — compiler le même projet
pour `net8.0` **et** `net48`, par exemple) est entièrement une construction
du SDK .NET (`Microsoft.NET.Sdk`), pas une fonctionnalité native de
MSBuild.

La solution retenue par le SDK : **réinvoquer le même fichier `.csproj`
plusieurs fois**, une fois par TFM, via la tâche MSBuild récursive
(`<MSBuild>` task), chaque invocation recevant `TargetFramework` (singulier)
en propriété globale sur sa ligne de commande. Une invocation "chapeau"
orchestre l'ensemble : c'est le split outer/inner build.

## Comment la décision est prise

Le test a lieu très tôt dans `Sdk.props`, avant même que le corps du
`.csproj` ne soit lu. La logique, simplifiée :

```
Si TargetFramework (singulier) est défini et TargetFrameworks (pluriel) est vide
  → build simple, pas de split.

Si TargetFrameworks (pluriel) est défini et TargetFramework (singulier) est vide
  → active le split outer/inner build.

Si TargetFramework ET TargetFrameworks sont tous les deux non-vides
  → cas spécial : c'est interprété comme le signal qu'on est déjà à
    l'intérieur d'un inner build (voir plus bas) ; le SDK NE relance PAS
    un nouveau split — mais si ce n'était PAS censé être un inner build,
    ce chevauchement produit un comportement incohérent (souvent
    NETSDK1013, ou une résolution TFM inattendue).
```

C'est exactement pour cette raison qu'un conflit entre un `TargetFramework`
posé dans le corps du projet et un `TargetFrameworks` posé par
`Directory.Build.props` est dangereux : le SDK voit les deux propriétés
non-vides et interprète ça comme "on est dans un inner build", ce qui
court-circuite silencieusement toute la logique normale de résolution, avec
des effets de bord difficiles à diagnostiquer (voir le document *Ordre
d'import MSBuild et résolution de TargetFramework* pour le détail de ce
scénario).

Le test précis se trouve dans les fichiers internes du SDK
(`Microsoft.NET.DefaultOutputPaths.props` / fichiers associés), sous une
forme du type :

```xml
<PropertyGroup Condition="'$(TargetFrameworks)' != '' and '$(TargetFramework)' == ''">
  <_IsOuterBuild>true</_IsOuterBuild>
  <ShouldDoBuildAsOuterBuild>true</ShouldDoBuildAsOuterBuild>
</PropertyGroup>
```

Le nom exact des propriétés internes varie selon les versions du SDK, mais
le principe reste identique : **le split est déterminé par un simple test
textuel de présence/absence sur ces deux propriétés**, évalué en tout début
de `Sdk.props` — d'où l'importance critique de l'ordre d'import documenté
par ailleurs.

## Ce que fait l'outer build

L'outer build est l'invocation MSBuild "originale", celle que l'on lance
directement (`dotnet build PROJ.csproj`). Dès qu'il détecte
`TargetFrameworks` sans `TargetFramework`, il ne compile **rien**
lui-même. Son rôle :

1. Parser la liste `TargetFrameworks` (ex. `net8.0;net48` →
   `["net8.0", "net48"]`).
2. Pour chaque TFM, invoquer une tâche MSBuild récursive en repassant sur
   **le même fichier `.csproj`**, avec la propriété globale
   `TargetFramework=net8.0` (singulier, cette fois) injectée explicitement
   sur la ligne de commande de l'invocation enfant.
3. Agréger les résultats des différentes invocations enfants — par exemple,
   pour la cible `Pack`, combiner les artefacts de chaque TFM dans un seul
   `.nupkg` multi-cible.
4. Exposer des cibles d'agrégation ou "no-op" pour les opérations qui n'ont
   de sens qu'une fois par projet, pas une fois par TFM (certaines étapes de
   packaging, par exemple).

L'outer build ne produit **aucune DLL** et n'a pas de dossier de sortie
`bin/Debug/<TFM>/` qui lui soit propre.

## Ce que fait l'inner build

Chaque inner build est une invocation MSBuild indépendante et complète du
même `.csproj`, avec `TargetFramework` (singulier) fixé sur la ligne de
commande par l'outer build. C'est exactement le chemin "ciblage simple"
normal :

1. `Sdk.props` est réévalué pour cette invocation — mais cette fois
   `TargetFramework` est non-vide, donc **aucun nouveau split** ne se
   déclenche (sinon on obtiendrait une récursion infinie).
2. Résolution des références de framework, des packages NuGet compatibles
   avec ce TFM spécifique, des imports/éléments conditionnels
   (`Condition="'$(TargetFramework)'=='net48'"`).
3. Compilation réelle (`Csc`/`Vbc`/`Fsc`), génération de la DLL/EXE dans
   `bin/Debug/net8.0/` ou `bin/Debug/net48/` selon le cas.
4. Retour des artefacts (chemins de sortie, références) à l'outer build qui
   l'a invoqué, via les métadonnées de la tâche `<MSBuild>`.

## Pourquoi une mauvaise détection casse le build

Si, à cause d'un conflit de propriétés, un projet censé être en ciblage
simple se retrouve avec `TargetFrameworks` défini (même avec une seule
valeur dedans), il bascule en mode outer/inner alors qu'il ne le devrait
pas :

- Les chemins de sortie changent de forme (`bin/Debug/net8.0/` au lieu de
  `bin/Debug/`), ce qui casse des références en dur ailleurs dans la
  solution ou dans des scripts de post-build.
- Certaines tâches censées ne s'exécuter qu'une seule fois (génération de
  fichiers, copies, signature) se comportent différemment selon qu'elles
  tournent côté outer ou côté inner — ce qui peut dupliquer du travail, ou à
  l'inverse ne rien produire si la tâche est câblée pour ne s'exécuter que
  dans un contexte de "vrai" ciblage simple.
- MSBuild lance des évaluations/process supplémentaires (un par TFM), ce qui
  ralentit le build même si un seul TFM est réellement pertinent.

## Le restore et le split outer/inner

Le `restore` ne suit pas le même mécanisme que `Build`, `Pack` ou
`Publish`. Ces cibles utilisent la tâche `<MSBuild>` récursive pour
réinvoquer le `.csproj` une fois par TFM (le split outer/inner classique
décrit plus haut). Le `restore`, lui, s'appuie sur un *dependency graph
spec* (un fichier temporaire `.dg` généré par `dotnet restore` /
`msbuild -t:Restore`), construit ainsi :

1. **Une seule évaluation "statique" du projet est lancée d'abord** (pas un
   vrai build — juste une évaluation MSBuild) pour découvrir
   `TargetFrameworks`. C'est là qu'intervient
   `RestoreUseStaticGraphEvaluation` (activé par défaut depuis le SDK 5+),
   un mode d'évaluation optimisé qui lit les propriétés/items sans exécuter
   de targets.
2. À partir de cette évaluation initiale, MSBuild sait que le projet est
   multi-cible. Il génère alors **une évaluation par TFM** du même fichier,
   avec `TargetFramework=<tfm>` fixé — exactement comme les inner builds
   classiques, sauf qu'ici le but n'est pas de compiler mais de résoudre la
   liste des `PackageReference` (versions et `Condition` incluses)
   **spécifique à ce TFM**.
3. Ces N évaluations (une par TFM) sont assemblées dans un seul graphe de
   dépendances (le fichier `.dg`), passé au moteur de résolution NuGet
   (`NuGet.Build.Tasks` / `RestoreTask`). NuGet résout alors les versions
   de packages **par TFM**, gère les conflits, applique le Central Package
   Management si actif, etc.
4. Le résultat est un **unique fichier `obj/project.assets.json`** par
   projet (pas un par TFM), mais dont le contenu interne est structuré par
   "target" — chaque TFM ayant sa propre section listant ses packages
   résolus, ses assemblies de référence, etc. `obj/<proj>.csproj.nuget.g.props`
   et `.g.targets` sont générés en conséquence.

### Implications pratiques

Le restore lit `TargetFramework`/`TargetFrameworks` exactement au même
moment critique que le `Sdk.props` d'un build normal, donc :

- Si un conflit `TargetFramework` (corps du projet) vs `TargetFrameworks`
  (`Directory.Build.props`) existe, il pollue **aussi le restore**, pas
  seulement le build. On peut se retrouver avec un `project.assets.json`
  ne contenant qu'un seul TFM résolu (ou le mauvais), ou une erreur
  `NU1201`/`NU1202` (package incompatible avec le TFM) qui semble sans
  rapport avec le souci de build — alors que la cause racine est la même
  mauvaise détection de propriétés en amont.
- Le pattern `Directory.Build.props` par dossier (documenté séparément)
  corrige donc le problème **à la source**, avant que ni le restore ni le
  build ne voient les propriétés — c'est pour ça qu'une seule correction
  résout les deux symptômes.

### Une nuance : `RestoreUseStaticGraphEvaluation`

Avec ce mode (par défaut aujourd'hui), l'évaluation initiale qui découvre
`TargetFrameworks` utilise un évaluateur MSBuild allégé
(`Microsoft.Build.Evaluation` en mode statique), sans exécuter les targets
d'un vrai build. Cela signifie que si la logique de détermination du TFM
dépend d'un **target** personnalisé (`<Target Name="...">`) plutôt que d'un
`PropertyGroup` top-level, **le restore risque de ne jamais voir cette
logique**, puisque les targets ne s'exécutent pas en mode évaluation
statique — seuls les imports et les `PropertyGroup`/`ItemGroup` évalués "à
froid" comptent. C'est une raison de plus pour garder la logique TFM
strictement déclarative (dans des fichiers importés, pas dans un `Target`),
cohérent avec le pattern documenté par ailleurs.

### Résumé

| Aspect | Build (outer/inner) | Restore |
|---|---|---|
| Mécanisme | Réinvocation récursive via tâche `<MSBuild>` | Dependency graph spec (`.dg`) + évaluations séparées par TFM |
| Nombre de sorties | Une par TFM (`bin/<tfm>/...`) | Une seule (`project.assets.json`), structurée en interne par TFM |
| Sensible au même conflit `TargetFramework`/`TargetFrameworks` ? | Oui | Oui, au même stade précoce dans `Sdk.props` |
| Sensible à la logique dans un `Target` custom ? | Oui (les targets s'exécutent) | Non fiable si `RestoreUseStaticGraphEvaluation` est actif (défaut) |

## Points à retenir

- Le split outer/inner build est une construction du SDK .NET, pas une
  fonctionnalité native de MSBuild.
- La décision se prend par un simple test de présence de propriétés
  (`TargetFramework` vs `TargetFrameworks`), évalué en tout début de
  `Sdk.props` — avant le corps du `.csproj`.
- L'outer build orchestre et agrège, sans jamais compiler directement.
- L'inner build est un rebuild complet et indépendant du même fichier
  projet, une fois par TFM, avec `TargetFramework` fixé explicitement.
- Un chevauchement accidentel de `TargetFramework` et `TargetFrameworks`
  (souvent dû à un ordre d'import mal maîtrisé) fausse cette détection et
  produit des symptômes difficiles à diagnostiquer (chemins de sortie
  inattendus, `NETSDK1013`, tâches dupliquées ou manquantes).
