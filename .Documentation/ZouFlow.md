#### [Semantic versioning](https://semver.org/)

MAJOR.MINOR.PATCH

- MAJOR: modifications incompatibles de l'API
- MINOR: ajout de fonctionnalit�s r�trocompatibles
- PATCH: corrections de bogues r�trocompatibles.

D�marrage du d�veloppement (API public instable)
- 0.1.0, puis 0.y.z

Phase de production (API public stable)
- 1.0.0, puis 1.y.z

Version prerelease
- 1.0.0-alpha, 1.0.0-beta, 1.0.0-rc

#### Git
- `commit` : repr�sente l'image, � un moment donn� du dossier contr�l� par `git`.
- `branch` : �volution dans le temps des images du dossier en question.

## Segmentation spatiale - `bundles` et modules
- le **`bundle`** se compose de **modules**.
- `bundle` = &#931; `module`

## Segmentation temporelle - historique
- l'unit� temporelle est le `commit`
- le `commit` du `bundle` aggr�ge les `commits` des sous-modules associ�s.
- c'est l'image, � un moment donn� de tout le contenu du bundle.
- distinguons les `commits` de module des `commits` de bundle:
  - commit de module: **`commit`**
  - commit de bundle: **`snapshot`**
- `module-history` = &#931; `commit`
- `bundle-history` = &#931; `snapshot`

**Fig. 1a: Repr�sentation bidimensionnelle**

| t | bundle | module1 | module2 |
|:-:|:-:|:-:|:-:
| t0 |:| `commit11` |:
| t1 |:|:| `commit21`
| t2 | `snapshot1`<br>=<br>`commit11 + commit21`|:<br>:<br>:|:<br>:<br>:|
| t3 |:| `commit12`|:
| t4 | `snapshot2`<br>=<br>`commit12 + commit21`|:<br>:<br>:|:<br>:<br>:|

**Fig. 1b: Repr�sentation bidimensionnelle condens�e**

| t | bundle | module1 | module2 |
|:-:|:-:|:-:|:-:
| t0 |:| `commit11`|:
| t1 | `snapshot1` |:| `commit21`
| t2 | `snapshot2` | `commit12`|:

**Fig. 2: Classification des `commits` et des `snapshots` - les branches**

| t | bundle || module1 || module2
||:-:|:-:|:-:|:-:|:-:|:-:
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

| t | master | wip | 0.1 | 1.0 |
|:-:|:-:|:-:|:-:
| t0 | m1 || &rArr; **v0.1-@**
| t1 | m2 &lArr;| w1|:
| t2 | m3 |:| &rArr; **v0.1.0**
| t3 | m4 |:|| &rArr; **v1.0-@**
| t4 | : |w2||:
| t5 | m6 &lArr; |w3||:
| t6 | m7 |:|:|&rArr; **v1.0.0**

### Bundle zou-flow

- les snapshot master ne contiennent que des commits master
- les snapshots versionn�s ne contiennent que des commits versionn�s
bundle.master = &#931; module.master
bundle.vbranch  = &#931; module.vbranch

