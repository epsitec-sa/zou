#!/usr/bin/env bash

# Parse
while [[ $# -gt 0 ]]; do
    case $1 in
        -h )
            echo
            echo 'Usage:'
            echo "  $0 [OPTIONS]"
            echo
            echo 'Options:'
            echo '  -h		Help'
            echo '  -r		Reset aliases'
            echo "  -c		Create a copy to $(echo $USERPROFILE | sed 's,\\,/,g')/.gitconfig.bash.zou"
            exit 0
            ;;
        -r ) reset=true; shift;;
        -c ) copy=true; shift;;
    esac
done

# https://stackoverflow.com/questions/5195859/how-do-you-push-a-tag-to-a-remote-repository-using-git
git config --global push.followTags true

# save credentials in ~/.git-credentials
git config --global credential.helper store

[ "$reset" == 'true' ] && {
	git config --global --remove-section status >/dev/null 2>&1
	git config --global --remove-section diff >/dev/null 2>&1
	git config --global --remove-section alias >/dev/null 2>&1
}

# Semantic versioning
git config --global --remove-section versionsort >/dev/null 2>&1
git config --global --add versionsort.suffix -@
git config --global --add versionsort.suffix -alpha
git config --global --add versionsort.suffix -beta
git config --global --add versionsort.suffix -rc

[ "$copy" == 'true' ] && cp -f "$USERPROFILE/.gitconfig" "$USERPROFILE/.gitconfig.bash.zou"