# [<](property-sheet-overload.md)&emsp;&emsp;[>](vcxproj-zouification.md)

# Surcharge *partagée* d'une feuille de propriétés - *en construction*.
Il est possible de surcharger une feuille de propriétés qui est partagée par plusieurs *bundles*.
Il suffit pour cela que cette feuille soit créée dans un dossier de configuration *zou*
lui-même partagé via un sous-module *git*.
Il ne peut bien entendu s'agir que d'une configuration liée à une **solution** ou à un **projet**,
puisque la configuration d'un *bundle* est par définition *locale* à ce *bundle*.

### Exemple:

Le sous-module *libsalaires* du *bundle* *salaires* doit être compilé pour la version *Vista* alors que le reste du *bundle* est compilé pour *XP* (version par défaut de *zou*). Dans ce cas particulier, le dossier de configuration zou

Etant donné que libsalaires est un sous-module git, il est potentiellement partagé

---
# [<](property-sheet-overload.md)&emsp;&emsp;[>](vcxproj-zouification.md)