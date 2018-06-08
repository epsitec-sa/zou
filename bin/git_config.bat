@echo off

git config --global status.submoduleSummary true
git config --global diff.submodule log

:: Aliases
git config --global --remove-section alias >nul 2>&1
git config --global alias.oprune "fetch origin --prune"
:: Sub-modules
git config --global alias.sdiff "!git diff && git submodule foreach 'git diff'"
git config --global alias.spull "!git pull && git submodule sync --recursive && git submodule update --init --recursive"
git config --global alias.spush "push --recurse-submodules=on-demand"
git config --global alias.sclean "![ -d node_modules ] && rm -rf node_modules; git clean -xdf -e packages && git submodule foreach --recursive '[ -d node_modules ] && rm -rf node_modules; git clean -xdf'"
git config --global alias.supdate "submodule update --init --recursive"
:: Branch
git config --global alias.issue "!f() { git checkout -b issue/$1 master && git push -u origin issue/$1; }; f"
git config --global alias.smaster "!git checkout master && git pull && git submodule foreach 'git checkout master && git pull'"
:: rename local + remote branch 
git config --global alias.mvbranch "!f() { local old=$1; local new=$2; git ls-remote --heads --exit-code origin $old >/dev/null || git push origin $old; git branch $new origin/$old >/dev/null 2>&1; git push origin --set-upstream $new >/dev/null 2>&1; git push --delete --force origin $old >/dev/null 2>&1; git branch -D $old >/dev/null 2>&1; }; f"

:: Tags
git config --global alias.prunetags "!git tag -l | xargs git tag -d && git fetch -t"
git config --global alias.tags "!git tag; git ls-remote --tags origin | sed s/[[:alnum:]]*[[:space:]]*//"
git config --global alias.newtag "!f() { git tag $1 $2; git push origin $1; }; f"
git config --global alias.deltag "!f() { git tag --delete $1; git push --delete origin $1; }; f"
:: get short hash of given tag
git config --global alias.htag "!f() { git describe --tags --long $1 | sed s,.*-[0-9]*-g,,g; }; f"
:: rename local + remote tag 
git config --global alias.mvtag "!f() { local old=$1; local new=$2; local hash=$(git htag $old); git rmtag $old; git maketag $new $htag; }; f"

:: Semantic versioning
git config --global --remove-section versionsort >nul 2>&1
git config --global --add versionsort.suffix -@
git config --global --add versionsort.suffix -alpha
git config --global --add versionsort.suffix -beta
git config --global --add versionsort.suffix -rc

:: VBranch
git config --global alias.vbranch "!f() { git checkout -b $1 && git tag v$1-@; }; f"
:: VTag
git config --global alias.vmin "!f() { local version=$1; local regex; if [[ -z \"$version\" ]]; then regex=[0-9]; else regex=$(echo $version | sed s,[.],\\.,g); fi; git tag -l --sort=v:refname | grep -m1 ^v$regex; }; f"
git config --global alias.vmax "!f() { local version=$1; local regex; if [[ -z \"$version\" ]]; then regex=[0-9]; else regex=$(echo $version | sed s,[.],\\.,g); fi; git tag -l --sort=-v:refname | grep -m1 ^v$regex; }; f"
git config --global alias.vtags "!f() { local version=$1; local regex; if [[ -z \"$version\" ]]; then regex=[0-9]; else regex=$(echo $version | sed s,[.],\\.,g); fi; git tag -l --sort=-v:refname | grep ^v$regex; }; f"
