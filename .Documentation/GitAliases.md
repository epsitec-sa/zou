# Git aliases

Git aliases can be enabled by running `zou/bin/git_config.{bat|sh}` (see [git_config.bat](../bin/git_config.bat), [git_config.sh](../bin/git_config.sh))

| Alias | Parameters | Description | Comment/Exemple
|:-|:-|:-
| git **bundle-dir** | *[FOLDER]* | display the parent bundle directory (see **module-id**) | `git bundle-dir /s/devel/cresus-dev/zou`<br>*=> /s/devel/cresus-dev*
| git **curbranch** | *[FOLDER]* | display the active branch in the given folder
| git **deltag** | *TAG* | delete a tag locally and remotely (see **newtag**, **mvtag**) | `git deltag v1.0.0`
| git **foldtags** | *SUFFIX* | fold suffixed tags in suffixed folder | `git foldtags server`<br>*=> v2.0.0-server -> server/v2.0.0*
| git **foldotags** |  | fold non `semver` tags to `other` folder (see **otags**) | `git foldotags`<br>*=> tag -> other/tag*
| git **for** | *COMMAND* | execute given command in bundle and 1st level sub-modules | `git for git vmax`
| git **for-q** | *COMMAND* | execute given command in bundle and 1st level sub-modules -- quiet
| git **for-qr** | *COMMAND* | execute given command in bundle and all sub-modules --quiet -- recursive
| git **for-r** | *COMMAND* | execute given command in bundle and all sub-modules -- recursive
| git **issue** | *DESCR* | create an issue branch | `git issue 305-buggy`<br>*=> issue/305-buggy*
| git **module-id** | *[FOLDER]* | display the module ID (relative path to bundle directory) of the given module folder (see **bundle-dir**) | `git module-id /s/devel/cresus-dev/zou/bin`<br>*=> zou*
| git **mvbranch** | *OLD NEW* | rename a branch locally and remotely
| git **mvtag** | *OLD NEW* | rename a tag locally and remotely (see **deltag**, **newtag**)
| git **newtag** | *TAG[ ...]* | create a local annotated tag and push it on origin (see **deltag**, **mvtag**) | `git newtag v1.0.0`<br>`git newtag v1.0.1 -m"Demo"`
| git **prunetags** || prune local tags no longer present on origin
| git **oprune** || prune tracking branches no longer present on origin
| git **otags** || display non `semver` tags excluding folded tags (see **foldotags**)
| git **sclean** || remove all untracked files (see **zclean**) | ~ `git for git zclean`
| git **sdiff** ||
| git **sfor** | *COMMAND* | execute given command in 1st level sub-modules
| git **sfor-q** | *COMMAND* | execute given command in 1st level sub-modules -- quiet
| git **sfor-qr** | *COMMAND* | execute given command in all sub-modules --quiet -- recursive
| git **sfor-r** | *COMMAND* | execute given command in all sub-modules -- recursive
| git **smaster** || update master snapshot by updating master branches of registered submodules (see **master**) | ~ `git for git master`
| git **spull** ||
| git **spush** ||
| git **supdate** || for each registered submodules, clone missing ones and update their working trees
| git **tag2hash** | *TAG* | display short hash of given tag
| git **tags** || list local and remote tags
| git **vbranch** | *MAJOR.MINOR[ COMMIT]* | create a `vbranch` and a `vnode` | `git vbranch 0.1`<br>`git vbranch 0.1 cc1aa32`<br>*=> 1.0 <- v1.0-@*
| git **vcheckout** | *[VCOMMIT]* | checkout the `vbranch` of the given `vcommit`
| git **vcommit2major** | *[VCOMMIT]* | display the most recent `MAJOR` version reachable from given `vcommit`
| git **vcommit2minor** | *[VCOMMIT]* | display the most recent `MAJOR.MINOR` version reachable from given `vcommit`
| git **vcommit2tag** | *[VCOMMIT]* | display the most recent `vtag` description (1) reachable from given `vcommit`
| git **vmajor** | *[VCOMMIT]* | checkout the `vbranch` having the same `MAJOR` and greatest `MINOR` version as the given `vcommit`
| git **vmax** | *[MAJOR[.MINOR]]* | display maximum version using given pattern
| git **vminor** | *[VCOMMIT]* | checkout the `vbranch` having the same `MAJOR.MINOR` version as the given `vcommit`
| git **vmin** | *[MAJOR[.MINOR]]* | display minimum version using given pattern
| git **vnext** | *[VCOMMIT]* | checkout the `vbranch` with the greatest `MAJOR` and `MINOR` version
| git **vtable** || create a markdown table of sub-module versions (`versions.md`)
| git **vtags** || list local `vtag`s
| git **zclean** || remove all untracked files and `node_modules` folder except `packages` folder in current repo/submodule
| git **zmaster** || checkout and pull master branch (see **smaster**)
| git **ztags** || zouify tags (2)


(1) git **vcommit2tag** notes:

`vtag` description exemple : `v1.0.0-2-gc8af267` (see [git describe](https://git-scm.com/docs/git-describe))

(2) git **ztags** processing (see [zou-flow](ZouFlow.md)):
- move non [SemVer](https://semver.org/) tags to `other` folder (rename tag with `other/` prefix)
- group tags with the same commit hash (detect commit with multiple tags)
- delete `other` folder's tags that have a sibling in the commit hash group (delete redondant tags from `other` folder)
