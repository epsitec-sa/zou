# Git aliases

Git aliases can be enabled by running `zou/bin/git_config[.bat|.sh]`

| Alias | Parameters | Description
|:-|:-|:-
| git **bundle-dir** | [*FOLDER*] | display the parent bundle directory (see **module-id**)
| git **curbranch** | [*FOLDER*] | display the current branch 
| git **deltag** | *TAG* | delete a tag locally and remotely (see **newtag**, **mvtag**)
| git **foldtags** | *SUFFIX* | fold suffixed tags in suffixed folder (`v2.0.0-server -> server/v2.0.0`)
| git **foldotags** | *SUFFIX* | fold non `semver` tags to `other` folder (`tag -> other/tag`) (see **otags**)
| git **for** | **'** *COMMAND* **'** | execute given command in bundle and 1st level sub-modules (`git for 'git vmax'`)
| git **for-q** | **'** *COMMAND* **'** | execute given command in bundle and 1st level sub-modules -- quiet
| git **for-qr** | **'** *COMMAND* **'** | execute given command in bundle and all sub-modules --quiet -- recursive
| git **for-r** | **'** *COMMAND* **'** | execute given command in bundle and all sub-modules -- recursive
| git **issue** | *DESCR* | create an issue branch (`git issue 305-buggy` -> *issue/305-buggy**)
| git **module-id** | [*FOLDER*] | display the module ID (relative path to bundle directory) of the given module folder (see **bundle-dir**)
| git **mvbranch** | *OLD NEW* | rename a branch locally and remotely
| git **mvtag** | *OLD NEW* | rename a tag locally and remotely (see **deltag**, **newtag**)
| git **newtag** | *TAG* | create a local tag and push it on origin (see **deltag**, **mvtag**)
| git **prunetags** || prune local tags no longer present on origin
| git **oprune** || prune tracking branches no longer present on origin
| git **otags** || display non `semver` tags (see **foldotags**)
| git **sclean** || remove all untracked files
| git **sdiff** ||
| git **sfor** | **'** *COMMAND* **'** | execute given command in 1st level sub-modules
| git **sfor-q** | **'** *COMMAND* **'** | execute given command in 1st level sub-modules -- quiet
| git **sfor-qr** | **'** *COMMAND* **'** | execute given command in all sub-modules --quiet -- recursive
| git **sfor-r** | **'** *COMMAND* **'** | execute given command in all sub-modules -- recursive
| git **smaster** || update master snapshot by updating (checkout + pull) master branches of registered submodules
| git **spull** ||
| git **spush** ||
| git **supdate** || for each registered submodules, clone missing ones and update their working trees
| git **tag2hash** | *TAG* | display short hash of given tag
| git **tags** || list local and remote tags
| git **vbranch** | *MAJOR.MINOR* | create a `vbranch` and a `vnode` (*1.0 <- v1.0-@*)
| git **vcheckout** | [*HASH*] | checkout the `vbranch` of the given `vcommit`
| git **vcommit2major** | [*HASH*] | display the most recent `MAJOR` version reachable from given `vcommit`
| git **vcommit2minor** | [*HASH*] | display the most recent `MAJOR.MINOR` version reachable from given `vcommit`
| git **vcommit2tag** | [*HASH*] | display the most recent `vtag` description (1) reachable from given `vcommit`
| git **vmajor** | [*HASH*] | checkout the `vbranch` having the same `MAJOR` and greatest `MINOR` version as the given `vcommit`
| git **vmax** | *[MAJOR[.MINOR]]* | display maximum version using given pattern
| git **vminor** | [*HASH*] | checkout the `vbranch` having the same `MAJOR.MINOR` version as the given `vcommit`
| git **vmin** | *[MAJOR[.MINOR]]* | display minimum version using given pattern
| git **vnext** | [*HASH*] | checkout the `vbranch` with the greatest `MAJOR` and `MINOR` version
| git **vtable** || create a markdown table of sub-module versions (`versions.md`)
| git **vtags** || list local `vtag`s

(1) `vtag` description exemple : `v1.0.0-2-gc8af267` (see [git describe](https://git-scm.com/docs/git-describe))
