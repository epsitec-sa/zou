## TODOs
- Rename InteropProxy.* scripts  to Interop
- Rename iproxy to Interop
- Add Swissdec.prepack to Swissdec.pack.sln
- ImportFiles: rename metadata fields
	- ImportDir -> TargetDir
	- ImportFile -> TargetFile
- Fix the OutputPath error (`msbuild Tasks.pack.sln`)

## Notes

### Plateforme `Any CPU` ou `AnyCPU`.

- pour un `.sln` spécifier `Any CPU` (avec l'espace)
- pour un `.csproj` spécifier `AnyCPU`  (sans l'espace)

Lors de l'exécution du `.csproj`, c'est toujours `AnyCPU`, même si le `.csproj` est exécuté par une solution.
La plateforme est déclarée dans le `.csproj` lui-même après l'importation de `Microsoft.Common.props`.
**Zou** résoud ce problème durant l'importation de projets via la tâche `AddBuildOptions`.

### Plateformes `x86` et `Win32`
Dans un projet C++, le script `Microsoft.Cpp.Default` transforme la plateformwe `x86` en `Win32`.
Dans un projet C#, il faut utiliser la plateforme `x86`: `Win32` n'est pas supportée.

### `OutputPath` et `OutDir`
Si on exécute un .csproj via la tâche MSBuild et non l'exécutable, il faut que `OutputPath` soit spécifié.

## Packaging
On ne peut pas construire un projet et le packager dans le même script *MSBuild*. En effet, la sortie du premier projet n'est pas encore créée lorsque MSBuild évalue les éléments à packager (*MSBuild evaluation time*). Le packager va donc utiliser les fichiers du build précédent (c.à.d. aucun après un *clean* par exemple).

### C# Packaging
La plupart des projets C# ne nécessitent pas une *zouification*, c.à.d. qu'ils n'incluent pas le script `Cs.Boot.props` au début du projet. Il y a cependant une exception lorsqu'il s'agit d'un projet d'interopérabilité avec un composant natif.

Les dossiers `OutDir` et `IntDir` sont relatifs au projet. Ce qui a l'avantage de mieux encapsuler les fichiers produits. L'assemblage des composants est intrinsèque: le composant principal tire les composants dépendants dans son dossier de sortie.

#### Ancrage des composants
On va d'abord créer un projet `.prepack` qui permet de construire les composants dans un dossier d'ancrage. Une des responsabilités de ce projet est de toujours faire suivre le chemin absolu du dossier de sortie `OutDir` aux projets/solutions imbriquées. (cf `Cs.Prepack.Targets`).


Deux cas peuvent se présenter:

### 1. Le projet `.pack` est dans la même solution que le projet à packager.
Dans ce cas, le projet .pack doit on va créer une dépendence de build du .pack vers le projet principal et spécifier la source à packager.
### 2. Le projet `.pack` ne peut pas être dans la même solution que le projet à packager.


- dans ce cas là, il faut donc deux projets:
  - un .prepack qui builde le projet
  - un .pack  qui s'occupe du packaging
- il faut aussi mettre à jour les dépendences:
  - du .prepack vers le projet principal
  - du .pack vers le .prepack

Il faut aussi créer le .pack et le .prepack dans le même dossier que le projet dont on veut packager la sortie.

Deux possibilités:

 1. Le projet `.pack` est dans la même solution que les projets à packager. Il suffit de créer une dépendence du projet `.pack` vers le projet principal à packager et il faut rajouter dans le projet .pack la propriété PkgSourceDir qui pointe sur 
 2. Le projet `.pack` est dans une autre solution. Il faut alors créer un projet .prepack


## Liens externes
- [Generate msbuild file from solution](https://anubhavg.wordpress.com/2013/08/05/generate-msbuild-file-from-solution/) (MSBuildEmitSolution=1)
- [A guide to .vcxproj and .props file structure](https://blogs.msdn.microsoft.com/visualstudio/2010/05/14/a-guide-to-vcxproj-and-props-file-structure/)
- [MSBuild Property Evaluation](https://blogs.msdn.microsoft.com/aaronhallberg/2007/07/16/msbuild-property-evaluation/)
- [How To: Implementing Custom Tasks – Part I](https://blogs.msdn.microsoft.com/msbuild/2006/01/21/how-to-implementing-custom-tasks-part-i/)
- [HOW TO: Writing Custom Tasks – Part II – Types and Task Parameters](https://blogs.msdn.microsoft.com/msbuild/2006/02/02/how-to-writing-custom-tasks-part-ii-types-and-task-parameters/)
- [MSBuild Properties](https://msdn.microsoft.com/en-us/library/ms171458.aspx)
## C++ Output directories

### Default

	OutDir is treated as a local property (
	Configuration = Win32
		OutDir = $(SolutionDir)$(Configuration)\
		IntDir = $(Configuration)\

	Configuration = x64
		OutDir = $(SolutionDir)$(Platform)\$(Configuration)\
		IntDir = $(Platform)\$(Configuration)\

	// OutDir value can be relative or absolute
	TargetDir = ProjectDir\OutDir if IsRelative(OutDir)
	TargetDir =            OutDir if IsAbsolute(OutDir)
    
### *Fallback*:
- zou.Bundle.Solution.Project
- zou.Bundle.Project
- zou.Bundle.Solution
- zou.Bundle
- zou.Solution.Project
- zou.Project
- zou.Solution
- zou.default


### [*Cpp.OutDir.props*](Cpp.OutDir.props)
### [*Cpp.OutDir.Default.props*](Cpp.OutDir.Default.props)

Cette feuille de propriétés a été créée avec l'éditeur de propriétés de Visual Studio.  
Elle définit et normalise les macros ***$(OutDir)*** et ***$(IntDir)***.

    <PropertyGroup>
      <OutDir>$(SolutionDir)$(Platform)\$(PlatformToolset)\$(Configuration)\</OutDir>
      <IntDir>$(Platform)\$(PlatformToolset)\obj\$(Configuration)\$(ProjectName)</IntDir>
    </PropertyGroup>

## Opérations

Certaines opérations répétitives peuvent aussi être programmées via ces feuilles de propriétés.
Par exemple, le déploiement de *libcreact* dans un projet *C#* est implémenté dans la feuille de propriétés
***libcreact/Deploy.props***:

    <?xml version="1.0" encoding="utf-8"?>
    <Project ToolsVersion="4.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
      <PropertyGroup Label="libcreact">
	    <libcreact_OutDir>$(SolutionDir)Win32\v140_xp\$(ConfigurationName)\</libcreact_OutDir>
      </PropertyGroup>
      <PropertyGroup>
	    <PostBuildEvent>echo Deploying libcreact...
    copy "$(libcreact_OutDir)libcreact.dll" "$(TargetDir)." &gt;nul
    copy "$(libcreact_OutDir)libcreact.pdb" "$(TargetDir)." &gt;nul
		</PostBuildEvent>
      </PropertyGroup>
    </Project>


> On ne peut malheureusement pas partager les macros d'un projet *C++* avec un projet *C#* (certaines macros C++ écraseraient les macros C# existantes). Dans cet exemple on a du hardcoder la platforme et le tool set (`Win32\v140_xp`).

### Importation d'une feuille de propriétés dans un projet ***C#***

Le gestionnaire de feuilles de propriétés n'est pas disponible pour les projets C#. L'importation doit donc se faire manuellement en éditant le projet C# en question.

Par exemple, le projet *Activation.Engine.Test* utilise la DLL *libcreact* pour l'exécution des tests unitaires.

L'importation se fait comme ceci:

- ouvrir le fichier *.csproj* avec un éditeur ou directement depuis Visual Studio (*Unload project, Edit `project`.csproj*).
- insérer à la fin du fichier, juste avant le tag de fin du projet la ligne d'importation suivante:

	    	...
		    <Import Project="..\libcreact\Deploy.props" />
	    </Project>


## [Sous-modules facturation/salaires](.Documentation/Submodules.md)
