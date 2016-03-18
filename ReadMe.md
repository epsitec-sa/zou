## Introduction

Actuellement, nos projets de base (**comptabilité**, **facturation** et **salaires**) partagent certaines fonctionnalités à l'aide de sous-modules *git*. Un tel partage de code fournit pas mal d'avantages mais aussi quelques inconvénients. En effet, la paramétrisation d'un projet de sous-module peut se répercuter sur tous les projets de base qui utilisent ce sous-module.

Imaginons par exemple que les versions minimales de Windows supportées soient *XP* pour *salaires* et *Vista* pour *facturation*. Comment paramétriser les sous-modules partagés entre salaires et facturation pour supporter ces exigences?

Une solution serait de créer plusieurs branches *git* dans chaque sous-module, une pour *salaires* et une pour *facturation* avec un paramétrage différent des projets dans chaque branche. L'inconvénient principal de cette méthode est qu'il faut maintenir plusieurs versions des projets de *build* dans chaque branche de chaque sous-module partagé, et cela pourrait vite se transformer en cauchemar...

**Zou** a été créé pour essayer de résoudre ce genre de problèmes. Il réunit différents ***z*****ou**tils de gestion et de paramétrisation:

- il permet de centraliser certains paramètres et outils de *build*.
- il permet d'unifier, de normaliser et surtout de virtualiser la paramétrisation des projets partagés.
- il suggère une certaine organisation des différents composants. 

## Le modèle logique ou la trinité *zou*

Les principaux acteurs pris en considération par *zou*, dans un ordre hiérarchique, sont le ***bundle***, la **solution** et le **projet**.  
Le *bundle* peut contenir plusieurs solutions qui chacune peut contenir plusieurs projets.  

### 1. ***Bundle***
Le *bundle* représente un arbre de développement (***bundle tree***) en tant qu'il est distinct d'autres *bundles*.  
Chaque **sous-arbre** du *bundle* (***bundle subtree***) peut être:

- soit **partagé** avec d'autres *bundle*s. Dans ce document, le mot '*partagé*' signifie *partagé via un sous-module git*  
*(il n'est réellement partagé que lorsque les sous-modules respectifs sont positionnés sur le même **commit***).
- soit **local** au *bundle* (non partagé).

Les sous-dossiers contenant une *solution* et/ou un *projet* sont des **noeuds** logiques du *bundle* (***bundle node***).  
Chaque noeud logique appartient à un sous-arbre et partage donc ses attributs (partagé ou local).

> Il est préférable de ne pas créer de fichiers dans la racine du bundle à part peut-être les fichiers communs de configuration *git*.

### 2. ***Solution***
La *solution* associe et combine les configurations et plateformes de plusieurs *projets*. *Visual Studio* nous en donne une bonne représentation avec son `Solution Explorer` et son `Configuration Manager`. 
  
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

## Paramétrisation des projets C++

La paramétrisation des projets C++ est basée sur les [feuilles de propriétés](https://msdn.microsoft.com/en-us/library/669zx6zc.aspx) de *MSBuild*.

> #### Importation d'une feuille de propriétés dans un projet C++
> 
> On peut importer une feuille de propriétés dans un projet C++ directement depuis Visual Studio en utilisant le gestionnaire de propriétés:
>  
> - activer le gestionnaire de propriétés - `View/Other Windows/Property Manager`.
> - dans le menu contextuel du projet ou de l'une de ses configurations cliquer `Add existing property sheet...`.
> - choisir un fichier `.props` et le tour est joué.

> #### Génération d'une feuille de propriétés
> On peut aussi générer des feuilles de propriétés depuis *Visual Studio*. Comme exemple, nous allons en créer une qui ajoute la macro `_WIN32_WINNT=0x501` au preprocesseur *C++*.
> 
> - créer un projet C++ quelconque ou ouvrez un projet existant (on ne sauvera pas les modifications).
> - activer le gestionnaire de propriétés - `View/Other Windows/Property Manager`.
> - afficher le menu contextuel de votre projet et aller dans `Add New Property Sheet...`.
> - nommer votre feuille de propriétés `NTVersion.XP.props` puis presser `OK`.
> - aller dans dans le champ `(Name)` de l'explorateur de propriétés et remplacer la valeur actuelle par `NT Version = XP`.
> - afficher le menu contextuel de la feuille de propriétés et aller dans `Properties...`.
> - activer l'onglet `C/C++` puis `Preprocessor`.
> - dans le champ `Preprocessor definitions:` insérer `_WIN32_WINNT=0x501;` puis presser `OK`.
> - sauver la feuille de propriétés en pressant `Ctrl+S`.
> 
> Si on visualise le fichier généré (`NTVersion.XP.props`), on devrait voir ceci:
> 
> 	<?xml version="1.0" encoding="utf-8"?>
> 	<Project ToolsVersion="4.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
> 	  <ImportGroup Label="PropertySheets" />
> 	  <PropertyGroup Label="UserMacros" />
> 	  <PropertyGroup>
> 	    <_PropertySheetDisplayName>NT Version = XP</_PropertySheetDisplayName>
> 	  </PropertyGroup>
>  	  <ItemDefinitionGroup>
> 	    <ClCompile>
> 	      <PreprocessorDefinitions>_WIN32_WINNT=0x501;%(PreprocessorDefinitions)</PreprocessorDefinitions>
> 	    </ClCompile>
> 	  </ItemDefinitionGroup>
> 	  <ItemGroup />
> 	</Project>
> 
> Lors de cette opération, *Visual Studio* a aussi modifié le fichier du projet pour y stocker le lien vers cette feuille de propriétés:
> 
>		...
>		<ImportGroup Label="PropertySheets" >
>		  <Import Project="NTVersion.XP.props" />
>		</ImportGroup>
>		...
> 
> Le label `PropertySheets` permet au gestionnaire de propriétés de rassembler et de hiérarchiser les feuilles de propriétés liées au projet.


## Paramétrisation virtuelle

La question à laquelle il nous faut répondre, c'est comment paramétriser différemment des projets *partagés* selon qu'ils son enfants d'un `bundle` ou d'un autre? Ou plus précisément, comment virtualiser une feuille de propriétés en fonction du *bundle* auquel elle appartient?

### Feuille de propriétés *virtuelle*

Comme on l'a vu précédemment il est possible de **partager** une feuille de propriétés entre différents projets. Pour cela, il suffit de la stocker dans un dossier  auquel tous les projets ont accès. Ce dossier, c'est *zou*, en tant que sous-module git du *bundle*.

Voici le code de la feuille de propriétés virtuelle [zou/Cpp.NTVersion.props](Cpp.NTVersion.props) dans laquelle on a omis quelques sections pour faire ressortir les similitudes avec le fichier `NTVersion.XP.props` généré dans l'exemple précédent.

	<?xml version="1.0" encoding="utf-8"?>
	<Project ToolsVersion="4.0" ... xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
	  ...
	  <ImportGroup Label="PropertySheets">
	    ...
	  </ImportGroup>
	  <ItemDefinitionGroup>
	    <ClCompile>
	      <PreprocessorDefinitions>_WIN32_WINNT=$(NTVersion);%(PreprocessorDefinitions)</PreprocessorDefinitions>
	    </ClCompile>
	  </ItemDefinitionGroup>
	  ...
	</Project>

#### Mécanisme de *fallback*

Voici le code qui gère le mécanisme de *fallback*:

		...
	(1) <Import Condition="'$(Zou)' == ''" Project="private\zou.targets" />
		...
	(2) <ImportGroup Label="PropertySheets">
		  <Import Project="$(ZouBundleDir)**\$(MSBuildThisFile)"   Condition="'$(NTVersion)' == '' And '$(ZouBundleDir)'   != ''" />
		  <Import Project="$(ZouSolutionDir)**\$(MSBuildThisFile)" Condition="'$(NTVersion)' == '' And '$(ZouSolutionDir)' != '' And '$(ZouSolutionDir)' != '$(ZouBundleDir)'" />
		  <Import Project="$(ZouProjectDir)**\$(MSBuildThisFile)"  Condition="'$(NTVersion)' == '' And '$(ZouProjectDir)'  != '' And '$(ZouProjectDir)'  != '$(ZouSolutionDir)' And '$(ZouProjectDir)' != '$(ZouBundleDir)'" />
	(3)   <Import Project="$(ZouDir)$(MSBuildThisFileName).Default$(MSBuildThisFileExtension)" Condition="'$(NTVersion)' == ''" />
		</ImportGroup>
		...
	(4)	    <PreprocessorDefinitions>_WIN32_WINNT=$(NTVersion);%(PreprocessorDefinitions)</PreprocessorDefinitions>
		...

1. Le script `private/zou.targets` `(1)` définit, entre autres, les variables globales suivantes:

		`$(BundleDir)`                              : dossier *racine* du *bundle*.
		`$(ZouDir)         = $(BundleDir)zou\`      : dossier *zou*    du *bundle*, *partagé* (*au sens git du terme*) par les bundles.
		`$(ZouBundleDir)   = $(BundleDir)zou.cfg\`  : dossier de configuration du *bundle*, *local* au *bundle*.
		`$(ZouSolutionDir) = $(SolutionDir)zou.cfg\`: dossier de configuration de la solution, *partagé* ou *local*.
		`$(ZouProjectDir)  = $(ProjectDir)zou.cfg\` : dossier de configuration du projet, *partagé* ou *local*.

2. Le code qui suit `(2)` importe différents scripts dans un ordre particulier (*bundle*, solution, projet). L'importation se termine dès que la condition `'$(NTVersion)' == ''` est satisfaite, c.à.d. dès que la variable `NTVersion` est **surchargée**.
3. Si après l'étape précédente, la variable `NTVersion` n'est toujours pas initialisée `(3)`, on importe la **feuille de propriétés par défaut** `$(ZouDir)$(MSBuildThisFileName).Default$(MSBuildThisFileExtension)` fournie par *zou* - dans notre exemple il s'agit du script [zou/Cpp.NTVersion.**Default**.props](Cpp.NTVersion.Default.props) dont le code est le suivant:

		<Project ToolsVersion="4.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
		  <PropertyGroup>
		    <_PropertySheetDisplayName>zou - NT Version = XP</_PropertySheetDisplayName>
		  </PropertyGroup>
		  
		  <PropertyGroup>
		    <NTVersion Condition="'$(NTVersion)' == ''">0x501</NTVersion>
		  </PropertyGroup>
		</Project>

4. Finalement, on applique la fonctionnalité désirée `(4)`. Ici, on modifie les métadonnées du compilateur C++ pour tous les éléments `ClCompile`, c.à.d. tous les fichiers `.cpp` concernés.

#### Surcharge d'une feuille de propriétés

Reprenons l'exemple de *salaires* sous *XP* et de *facturation* sous *Vista* cité dans l'introduction. Le problème revient à paramétrer différemment la macro *C++* `_WIN32_WINNT` en fonction du *bundle* choisi:

- `_WIN32_WINNT=0x501` pour *salaires*.
- `_WIN32_WINNT=0x600` pour *facturation*.

Les étapes sont les suivantes:

1. Importer (à l'aide du gestionnaire de propriétés de *Visual Studio*) la feuille de propriétés virtuelle [zou/Cpp.NTVersion.props](Cpp.NTVersion.props) dans tous les projets des solutions facturation et salaires.
2. Etant donné que la version de *NT* par défaut définie par *zou* est *XP*, il suffit de surcharger cette valeur uniquement dans le `bundle` *facturation*, puisque *salaires* utilise la version *XP*. Pour ce faire, on va:  
	- créer un sous-dossier `zou.cfg` dans la racine du `bundle` *facturation*.
	- copier la feuille de propriétés [zou/Cpp.NTVersion.Default.props](Cpp.NTVersion.Default.props) dans le dossier `zou.cfg` que l'on vient de créer et la renommer en `Cpp.NTVersion.props`.
	- éditer et modifier la valeur de `NTVersion` à `0x0600`

###### Surcharge de la version NT dans le bundle facturation [zou.cfg/Cpp.NTVersion.props](https://git.epsitec.ch/cresus-suite/fact/blob/master/zou.cfg/Cpp.NTVersion.props).

		<?xml version="1.0" encoding="utf-8"?>
		<Project ToolsVersion="4.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
		  <PropertyGroup>
		    <_PropertySheetDisplayName>fact - NT Version = Vista</_PropertySheetDisplayName>
		  </PropertyGroup>
		  
		  <PropertyGroup Condition="'$(NTVersion)' == ''">
		    <NTVersion>0x0600</NTVersion>
		  </PropertyGroup>
		</Project>


Et voici ce que ça donne dans le gestionnaire de propriétés de *Visual Studio* pour le sous-module [libefx](https://git.epsitec.ch/cresus-suite/libefx):

1. Dans *salaires* (pas de surcharge):    
![](.Documentation/PropSheet_SalEfxNTVersion.png)  
  
1. Dans *facturation* (surcharge Vista):  
![](.Documentation/PropSheet_FactEfxNTVersion.png)

###### Résumé

- Dans les deux cas, la première feuille de propriétés - **zou - NT Version...** - est la feuille générique fournie par *zou* qui implémente le *fallback*: [zou/Cpp.NTVersion.props](Cpp.NTVersion.props). Par convention, le titre d'une feuille de propriétés *virtuelle* se termine par `...`. 

- Pour *salaires*, la feuille enfant - **zou - NT Version = XP** -  est le défaut fournie par *zou*: [zou/Cpp.NTVersion.Default.props](Cpp.NTVersion.Default.props).
- Pour *facturation*, la feuille enfant - **fact - NT Version = Vista** - est la surcharge stockée localement dans `fact/zou.cfg/Cpp.NTVersion.props`.

-------------

### Limitations

Les macros de configuration suivantes ne peuvent pas être virtualisées par zou:

- PlatformToolset
- Platform

Visual Studio -> propriétés du projet.
MSBuild -> msbuild /p:Platform=Win32;PlatformToolset=vc120_xp

-------------

### Liens externes
https://anubhavg.wordpress.com/2013/08/05/generate-msbuild-file-from-solution/
MSBuildEmitSolution=1