# Git aliases

Git aliases can be enabled by running `zou/bin/git_config[.bat|.sh]`

| Alias | Parameters | Description
|:-|:-|:-
| git **oprune** || prune tracking branches no longer present on origin
| git **sdiff** ||
| git **spull** ||
| git **spush** ||
| git **sclean** || remove all untracked files
| git **supdate** || for each registered submodules, clone missing ones and update their working trees
| git **smaster** || update master snapshot by updating (checkout + pull) master branches of registered submodules
| git **issue** | *DESCR* | create an issue branch (`git issue 305-buggy` -> *issue/305-buggy**)
| git **mvbranch** | *OLD NEW* | rename a branch locally and remotely
| git **prunetags** || prune local tags no longer present on origin
| git **tags** || list local and remote tags
| git **newtag** | *TAG* | create a local tag and push it on origin
| git **deltag** | *TAG* | delete a tag locally and remotely
| git **tag2hash** | *TAG* | display short hash of given tag
| git **mvtag** | *OLD NEW* | rename a tag locally and remotely
| git **vbranch** | *MAJOR.MINOR* | create a `vbranch` and a `vnode` (*1.0 <- v1.0-@*)
| git **vmin** | *[MAJOR[.MINOR]]* | display minimum version using given pattern
| git **vmax** | *[MAJOR[.MINOR]]* | display maximum version using given pattern
| git **vtags** || list local `vtag`s
| git **vcommit2tag** | [*HASH*] | display the most recent `vtag` description (1) reachable from given `vcommit`
| git **vcommit2major** | [*HASH*] | display the most recent `MAJOR` version reachable from given `vcommit`
| git **vcommit2minor** | [*HASH*] | display the most recent `MAJOR.MINOR` version reachable from given `vcommit`
| git **vcheckout** | [*HASH*] | checkout the `vbranch` of the given `vcommit`
| git **vminor** | [*HASH*] | checkout the `vbranch` having the same `MAJOR.MINOR` version as the given `vcommit`
| git **vmajor** | [*HASH*] | checkout the `vbranch` having the same `MAJOR` and greatest `MINOR` version as the given `vcommit`
| git **vnext** | [*HASH*] | checkout the `vbranch` with the greatest `MAJOR` and `MINOR` version

(1) `vtag` description exemple : `v1.0.0-2-gc8af267` (see [git describe](https://git-scm.com/docs/git-describe))
