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

:: https://stackoverflow.com/questions/5195859/how-do-you-push-a-tag-to-a-remote-repository-using-git
git config --global push.followTags true

:: save credentials on Windows
git config --global credential.helper wincred

if '%reset%'=='true' (
	git config --global --remove-section status >nul 2>&1
    git config --global --remove-section diff >nul 2>&1
	git config --global --remove-section alias >nul 2>&1
)

:: Semantic versioning
git config --global --remove-section versionsort >nul 2>&1
git config --global --add versionsort.suffix -@
git config --global --add versionsort.suffix -alpha
git config --global --add versionsort.suffix -beta
git config --global --add versionsort.suffix -rc

if '%copy%'=='true' copy /Y "%USERPROFILE%\.gitconfig" "%USERPROFILE%\.gitconfig.cmd.zou" >nul