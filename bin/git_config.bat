@echo off

:: Parse command line
:parse
if NOT '%1' == '' (
	if /i '%1' == '-h' (
		echo.
		echo Usage:
		echo   %0 [OPTIONS]
		echo.
		echo Options:
		echo   -h		Help
		echo   -r		Reset aliases
		echo   -c		Create a copy to %USERPROFILE%\.gitconfig.cmd.zou
		exit /b
	)
	if /i '%1' == '-r' set reset=true
	if /i '%1' == '-c' set copy=true
	shift
	goto :parse
)

git config --global status.submoduleSummary true
git config --global diff.submodule log

:: Aliases
if '%reset%'=='true' git config --global --remove-section alias >nul 2>&1

git config --global alias.oprune "fetch origin --prune"
:: Bundle and sub-modules
git config --global alias.bundle-dir "!f() { r=${1:-`pwd -P`}; while [[ \"$r\" != '' && ! -d \"$r/.git\" ]]; do r=${r%%/*}; done; echo $r; }; f"
git config --global alias.module-id  "!f() { r=${1:-`pwd -P`}; c=$r; while [[ \"$r\" != '' && ! -d \"$r/.git\" ]]; do r=${r%%/*}; done; id=${c#$r}; id=${id#/}; echo ${id%%/}; }; f"
git config --global alias.sfor "submodule foreach"
git config --global alias.sfor-q "submodule --quiet foreach"
git config --global alias.sfor-r "submodule foreach --recursive"
git config --global alias.sfor-qr "submodule --quiet foreach --recursive"
git config --global alias.for "!path=$(basename `pwd`); eval $@; git submodule foreach"
git config --global alias.for-r "!path=$(basename `pwd`); eval $@; git submodule foreach --recursive"
git config --global alias.for-q "!path=$(basename `pwd`); eval $@; git submodule --quiet foreach"
git config --global alias.for-qr "!path=$(basename `pwd`); eval $@; git submodule --quiet foreach --recursive"

git config --global alias.sdiff "!git for git diff"
git config --global alias.spull "!git pull && git submodule sync --recursive && git submodule update --init --recursive"
git config --global alias.spush "push --recurse-submodules=on-demand"
git config --global alias.zclean "![ -d node_modules ] && rm -rf node_modules; git clean -xdf -e packages"
git config --global alias.sclean "!git for git zclean"
git config --global alias.supdate "submodule update --init --recursive"
:: Branch
git config --global alias.issue "!f() { git checkout -b issue/$1 master && git push -u origin issue/$1; }; f"
git config --global alias.zmaster "!git checkout master && git pull"
git config --global alias.smaster "!git for git zmaster"
:: rename local + remote branch 
git config --global alias.mvbranch "!f() { local old=$1; local new=$2; git ls-remote --heads --exit-code origin $old >/dev/null || git push origin $old; git branch $new origin/$old >/dev/null 2>&1; git push origin --set-upstream $new >/dev/null 2>&1; git push --delete --force origin $old >/dev/null 2>&1; git branch -D $old >/dev/null 2>&1; }; f"
git config --global alias.curbranch "!c=$(git rev-parse --abbrev-ref HEAD); [ $c == 'HEAD' ] && git rev-parse --short HEAD || echo $c"

:: Tags
git config --global alias.prunetags "!git tag -l | xargs git tag -d && git fetch -t"
git config --global alias.foldtags "!f() { local suffix=$1; for t in $(git tag | grep -$suffix$); do st=$(echo $t | sed s,-$suffix$,,I); git mvtag $t $suffix/$st ; done; }; f"
git config --global alias.otags "!git tag | grep -Ev '^v[0-9]+\.[0-9]+(-@|\.[0-9]+(-(alpha|beta|rc)[0-9A-Za-z-]*(\.[0-9A-Za-z-]*)*)?(\+[0-9A-Za-z-]+(\.[0-9A-Za-z-]*)*)?)$' | grep -Ev '^[^/]+/.+$' || true"
git config --global alias.foldotags "!for t in $(git otags); do git mvtag $t other/$t; done"
git config --global alias.tags "!git tag; git ls-remote --tags origin | sed s,[[:alnum:]]*[[:space:]]*,,"
git config --global alias.newtag "!f() { local tag=$1; shift; local opt=$([[ \"$@\" == '' ]] && echo -m$(git log -1 --pretty=%%B) || echo -a $@); git tag \"$opt\" $tag; git push origin $tag; }; f"
git config --global alias.deltag "!f() { git tag --delete $1 >/dev/null 2>&1; git push --delete origin $1 >/dev/null 2>&1; }; f"	
:: get short hash of given tag
git config --global alias.tag2hash "!f() { git describe --tags --long $1 | sed s,.*-[0-9]*-g,,g; }; f"
:: rename local + remote tag 
git config --global alias.mvtag "!f() { local old=$1; local new=$2; echo Renaming $old to $new; local ocomment=$(git tag -l -n1 $old | cut -d' ' -f2- | sed s,^[[:space:]]*,,); git tag -f -m \"$ocomment\" $new $old^{}; [[ \"$old\" != \"$new\" ]] && git deltag $old; git push origin $new >/dev/null 2>&1; }; f"
:: zouify tags (apply zou-flow):
:: 1. move non SemVer tags (otags) to other folder
:: 2. group tags pointing to the same commit hash - create a lookup table with the commit hash as key and the commit tags (space separated) as value.
:: 3. remove other folder's redondant tags
git config --global alias.ztags "!f() { for tag in $(git otags); do echo Moving $tag to other folder; git mvtag $tag \"other/$tag\"; done; declare -A lookup; for tag in $(git tag); do h=$(git tag2hash $tag); if [ -z \"${lookup[$h]}\" ]; then lookup[$h]=$tag; echo \"$h : ${lookup[$h]}\"; else lookup[$h]=\"${lookup[$h]} $tag\"; echo \"$h : ${lookup[$h]}\"; fi; done; echo; echo Redondant tags; for k in ${!lookup[@]}; do read -a r <<< \"${lookup[$k]}\"; if [[ ${#r[@]} > 1 ]]; then echo ${#r[@]} $k = ${r[@]}; for tag in \"${r[@]}\"; do [[ $tag == other/* ]] && echo Deleting $tag && git deltag $tag; done; fi; done; }; f"

:: Semantic versioning
git config --global --remove-section versionsort >nul 2>&1
git config --global --add versionsort.suffix -@
git config --global --add versionsort.suffix -alpha
git config --global --add versionsort.suffix -beta
git config --global --add versionsort.suffix -rc

:: VBranch
git config --global alias.vbranch "!f() { git checkout -b $@ && git newtag v$1-@ && git push -u origin $1; }; f"
:: VTag
git config --global alias.vmin "!f() { local version=$1; local regex; if [[ -z \"$version\" ]]; then regex=[0-9]; else regex=$(echo $version | sed s,[.],\\.,g); fi; git tag -l --sort=v:refname | grep -m1 ^v$regex || true; }; f"
git config --global alias.vmax "!f() { local version=$1; local regex; if [[ -z \"$version\" ]]; then regex=[0-9]; else regex=$(echo $version | sed s,[.],\\.,g); fi; git tag -l --sort=-v:refname | grep -m1 ^v$regex || true; }; f"
git config --global alias.vtags "!f() { local version=$1; local regex; if [[ -z \"$version\" ]]; then regex=[0-9]; else regex=$(echo $version | sed s,[.],\\.,g); fi; git tag -l --sort=-v:refname | grep ^v$regex || true; }; f"

git config --global alias.vcommit2tag "!f() { git describe --tags --match 'v[0-9]*' $1 2>/dev/null || true; }; f"
git config --global alias.vcommit2major "!f() { git vcommit2tag $1 |  sed -E 's,^v([0-9]+).*,\1,'; }; f"
git config --global alias.vcommit2minor "!f() { git vcommit2tag $1 |  sed -E 's,^v([0-9]+\.[0-9]+).*,\1,'; }; f"
git config --global alias.vcheckout "!f() { git vcommit2minor $1 | xargs git checkout >/dev/null; }; f"
git config --global alias.vmajor "!f() { git vcommit2major $1 | xargs git vmax | xargs git vcheckout; }; f"
git config --global alias.vminor "!f() { git vcommit2minor $1 | xargs git vmax | xargs git vcheckout; }; f"
git config --global alias.vnext "!f() { git vmax $1 | xargs git vcheckout; }; f"
git config --global alias.vtable "!row() { echo _ $1 _ $(git curbranch) _ $((git vcommit2tag 2>/dev/null || true) | sed -E 's,(.*)-g[[:alnum:]]*$,\1,' | sed -E 's,(.*)-([0-9]+)$,\1 _ \2,'); }; export -f row; export v1=`pwd`/_v1; export v2=`pwd`/_v2; echo _ Module _ Head _ Version _ Delta >$v1; echo _:-_:-_:-_:- >>$v1; row $(basename `pwd`) >>$v1; git submodule foreach --recursive 'row $(git module-id) >>$v2'; sort $v2 >>$v1; sed -E 's,_,|,g' $v1 >versions.md; rm $v1 $v2"

if '%copy%'=='true' copy /Y "%USERPROFILE%\.gitconfig" "%USERPROFILE%\.gitconfig.cmd.zou" >nul