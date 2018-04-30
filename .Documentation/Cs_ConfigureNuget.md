# Configuration de NuGet

Lorsqu'une solution ne contient aucun projet _zouifié_, il faut ajouter
manuellement le fichier de configuration `NuGet.config` dans le dossier
racine du bundle.

* Ajouter `zou` en tant que sous module dans le bundle `foo-dev`.
* Copier `zou\.Templates\nuget\NuGet.config` à la racine de `foo-dev`.
