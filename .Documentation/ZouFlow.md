#### [Semantic versioning](https://semver.org/)

MAJOR.MINOR.PATCH

- MAJOR: modifications incompatibles de l'API
- MINOR: ajout de fonctionnalités rétrocompatibles
- PATCH: corrections de bogues rétrocompatibles.

Démarrage du développement (API public instable)
- 0.1.0, puis 0.y.z

Phase de production (API public stable)
- 1.0.0, puis 1.y.z

Version prerelease
- 1.0.0-alpha, 1.0.0-beta, 1.0.0-rc

#### Git
- `commit` : représente l'image, à un moment donné du dossier contrôlé par `git`.
- `branch` : évolution dans le temps des images du dossier en question.

## Segmentation spatiale - `bundles` et modules
- le **`bundle`** se compose de **modules**.
- `bundle` = &#931; `module`

## Segmentation temporelle - historique
- l'unité temporelle est le `commit`
- le `commit` du `bundle` aggrège les `commits` des sous-modules associés.
- c'est l'image, à un moment donné de tout le contenu du bundle.
- distinguons les `commits` de module des `commits` de bundle:
  - commit de module: **`commit`**
  - commit de bundle: **`snapshot`**
- `module-history` = &#931; `commit`
- `bundle-history` = &#931; `snapshot`

**Fig. 1a: Représentation bidimensionnelle**

| t | bundle | module1 | module2 |
|:-:|:-:|:-:|:-:
| t0 |:| `commit11` |:
| t1 |:|:| `commit21`
| t2 | `snapshot1`<br>=<br>`commit11 + commit21`|:<br>:<br>:|:<br>:<br>:|
| t3 |:| `commit12`|:
| t4 | `snapshot2`<br>=<br>`commit12 + commit21`|:<br>:<br>:|:<br>:<br>:|

**Fig. 1b: Représentation bidimensionnelle condensée**

| t | bundle | module1 | module2 |
|:-:|:-:|:-:|:-:
| t0 |:| `commit11`|:
| t1 | `snapshot1` |:| `commit21`
| t2 | `snapshot2` | `commit12`|:

**Fig. 2: Classification des `commits` et des `snapshots` - les branches**

| t | bundle || module1 || module2
|:-:|:-:|:-:|:-:|:-:|:-:|:-:
|| ***master*** | ***wip*** | ***master*** | ***wip1*** | ***master*** | ***wip2***
| t0 |:|:| `commit11` |:|:|:
| t1 | `snapshot1`|:|:|:| `commit21` |:
| t2 |:|:|:| *`commit12`* |:|:
| t3 |:| *`snapshot2`*|:|:|:| *`commit22`* |

# [Git flow](https://www.atlassian.com/git/tutorials/comparing-workflows/gitflow-workflow)

![git flow](git-flow.jpg)

# Zou flow

**Fig 3: git flow vs zou flow**

| git flow | zou flow
|:-:|:-:
|develop|master
|master|0.1<br>0.2<br>1.0
|feature|wip/feature
|hotfix|issue/123

**Fig. 4: module flow**

| t | module1
|:-:|:-:|:-:|:-:|:-:
| | ***master*** | ***wip*** | ***0.1*** | ***1.0***
| t0 | cm1 || &rArr; **v0.1-@**
| t1 | cm2 &lArr;| cw1|:
| t2 | cm3 |:| &rArr; **v0.1.0**
| t3 | cm4 |:|| &rArr; **v1.0-@**
| t4 | : |cw2||:
| t5 | cm6 &lArr; |cw3||:
| t6 | cm7 |:||&rArr; **v1.0.0**

**Fig. 4: bundle flow**

| t | bundle |||
|:-:|:-:|:-:|:-:|:-:
| | ***master*** | ***wip/mod1*** | ***sku/mod1/0.1*** | ***sku/mod1/1.0***
| t0 | sm1 || &rArr; **v0.1-@**
| t1 | sm2 &lArr;| sw1|:
| t2 | sm3 |:| &rArr; **v0.1.0**
| t3 | sm4 |:|| &rArr; **v1.0-@**
| t4 | : |sw2||:
| t5 | sm6 &lArr; |sw3||:
| t6 | sm7 |:||&rArr; **v1.0.0**

# Versioning

### Nomenclature
- `vbranch` : branch versionnée &rArr; **1.0**
- `vnode` : commit de départ d'une `vbranch`
- `vnode-tag`: `tag` associé à un `vnode` &rArr; **v1.0-@**
- `vtag` : `tag` de version

