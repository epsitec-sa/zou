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
| git **issue** | DESCR | create an issue branch (`git issue 305-buggy` -> *issue/305-buggy**)
| git **mvbranch** | *OLD NEW* | rename a branch locally and remotely
| git **prunetags** || prune local tags no longer present on origin
| git **tags** || list local and remote tags
| git **newtag** | *TAG* | create a local tag and push it on origin
| git **deltag** | *TAG* | delete a tag locally and remotely
| git **htag** | *TAG* | display short hash of given tag
| git **mvtag** | *OLD NEW* | rename a tag locally and remotely
| git **vbranch** | *BRANCH* | create a vbranch and a vnode
| git **vmin** | *[MAJOR[.MINOR]]* | display minimum version using given pattern
| git **vmax** | *[MAJOR[.MINOR]]* | display maximum version using given pattern
| git **vtags** || list local vtags
