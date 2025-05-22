# [<](../ReadMe.md)&emsp;&emsp;[>](logic-model.md)

# Introduction

Chez Epsitec, nos projets de base (**comptabilité**, **facturation** et **salaires**) partagent certaines fonctionnalités à l'aide de sous-modules *git*. Un tel partage de code fournit pas mal d'avantages mais aussi quelques inconvénients. En effet, le paramétrage d'un projet de sous-module peut se répercuter sur tous les projets qui en dépendent.

Imaginons par exemple que les versions minimales de Windows supportées soient *XP* pour *salaires* et *Vista* pour *facturation*. Comment paramétrer les sous-modules partagés entre salaires et facturation pour supporter ces exigences?

Une solution serait de créer plusieurs branches *git* dans chaque sous-module, une pour *salaires* et une pour *facturation* avec un paramétrage différent des projets dans chaque branche. L'inconvénient principal de cette méthode est qu'il faut maintenir plusieurs versions des projets de *build* dans chaque branche de chaque sous-module partagé, et cela pourrait vite se transformer en cauchemar...

**Zou** a été créé pour résoudre ce genre de problèmes. Il réunit différents **zou**tils de gestion et de paramétrage:

- il permet de centraliser certains paramètres et outils de *build*.
- il permet d'unifier, de normaliser et surtout de virtualiser le paramétrage des projets partagés.
- il suggère une certaine organisation des différents composants.

---
# [<](../ReadMe.md)&emsp;&emsp;[>](logic-model.md)
