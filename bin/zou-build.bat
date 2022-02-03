@echo off
setlocal EnableDelayedExpansion

rem Display command line
echo [33m[zou-build][0m [44m%0 %*[0m

goto Parse

:Error
echo.
echo [33m[zou-build][31m %error%[0m
set exitcode=1
goto :Help

:Help

echo [36m
echo Usage:
echo   pack [OPTIONS] ['"MSBUILD-OPTIONS"'] PROJECT-NAME
echo.
echo Options:
echo     -h             -- display current help
echo     -n             -- dry run
echo     -p PLATFORM    -- ^(x64^|x86^|Win32^)
echo     -c             -- clean only
echo     -b             -- build only
echo     -a             -- build for Windows, OSX and Linux
echo     -m             -- build remote components
echo     -k             -- redirect pack directory (PkgDir)
echo     -s             -- sign components
echo     -v             -- display some debug info
echo     -bl            -- create build log
echo     -pp            -- create preprocessed XML (does not build)
echo [0m
exit /b %exitcode%

:Parse

if '%1' == '' goto EndParse
set arg=%1
shift

rem Parse stuff like '"-p:PkgDir=..\pkg"'
set firstChar=%arg:~0,2%
set lastChar=%arg:~-2%
rem Replace ' and " with +
set firstChar=%firstChar:'=+%
set lastChar=%lastChar:'=+%
set firstChar=%firstChar:"=+%
set lastChar=%lastChar:"=+%
if '%firstChar%' == '++' if '%lastChar%' == '++' (
  set args=%arg:~2,-2%
  goto :Parse
)

rem 'OPTIONS'
if /i '%arg:~0,2%' == '-h' goto Help
if /i '%arg:~0,2%' == '/h' goto Help
if /i '%arg:~0,2%' == '-?' goto Help
if /i '%arg:~0,2%' == '/?' goto Help

if /i '%arg:~0,2%' == '-p' (
  set val=!arg:~2!
  if '!val!' == '' (
    shift
    set val=%1
  )
  if '!platforms!' == '' (
    set platforms=!val!
  ) else (
    set platforms=!platforms! !val!
  )
  goto :Parse
)
if /i '%arg:~0,2%' == '-k' (
  set val=!arg:~2!
  if '!val!' == '' (
    shift
    set val=%1
  )
  set pkgDir=!val!
  goto :Parse
)
if /i '%arg%' == '-c' (
  set clean=true
  goto :Parse
)
if /i '%arg%' == '-b' (
  set build=true
  goto :Parse
)
if /i '%arg%' == '-s' (
  set sign=true
  goto :Parse
)
if /i '%arg%' == '-m' (
  set rome=true
  goto :Parse
)
if /i '%arg%' == '-a' (
  set all=true
  goto :Parse
)
if /i '%arg%' == '-v' (
  set verbose=true
  goto :Parse
)
if /i '%arg%' == '-t' (
  set test=true
  goto :Parse
)
if /i '%arg%' == '-n' (
  set dry_run=true
  goto :Parse
)
if /i '%arg%' == '-bl' (
  set binlog=true
  goto :Parse
)
if /i '%arg%' == '-pp' (
  set preprocess=true
  goto :Parse
)

if /i '%arg:~0,1%' == '-' (
  set error=Unknown option: '%arg%'.
  goto :Error
)

rem 'PROJECT_NAME'
set project=%arg%

goto Parse

:EndParse

if '%project%' == '' (
  set error=Missing project name.
  goto :Error
)
if not exist "%project%" (
  set error=Project not found: '%project%'.
  goto :Error
)

rem extract project path info
for %%i in ("%project%") do (
  set projDir=%%~dpi
  set projName=%%~ni
  set projExt=%%~xi
)
rem set defaults
if '!platforms!' == '' set platforms=x86 x64
if '%clean%' == '' if '%build%' == '' (
  set clean=true
  set build=true
)

rem compute msbuild options
set opts=-nologo -v:m
if '%binlog%'     == 'true' set opts=%opts% -bl:%projDir%%projName%.binlog
if '%preprocess%' == 'true' set opts=%opts% -pp:%projDir%%projName%.pp.xml

rem compute properties
set props=Configuration=Release
if '%all%'     == 'true' set props=%props%;CrossBuild=true
if '%rome%'    == 'true' set props=%props%;BuildRome=true
if '%sign%'    == 'true' set props=%props%;Sign=true
if '%verbose%' == 'true' set props=%props%;RedistDebug=true;ZouDebug=true
if '%pkgDir%'  neq ''    set props=%props%;PkgDir=%pkgDir%

set command=msbuild %opts% %project% -p:%props%

if '%test%' == 'true' (
  echo [33m[zou-build][90m platforms = !platforms![0m
  echo [33m[zou-build][90m project   = %project%[0m
  echo [33m[zou-build][90m build     = %build%[0m
  echo [33m[zou-build][90m clean     = %clean%[0m
  echo [33m[zou-build][90m args      = %args%[0m
  exit /b
)

rem clean
if '%clean%' == 'true' (
  for %%x in (bin pkg %pkgDir%) do (
    rem avoid to delete root folder
    set dirName=%%x
    set firstChar=!dirName:~0,1!
    set firstChar=!firstChar:/=\!
    if '!firstchar!' neq '\' if exist %%x (
      echo [33m[zou-build][36m Removing %%x...[0m
      if '%dry_run%' == '' rmdir /S /Q %%x
    )
  )
  for %%p in (%platforms%) do (
    echo [33m[zou-build][36m Cleaning %%p...[0m
    echo [33m[zou-build][90m %command% -p:Platform=%%p -t:Clean %args%[0m
    if '%dry_run%' == '' (
      %command% -p:Platform=%%p -t:Clean %args%
      if !errorlevel! neq 0 exit /b !errorlevel!
    )
  )
)

rem build
if '%build%' == 'true' (
  if '%pkgDir%' == '' set pkgDir=pkg
  set projNoPack=!project:pack=!
  for %%p in (%platforms%) do (
    set message=Packing %%p to '!pkgDir!' folder
    if /I '!projNoPack!' == '!project!' set message=Building %%p
    echo [33m[zou-build][36m !message!...[0m
    echo [33m[zou-build][90m %command% -p:Platform=%%p %args%[0m
    if '%dry_run%' == '' (
      %command% -p:Platform=%%p %args%
      if !errorlevel! neq 0 exit /b !errorlevel!
    )
  )
)
