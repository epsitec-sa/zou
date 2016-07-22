@echo off

git config --global status.submoduleSummary true
git config --global diff.submodule log
git config --global alias.sdiff "!git diff && git submodule foreach 'git diff'"
git config --global alias.spull "!git pull && git submodule sync --recursive && git submodule update --init --recursive"
git config --global alias.spush "push --recurse-submodules=on-demand"
git config --global alias.sclean "!git clean -xdf && git submodule foreach --recursive 'git clean -xdf'"
git config --global alias.oprune "fetch origin --prune"
git config --global alias.supdate "submodule update --init --recursive"