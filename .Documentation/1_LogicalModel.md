# Le modèle logique ou la trinité *zou*

Les principaux acteurs pris en considération par *zou*, dans un ordre hiérarchique, sont le ***bundle***, la **solution** et le **projet**.  
Le *bundle* peut contenir plusieurs solutions qui chacune peut contenir plusieurs projets.

## 1. ***Bundle***
Le *bundle* représente un arbre de développement (***bundle tree***) en tant qu'il est distinct d'autres *bundles*.  
Chaque **sous-arbre** du *bundle* (***bundle subtree***) peut être:

- soit **partagé** avec d'autres *bundles*. Dans ce document, le mot *partagé* signifie *partagé via un sous-module git*  
*(il n'est réellement partagé que lorsque les sous-modules respectifs sont positionnés sur le même **commit***).
- soit **local** au *bundle* (non partagé).

Les sous-dossiers contenant une *solution* et/ou un *projet* sont des **noeuds** logiques du *bundle* (***bundle node***).  
Chaque noeud logique appartient à un sous-arbre et partage donc ses attributs (partagé ou local).

> Il est préférable de ne pas créer de fichiers dans la racine du bundle à part peut-être les fichiers communs de configuration *git*.

## 2. *Solution*
La *solution* associe et combine les configurations et plateformes de plusieurs *projets*. *Visual Studio* nous en donne une bonne représentation avec son `Solution Explorer` et son `Configuration Manager`.

Le dossier de la solution peut être utilisé comme base pour mettre en commun les résultats de certains projets:

- les paquets *nuget* sont stockés dans le sous-dossier `packages`.
- la sortie des projets C++ peut être définie comme un sous-dossier de la solution (`Win32\vc140\Debug` par exemple). Cela permet de réunir les dépendences natives et d'obtenir un **dossier exécutable** .

> - le format d'une solution est un peu particulier, dans le sens qu'il est différent du [format *MSBuild*](https://msdn.microsoft.com/en-us/library/5dy88c2e.aspx). Il est cependant possible de générer un [fichier au format MSBuild à partir d'une solution](http://www.codeproject.com/Tips/177770/Creating-MSBuild-projects-from-sln-files): *cette astuce peut s'avérer utile dans le cas par exemple ou l'on veut exécuter un projet particulier (une cible) situé dans un sous-dossier de la solution grâce à l'option `/target` de MSBuild*.
> - la [tâche *MSBuild*](https://msdn.microsoft.com/en-us/library/z7f65y0d.aspx) ne sait pas gérer l'ordre de *build* des projets d'une solution. Seul `msbuild.exe` et *Visual Studio* le savent.

## 3. *Projet*
Le projet est l'unité de base de *MSBuild*. C'est lui qui définit les paramètres et les tâches à accomplir. Il est composé d'un module principal (.vcxproj, .csproj, ...) et de sous-modules (.props, .targets) dont [l'importation](https://msdn.microsoft.com/en-us/library/92x05xfs.aspx) peut être contrôlée par des [conditions](https://msdn.microsoft.com/en-us/library/7szfhaft.aspx). Le module principal d'un projet est soumis à une [structure particulière](https://blogs.msdn.microsoft.com/visualstudio/2010/05/14/a-guide-to-vcxproj-and-props-file-structure/) liée au **modèle d'évaluation séquentiel** de MSBuild. 

# Schéma logique *bundle*-solution-projet
Dans l'exemple ci-dessous, les projets `S1` et `S2` sont **partagés**. Les autres solutions et projets sont **locaux**.

				Bundle[1]                       Bundle[2]
				|                               |
				|--Solution[L11]                |--Solution[L21]
				|  |--Projet[L11]               |  |--Projet[L21]
				|  |.........> Projet[S1] <.....:..|
				|  :                            |-
				|                               |
				|--Solution[L12]                |--Solution[L22]
				   |--Projet[L12]                  |--Projet[L22]
				   |.........> Projet[S2] <........|
				   :

# Contexte d'exécution
Comme on peut le voir dans le schéma logique précédent, un projet peut être partagé par plusieurs solutions et par plusieurs *bundles*. Le **contexte d'exécution** ***zou*** est défini par les propriétés `ZouDir`, `BundleDir`, `SolutionDir` et `ProjectDir` qui permettent de paramétrer et de relativiser l'exécution de chaque projet.

> - la variable `ZouDir` pointe sur le dossier du sous-module *zou* et vaut `$(BundleDir)zou\`.
> - la variable `BundleDir` est définie par *zou* ([private/zou.targets](private/zou.targets)).
> - la propriété `SolutionDir` n'est pas définie lorsque l'on exécute un projet. Dans ce cas sa valeur est fixée par *MSBuild* et vaut `'*Undefined*'`.

# Contexte de configuration
Similairement au *contexte d'exécution*, le **contexte de configuration** ***zou*** définit les propriétés `ZouBundleDir`, `ZouSolutionDir` et `ZouProjectDir` qui pointent sur des sous-dossiers de configuration nommés `.zou` et situés respectivement dans les dossiers `BundleDir`, `SolutionDir` et `ProjectDir`. Ces dossiers de configuration vont nous permettre de stocker des informations relatives soit au projet.

> - les variables `ZouBundleDir`, `ZouSolutionDir` et `ZouProjectDir` sont définies par *zou* ([private/zou.targets](private/zou.targets)).
> - la propriété `ZouSolutionDir` n'est pas initialisée lorsque l'on exécute un projet. Dans ce cas sa valeur est `''`.
