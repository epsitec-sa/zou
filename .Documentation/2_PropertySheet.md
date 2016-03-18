## Feuilles de propriétés

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