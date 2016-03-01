## Introduction

Actuellement, nos projets de base (**comptabilité**, **facturation** et **salaires**) partagent certaines fonctionnalités à l'aide de sous-modules *git*. Un tel partage de code fournit pas mal d'avantages mais aussi quelques inconvénients. En effet, la paramétrisation d'un projet de sous-module peut se répercuter sur tous les projets de base qui utilisent ce sous-module.  
Imaginons par exemple que les versions minimales de Windows supportées soient *XP* pour *salaires* et *Vista* pour *facturation*. Comment paramétriser les sous-modules partagés entre salaires et facturation pour supporter ces exigences?  
Une solution serait de créer plusieurs branches *git* dans chaque sous-module, une pour *salaires* et une pour *facturation* avec un paramétrage différent des projets dans chaque branche. L'inconvénient principal de cette méthode est qu'il faut maintenir plusieurs versions des projets de *build* dans chaque branche de chaque sous-module partagé, et cela pourrait vite se transformer en cauchemar...

**Zou** a été créer pour essayer de résoudre ce genre de problèmes. Il réunit différents ***z*****ou**tils de gestion et de paramétrisation:

- il permet de centraliser certains paramètres et outils de *build*.
- il permet d'unifier, de normaliser et surtout d'instancier la paramétrisation des projets partagés.
- il suggère une certaine organisation des différents composants. 

## Le modèle logique ou la trinité *zou*

Les principaux acteurs pris en considération par *zou*, dans un ordre hiérarchique, sont le ***bundle***, la **solution** et le **projet**.  
Le *bundle* peut contenir plusieurs solutions qui chacune peut contenir plusieurs projets.  

### 1. ***Bundle***
Le *bundle* représente un arbre de développement (***bundle tree***) en tant qu'il est distinct d'autres *bundles*.  
Chaque **sous-arbre** du *bundle* (***bundle subtree***) peut être:

- soit **partagé** avec d'autres *bundles* (sous-modules *git*).
- soit **local** au *bundle* (non partagé).

Les sous-dossiers contenant une *solution* et/ou un *projet* sont des **noeuds** logiques du *bundle* (***bundle node***).  
Chaque noeud logique appartient à un sous-arbre et partage donc ses attributs (partagé ou local).

> Il est préférable de ne pas créer de fichiers dans la racine du bundle à part peut-être les fichiers communs de configuration *git*.

### 2. ***Solution***
La *solution* est un aggrégat de *projets* interdépendants. On peut le voir comme une simple collection de références à des projets.  
Le dossier de la solution peut être utilisé comme base pour mettre en commun les résultats de certains projets:

- les paquets *nuget* sont stockés dans le sous-dossier `packages`.
- la sortie des projets C++ peut être définie comme un sous-dossier de la solution (`Win32\vc140\Debug` par exemple). Cela permet de réunir les dépendences natives et d'obtenir un **dossier exécutable** .

> - le format d'une solution est un peu particulier, dans le sens qu'il est différent du [format *MSBuild*](https://msdn.microsoft.com/en-us/library/5dy88c2e.aspx).
> - la [tâche *MSBuild*](https://msdn.microsoft.com/en-us/library/z7f65y0d.aspx) ne sait pas gérer l'ordre de *build* des projets d'une solution. Seul `msbuild.exe` et *Visual Studio* le savent.

### 3. ***Project***
Le projet est l'unité de base de *MSBuild*. C'est lui qui définit les paramètres et les tâches à accomplir.

## Schéma logique
Dans l'exemple ci-dessous, les projets `S1` et `S2` sont **partagés**. Les autres solutions et projets sont **locaux**.

				Bundle[1]                       Bundle[2]
				|                               |
				|--Solution[L11]                |--Solution[L21]
				|  |--Projet[L11]               |  |--Projet[L21]
				|  |.........> Projet[S1] <.....:..|
				|  :                            |
				|                               |
				|--Solution[L12]                |--Solution[L22]
				   |--Projet[L12]                  |--Projet[L22]
				   |.........> Projet[S2] <........|
				   :

## Paramétrisation des projets

La paramétrisation *zou* des projets est basée sur les [feuilles de propriétés](https://msdn.microsoft.com/en-us/library/669zx6zc.aspx) de *MSBuild*.

> ### Importation d'une feuille de propriétés dans un projet C++
> 
> On peut importer une feuille de propriétés dans un projet C++ directement depuis Visual Studio en utilisant le gestionnaire de propriétés:
>  
> - ouvrir le gestionnaire de propriétés: *View/Other Windows/Property Manager*.
> - dans le menu contextuel du projet ou de l'une de ses configurations cliquer *Add existing property sheet...*.
> - choisir un fichier *.props* et le tour est joué.

### Principe
Le principe de base est de toujours importer des feuilles de propriétés **génériques** fournies par *zou*. Ce lien d'importation est sérialisé dans le projet et n'est plus censé changer sauf rares exceptions.  
Mais comment peut-on paramétriser des projets partagés différemment selon qu'ils son enfants d'un bundle ou d'un autre?

En fait, certaines feuilles de propriétés *intelligentes* implémentent un mécanisme de ***fallback*** sur des **feuilles de propriétés enfants** stockées dans des dossiers bien spécifiques définis par *zou*. Ces dossiers de *fallback* sont soit locaux, soit partagés et sont définis en fonction du **champ d'application désiré des feuilles de propriétés**, du plus spécifique au plus large. Si aucune feuille de propriétés enfant n'est définie, une **feuille de propriétés à défaut** est fournie par *zou*.

Reprenons l'exemple de salaires sous *XP* et de facturation sous *Vista* cité dans l'introduction. Pour résoudre le problème de la paramétrisation des versions de *NT* avec *zou*, on va importer la feuille de propriétés intelligente `zou\Cpp.NTVersion.props` dans tous les projets contenus dans facturation et salaires. C'est cette feuille de propriétés qui implémente le *fallback*.


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
