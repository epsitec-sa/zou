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
git config --global alias.bundle-dir "!f() { r=${1:-`pwd -P`}; while [[ -n \"$r\" && ! -d \"$r/.git\" ]]; do r=${r%%/*}; done; echo $r; }; f"
git config --global alias.module-id  "!f() { r=${1:-`pwd -P`}; c=$r; while [[ -n \"$r\" && ! -d \"$r/.git\" ]]; do r=${r%%/*}; done; id=${c#$r}; id=${id#/}; echo ${id%%/}; }; f"
git config --global alias.sfor "submodule foreach"
git config --global alias.sfor-q "submodule --quiet foreach"
git config --global alias.sfor-r "submodule foreach --recursive"
git config --global alias.sfor-qr "submodule --quiet foreach --recursive"
git config --global alias.for "!path=$(basename `pwd`); eval $@; git submodule foreach"
git config --global alias.for-r "!path=$(basename `pwd`); eval $@; git submodule foreach --recursive"
git config --global alias.for-q "!path=$(basename `pwd`); eval $@; git submodule --quiet foreach"
git config --global alias.for-qr "!path=$(basename `pwd`); eval $@; git submodule --quiet foreach --recursive"
git config --global alias.lsm "!git config --file .gitmodules --get-regexp path | awk '{ print $2 }' | sort"
git config --global alias.sdiff "!git for git diff"
git config --global alias.spull "!git pull && git submodule sync --recursive && git submodule update --init --recursive"
git config --global alias.spush "push --recurse-submodules=on-demand"
git config --global alias.zclean "![ -d node_modules ] && rm -rf node_modules; git clean -xdf -e packages"
git config --global alias.sclean "!git for git zclean"
git config --global alias.supdate "submodule update --init --recursive"
:: Branch
git config --global alias.issue "!f() { git checkout -b issue/$1 master && git push -u origin issue/$1; }; f"
git config --global alias.zmaster "!git checkout master && git pull && git supdate"
git config --global alias.smaster "!git sfor git zmaster"
:: rename local + remote branch 
git config --global alias.mvbranch "!f() { local old=$1; local new=$2; git ls-remote --heads --exit-code origin $old >/dev/null || git push origin $old; git branch $new origin/$old >/dev/null 2>&1; git push origin --set-upstream $new >/dev/null 2>&1; git push --delete --force origin $old >/dev/null 2>&1; git branch -D $old >/dev/null 2>&1; }; f"
git config --global alias.curbranch "!c=$(git rev-parse --abbrev-ref HEAD); [ $c == 'HEAD' ] && git rev-parse --short HEAD || echo $c"

:: Tags
git config --global alias.prunetags "!git tag -l | xargs git tag -d; git fetch -t"
git config --global alias.foldtags "!f() { local suffix=$1; for t in $(git tag | grep -$suffix$); do st=$(echo $t | sed s,-$suffix$,,I); git mvtag $t $suffix/$st ; done; }; f"
git config --global alias.otags "!git tag | grep -Ev '^v[0-9]+\.[0-9]+(-@|\.[0-9]+(-(alpha|beta|rc)[0-9A-Za-z-]*(\.[0-9A-Za-z-]*)*)?(\+[0-9A-Za-z-]+(\.[0-9A-Za-z-]*)*)?)$' | grep -Ev '^[^/]+/.+$' || true"
git config --global alias.foldotags "!for t in $(git otags); do git mvtag $t other/$t; done"
git config --global alias.tags "!git tag; git ls-remote --tags origin | sed s,[[:alnum:]]*[[:space:]]*,,"
git config --global alias.newtag-lo "!f() { local tag=$1; shift; local opt=$([[ -z \"$@\" ]] && echo -m$(git log -1 --pretty=%%B) || echo -a $@); git tag \"$opt\" $tag; }; f"
git config --global alias.newtag "!f() { local tag=$1; git newtag-lo $@; git push origin $tag; }; f"
git config --global alias.deltag "!f() { git tag --delete $1; git push --delete origin $1 >/dev/null 2>&1; }; f"
:: get short hash of given tag
git config --global alias.tag2hash "!f() { git describe --tags --long $1 | sed s,.*-[0-9]*-g,,g; }; f"
:: rename local + remote tag
git config --global alias.tagger-name "!f() { git show $1 -q | grep Tagger: | sed -E 's,Tagger:\s+(.*)\s+<(.*)>,\1,'; }; f"
git config --global alias.tagger-email "!f() { git show $1 -q | grep Tagger: | sed -E 's,Tagger:\s+(.*)\s+<(.*)>,\2,'; }; f"
git config --global alias.tag-date "!f() { git show $1 -q | grep Date: | sed -E 's,Date:\s+(.*),\1,' | head -n 1; }; f"
git config --global alias.tag-comment "!f() { git tag -l -n1 $1 | sed -E 's,\w+\s+(.*),\1,'; }; f"
git config --global alias.mvtag-lo "!f() { local tagger=$(git tagger-name $1); [[ -z \"$tagger\" ]] && { echo Renaming lightweight tag $1 to $2; git tag -f $2 $1^{}; } || { echo Renaming annotated tag $1 to $2; GIT_COMMITTER_NAME=$tagger GIT_COMMITTER_EMAIL=$(git tagger-email $1) GIT_COMMITTER_DATE=$(git tag-date $1) git tag -f -m \"$(git tag-comment $1)\" $2 $1^{}; }; [[ \"$1\" != \"$2\" ]] && git tag --delete $1 >/dev/null; }; f"
git config --global alias.mvtag "!f() { git mvtag-lo $@; [[ \"$1\" != \"$2\" ]] && git push --delete origin $1 >/dev/null 2>&1; git push origin $2 >/dev/null 2>&1; }; f"
git config --global alias.mirrortags "!git push --tags --prune || true"
:: zouify tags (apply zou-flow):
:: 1. move non SemVer tags (otags) to other folder
:: 2. group tags pointing to the same commit hash - create a lookup table with the commit hash as key and the commit tags (space separated) as value.
:: 3. remove other folder's redondant tags
git config --global alias.ztags-lo "!f() { for tag in $(git otags); do echo Moving $tag to other folder; git mvtag-lo $tag \"other/$tag\"; done; declare -A lookup; for tag in $(git tag); do h=$(git tag2hash $tag); if [ -z \"${lookup[$h]}\" ]; then lookup[$h]=$tag; echo \"$h : ${lookup[$h]}\"; else lookup[$h]=\"${lookup[$h]} $tag\"; echo \"$h : ${lookup[$h]}\"; fi; done; echo; echo Redondant tags; for k in ${!lookup[@]}; do read -a r <<< \"${lookup[$k]}\"; if [[ ${#r[@]} > 1 ]]; then echo ${#r[@]} $k = ${r[@]}; for tag in \"${r[@]}\"; do [[ $tag == other/* ]] git tag --delete $tag; done; fi; done; }; f"
git config --global alias.ztags "!f() { local z=$(git ztags-lo $@); [[ -n \"$z\" ]] && git mirrortags }; f"

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
git config --global alias.vtag2next "!f () { [[ -n $1 ]] && tag=$1 || tag=$(git vcommit2tag); if [[ $tag =~ ^v([0-9]+)\.([0-9]+)(\.|-)(.*)-([0-9]+)-g([[:alnum:]]*)$ ]]; then major=${BASH_REMATCH[1]}; minor=${BASH_REMATCH[2]}; patch=${BASH_REMATCH[4]}; delta=${BASH_REMATCH[5]}; vmax=$(git vmax $major.$minor); if [[ $vmax =~ ^v([0-9]+)\.([0-9]+)(\.|-)(.*) ]]; then patch_max=${BASH_REMATCH[4]}; if [[ $patch_max != $patch ]]; then patch=$patch_max; delta=1; fi; fi; if [[ $patch == '@' ]]; then echo \"v$major.$minor.0\"; elif [[ -n $delta ]]; then if [[ $patch =~ ([0-9]+)(-[[:alnum:]]+)?(\+.+)?$ ]]; then patch=${BASH_REMATCH[1]}; suffix=${BASH_REMATCH[2]}; if [[ $suffix =~ -(alpha|beta|rc)([0-9]*) ]]; then prerel_label=${BASH_REMATCH[1]}; prerel_rev=${BASH_REMATCH[2]}; echo \"v$major.$minor.$patch-$prerel_label$((prerel_rev+1))\"; else echo \"v$major.$minor.$((patch+1))\"; fi; fi; fi; else [[ $tag != ${tag/-@/.0} ]] && echo ${tag/-@/.0}; fi; }; f"
git config --global alias.vtagauto-lo "!f() { vtag=$(git vcommit2tag); vnext=$(git vtag2next $vtag); [[ -z $vnext ]] && echo Current tag: $vtag || { git newtag-lo $vnext; echo Tag created: $vnext; }; }; f"
git config --global alias.vtagauto "!f() { vtag=$(git vcommit2tag); vnext=$(git vtag2next $vtag); [[ -z $vnext ]] && echo Current tag: $vtag || { git newtag $vnext; echo Tag created: $vnext; }; }; f"
git config --global alias.vcommit2tag "!f() { git describe --tags --match 'v[0-9]*' $1 2>/dev/null || true; }; f"
git config --global alias.vcommit2major "!f() { git vcommit2tag $1 |  sed -E 's,^v([0-9]+).*,\1,'; }; f"
git config --global alias.vcommit2minor "!f() { git vcommit2tag $1 |  sed -E 's,^v([0-9]+\.[0-9]+).*,\1,'; }; f"
git config --global alias.vcheckout "!f() { git vcommit2minor $1 | xargs git checkout >/dev/null; git pull --all --prune; git supdate; }; f"
git config --global alias.vmajor "!f() { git vcommit2major $1 | xargs git vmax | xargs git vcheckout; }; f"
git config --global alias.vminor "!f() { git vcommit2minor $1 | xargs git vmax | xargs git vcheckout; }; f"
git config --global alias.vnext "!f() { git vmax $1 | xargs git vcheckout; }; f"
git config --global alias.vtable "!row() { local vminor; local vmajor; local module=$1; local head=$(git curbranch); local tag=$(git vcommit2tag | sed -E 's,(.*)-g[[:alnum:]]*$,\1,'); if [[ $tag =~ (.*)-([0-9]+)$ ]]; then local version=${BASH_REMATCH[1]}; local delta=${BASH_REMATCH[2]}; else local version=$tag; fi; local OIFS=$IFS; IFS=' '; local tags=$(git tag -l --sort=-v:refname); if [[ $version =~ ^v([0-9]+)\.([0-9]+) ]]; then local major=${BASH_REMATCH[1]}; local minor=${BASH_REMATCH[2]}; vminor=$(echo $tags | grep -m1 ^v$major\.$minor); vmajor=$(echo $tags | grep -m1 ^v$major\.); fi; local vnext=$(echo $tags | grep -m1 ^v[0-9]); local md_module=$module; local md_version=$version; [[ $version =~ ^.*-@$ || -n $delta ]] && md_module=\"##$module##\"; [[ $version =~ ^.*-@$ ]] && md_version=\"##$version##\"; [[ $version == $vminor ]] && unset vminor || vminor=\"#$vminor#\"; [[ $version == $vmajor ]] && unset vmajor || vmajor=\"#$vmajor#\"; [[ $version == $vnext ]] && unset vnext || vnext=\"#$vnext#\"; echo $module:_ $md_module _ $head _ $delta _ $md_version _ $vminor _ $vmajor _ $vnext; IFS=$OIFS; }; v1=`pwd`/_v1; [[ -f $v1 ]] && rm $v1; echo _ Module _ Head _ Delta _ Version _ vminor _ vmajor _ vnext >$v1; echo _:-_:-_:-_:-_:-_:-_:- >>$v1; row $(basename `pwd`) | cut -d':' -f2 >>$v1; export -f row; export v2=`pwd`/_v2; [[ -f $v2 ]] && rm $v2; git submodule foreach --recursive 'row $(git module-id) >>$v2'; [[ -f $v2 ]] && sort -t: -k1 $v2 | cut -d: -f2 >>$v1 && rm $v2; sed -E 's,_,|,g' $v1 | sed -E 's,#,\*,g' >versions.md; rm $v1"
git config --global alias.attach "!f() { set -f; commit=HEAD; patterns=(); while [[ \"$#\" > 0 ]]; do case \"$1\" in -c|--commit) commit=\"$2\"; shift 2;; --commit=*) commit=\"${1#*=}\"; shift;; -d|--dev) opt_patterns+=('\bdev\b' '^master$'); shift;; -p|--prod) opt_patterns+=('[0-9]+\.[0-9]+' '\bprod\b' '^master$'); shift;; -*) echo \"unknown option: $1\" >&2; exit 1;; *) patterns+=(\"$1\"); shift 1;; esac done; patterns+=(${opt_patterns[@]}); [[ ${#patterns[@]} == 0 ]] && patterns+=('.*'); hash=$(git rev-parse $commit); branches=(); for b in $(git branch -a --merged $commit | grep -v HEAD); do [[ $(git rev-parse $b) == $hash ]] && branches+=(${b#remotes/origin/}); done; branches=(`echo ${branches[@]} | xargs -n1 echo | sort -u | xargs echo`); for b in $(git branch -a --no-merged $commit | grep -v HEAD); do for i in ${!branches[@]}; do [[ ${branches[i]} == $b ]] && unset branches[i]; done; done; if (( ${#branches[@]} >= 1 )); then for p in ${patterns[@]}; do for b in ${branches[@]}; do branch=$(echo $b | grep -E $p); [[ -n $branch ]] && break 2; done done fi; [[ -n \"$branch\" ]] && git checkout $branch || true; }; f"

if '%copy%'=='true' copy /Y "%USERPROFILE%\.gitconfig" "%USERPROFILE%\.gitconfig.cmd.zou" >nul