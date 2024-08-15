# [<](property-sheets)&emsp;&emsp;[>](property-sheet-overload)

# Feuilles de propriétés *virtuelles*

La question à laquelle il nous faut répondre, c'est comment paramétrer
différemment des projets *partagés* selon qu'ils son enfants d'un `bundle` ou
d'un autre? Ou plus précisément, comment virtualiser une feuille de propriétés
en fonction du *bundle* auquel elle appartient?

Comme on l'a vu précédemment il est possible de **partager** une feuille de
propriétés entre différents projets. Pour cela, il suffit de la stocker dans un
dossier  auquel tous les projets ont accès. Ce dossier, c'est *zou*, en tant que
sous-module git du *bundle* auquel il est attaché.

Voici le code de la feuille de propriétés virtuelle
[zou/Cpp.NTVersion.props](../Cpp.NTVersion.props) dans laquelle on a omis quelques
sections pour faire ressortir les similitudes avec le fichier
`NTVersion.XP.props` généré dans [l'exemple précédent](property-sheets.md).

	<?xml version="1.0" encoding="utf-8"?>
	<Project ToolsVersion="4.0" ... xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
	  ...
	  <PropertyGroup>
		<_PropertySheetDisplayName>zou - NT Version...</_PropertySheetDisplayName>
	  </PropertyGroup>
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

## Mécanisme de *fallback*

Le mécanisme de fallback visite les dossiers de configuration *zou* (`zou.cfg`) existants dans l'ordre suivant:

- dans le dossier racine du `bundle`.
- dans le même dossier que le fichier de la solution.
- dans le même dossier que le fichier du projet.

Les dossiers qui se se chevauchent ne sont pas revisités. Voici le code qui gère ce mécanisme:

		...
	(1) <Import Condition="'$(Zou)' == ''" Project="zou.props" />
		...
	(2) <ImportGroup Label="PropertySheets">
		  <Import Project="$(ZouBundleDir)**\$(MSBuildThisFile)"   Condition="'$(NTVersion)' == '' And '$(ZouBundleDir)'   != ''" />
		  <Import Project="$(ZouSolutionDir)**\$(MSBuildThisFile)" Condition="'$(NTVersion)' == '' And '$(ZouSolutionDir)' != '' And '$(ZouSolutionDir)' != '$(ZouBundleDir)'" />
		  <Import Project="$(ZouProjectDir)**\$(MSBuildThisFile)"  Condition="'$(NTVersion)' == '' And '$(ZouProjectDir)'  != '' And '$(ZouProjectDir)'  != '$(ZouSolutionDir)' And '$(ZouProjectDir)' != '$(ZouBundleDir)'" />
	(3)   <Import Project="$(ZouPrivateDir)$(MSBuildThisFileName).Default$(MSBuildThisFileExtension)" Condition="'$(NTVersion)' == ''" />
		</ImportGroup>
		...
	(4)	    <PreprocessorDefinitions>_WIN32_WINNT=$(NTVersion);%(PreprocessorDefinitions)</PreprocessorDefinitions>
		...

1. Le script [zou.props](../zou.props) `(1)` définit, entre autres, les variables
globales suivantes:

		$(BundleDir)                               : dossier racine du bundle.
		$(ZouDir)         = $(BundleDir)zou\       : dossier zou    du bundle, partagé (au sens git du terme) par les bundles.
		$(ZouBundleDir)   = $(BundleDir)zou.cfg\   : dossier de configuration du bundle, local au bundle.
		$(ZouSolutionDir) = $(SolutionDir)zou.cfg\ : dossier de configuration de la solution, partagé ou local.
		$(ZouProjectDir)  = $(ProjectDir)zou.cfg\  : dossier de configuration du projet, partagé ou local.

2. Le code qui suit `(2)` importe différents scripts dans un ordre particulier
(*bundle*, solution, projet). L'importation se termine dès que la condition
`'$(NTVersion)' == ''` est satisfaite, c.à.d. dès que la variable `NTVersion`
est **surchargée**.
3. Si après l'étape précédente, la variable `NTVersion` n'est toujours pas
initialisée `(3)`, on importe la **feuille de propriétés par défaut**
`$(ZouPrivateDir)$(MSBuildThisFileName).Default$(MSBuildThisFileExtension)` - dans notre exemple il s'agit du script
[zou/private/Cpp.NTVersion.**Default**.props](../private/Cpp.NTVersion.Default.props) dont le code
est le suivant:
```
		<Project ToolsVersion="4.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
		  <PropertyGroup>
		    <_PropertySheetDisplayName>zou - NT Version = XP</_PropertySheetDisplayName>
		  </PropertyGroup>

		  <PropertyGroup>
		    <NTVersion>0x501</NTVersion>
		  </PropertyGroup>
		</Project>
```
4. Finalement, on applique la fonctionnalité désirée `(4)`. Ici, on modifie les
métadonnées du compilateur C++ pour tous les éléments `ClCompile`, c.à.d. tous
les fichiers `.cpp` concernés.

## Limitations

La macro de configuration `Platform` ne peut pas être virtualisées par zou:

- Dans *Visual Studio*, elle est modifiable manuellement (*toolbar*, propriétés, ...)
- Sur la ligne de commande, elles sont fournies via l'option `/p` de `msbuild`:  
`msbuild /p:Platform=Win32;PlatformToolset=vc120_xp...`

---
# [<](property-sheets)&emsp;&emsp;[>](property-sheet-overload)