## UNDER CONSTRUCTION

### *Fallback*:
- zou.Bundle.Solution.Project
- zou.Bundle.Project
- zou.Bundle.Solution
- zou.Bundle
- zou.Solution.Project
- zou.Project
- zou.Solution
- zou.default


### [*Cpp.NTVersion.props*](Cpp.NTVersion.props)
### [*Cpp.NTVersion.default.props*](Cpp.NTVersion.default.props)
### [*Cpp.NTVersion.Vista.props*](Cpp.NTVersion.Vista.props)

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
