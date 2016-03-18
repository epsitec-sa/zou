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

### Limitations

Les macros de configuration `Platform` et `PlatformToolset` ne peuvent pas être virtualisées par zou:

- Dans *Visual Studio*, elle sont modifiables manuellement (*toolbar*, propriétés, ...)
- Sur la ligne de commande, elles sont fournies via l'option `/p`:  
`msbuild /p:Platform=Win32;PlatformToolset=vc120_xp...`
