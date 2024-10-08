# [<](property-sheet-shared-overload)

# Zouification d'un projet C++

## Intégration de *zou* et ses feuilles de propriétés.
- Ouvrir le `.vcxproj` dans un éditeur.
- Au début du fichier, après le groupe `ProjectConfigurations` et avant le groupe `Globals`, insérer le code de boot de *zou* et modifier le chemin relatif (`..\zou` dans l'exemple ci-dessous) en fonction du dossier dans lequel se trouve votre projet: 
```
<Project DefaultTargets="Build" ToolsVersion="14.0" xmlns="http://schemas.microsoft.com/eveloper/msbuild/2003">
  <ItemGroup Label="ProjectConfigurations">
	...
  </ItemGroup>

  <!-- zou - extension settings -->
  <Import Project="..\zou\Cpp.Boot.props" />
  
  <PropertyGroup Label="Globals">
	...
```
- Plus bas, entre le groupe `ExtensionSettings` et le groupe `UserMacros`, remplacer les groupes d'importation `ImportGroup` par les feuilles de propriétés standard de *zou* comme ceci:
```
  ...  
  <ImportGroup Label="ExtensionSettings">
  </ImportGroup>
  
  <!-- zou - standard property sheets -->
  <ImportGroup Label="PropertySheets">
    <Import Project="$(ZouDir)Cpp.Standard.props" />
  </ImportGroup>
  
  <PropertyGroup Label="UserMacros" />
  ...
```
- A la fin du fichier, dans le groupe `ExtensionTargets`, insérer les cibles C++ de *zou*
```
  ...
  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.targets" />
  <ImportGroup Label="ExtensionTargets">
    <Import Project="$(ZouDir)Zou.Cpp.targets" />
  </ImportGroup>
</Project>
```
- Sauver le `.vcxproj`


## Ajustement des propriétés du projet.
Les feuilles de propriétés de *zou* importées dans l'étape précédente normalisent les valeurs par défaut des propriétés d'un projet. L'ajustement (nettoyage) revient à modifier les propriétés actuelles du projet en les faisant directement hériter des feuilles de propriétés standard de *zou*. Pour se faire:
- Ouvrir le projet dans Visual Studio et afficher les propriétés du projet.
- Traverser les feuilles de propriétés.

---
# [<](property-sheet-shared-overload)