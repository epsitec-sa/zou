#!/bin/sh

git config --global status.submoduleSummary true
git config --global diff.submodule log
git config --global alias.sdiff '!'"git diff && git submodule foreach 'git diff'"
git config --global alias.spull '!git pull && git submodule sync --recursive && git submodule update --init --recursive'
git config --global alias.spush 'push --recurse-submodules=on-demand'
git config --global alias.sclean '!'"[ -d node_modules ] && rm -rf node_modules; git clean -xdf -e packages && git submodule foreach --recursive '[ -d node_modules ] && rm -rf node_modules; git clean -xdf'"
git config --global alias.oprune 'fetch origin --prune'
git config --global alias.supdate 'submodule update --init --recursive'
git config --global alias.issue '!f() { git checkout -b issue/$1 master && git push -u origin issue/$1; }; f'
git config --global alias.smaster '!'"git checkout master && git pull && git submodule foreach 'git checkout master && git pull'"

# Semantic versioning
git config --global --remove-section versionsort >/dev/null 2>&1
git config --global --add versionsort.suffix -@
git config --global --add versionsort.suffix -alpha
git config --global --add versionsort.suffix -beta
git config --global --add versionsort.suffix -rc

git config --global alias.vmax '!f() { local version=$1; local regex; if [[ -z "$version" ]]; then regex=[0-9]; else regex=$(echo $version | sed s,[.],\\.,g); fi; git tag -l --sort=-v:refname | grep -m1 ^v$regex; }; f'
git config --global alias.vlog '!f() { local version=$1; local regex; if [[ -z "$version" ]]; then regex=[0-9]; else regex=$(echo $version | sed s,[.],\\.,g); fi; git tag -l --sort=-v:refname | grep ^v$regex; }; f'
git config --global alias.vbranch '!'"f() { git checkout -b $1 && git tag v$1-@; }; f"