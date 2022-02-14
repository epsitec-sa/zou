@echo off
setlocal EnableDelayedExpansion

rem Display command line
echo [33m[zou-build] [0;1;4m%0 %*[0m

goto Parse

:Error
echo.
echo [91mError: %_error%[0m
set _exitcode=1
goto :Help

:Help

echo [92m
echo Usage:
echo [37m    pack [OPTIONS] ['"MSBUILD-OPTIONS"'] PROJECT-NAME
echo.
echo [92mOptions:
echo [37m    -h             [92m-- display current help
echo [37m    -n             [92m-- dry run
echo [37m    -pPLATFORM     [92m-- [37m^(*x64^|*x86^|Win32^)
echo [37m    -c             [92m-- clean only
echo [37m    -b             [92m-- build only
echo [37m    -a             [92m-- build for Windows, OSX and Linux
echo [37m    -m             [92m-- build remote components
echo [37m    -k             [92m-- redirect pack directory (PkgDir)
echo [37m    -s             [92m-- sign components
echo [37m    -v             [92m-- display some debug info
echo [37m    --cp           [92m-- custom platform (do not specify any platform to msbuild)
echo [37m    --bl           [92m-- create build log (.binlog)
echo [37m    --pp           [92m-- create preprocessed XML (does not build)
echo [0m
exit /b %_exitcode%

:Parse

if '%1' == '' goto EndParse
set _arg=%1
shift

rem Parse stuff like '"-p:PkgDir=..\pkg"'
set _firstChar=%_arg:~0,2%
set _lastChar=%_arg:~-2%
rem Replace ' and " with +
set _firstChar=%_firstChar:'=+%
set _lastChar=%_lastChar:'=+%
set _firstChar=%_firstChar:"=+%
set _lastChar=%_lastChar:"=+%
if '%_firstChar%' == '++' if '%_lastChar%' == '++' (
  set _args=%_arg:~2,-2%
  goto :Parse
)

rem 'OPTIONS'
if /i '%_arg:~0,2%' == '-h' goto Help
if /i '%_arg:~0,2%' == '/h' goto Help
if /i '%_arg:~0,2%' == '-?' goto Help
if /i '%_arg:~0,2%' == '/?' goto Help

if /i '%_arg%' == '--cp' (
  set _customPlatform=true
  goto :Parse
) else (
  if /i '%_arg:~0,2%' == '-p' (
    set val=!_arg:~2!
    if '!val!' == '' (
      shift
      set val=%1
    )
    if '!_platforms!' == '' (
      set _platforms=!val!
    ) else (
      set _platforms=!_platforms! !val!
      set _byRuntime=true
    )
    goto :Parse
  )
)
if /i '%_arg:~0,2%' == '-k' (
  set val=!_arg:~2!
  if '!val!' == '' (
    shift
    set val=%1
  )
  set _pkgDir=!val!
  goto :Parse
)
if /i '%_arg%' == '-c' (
  set _clean=true
  goto :Parse
)
if /i '%_arg%' == '-b' (
  set _build=true
  goto :Parse
)
if /i '%_arg%' == '-s' (
  set _sign=true
  goto :Parse
)
if /i '%_arg%' == '-m' (
  set _rome=true
  goto :Parse
)
if /i '%_arg%' == '-a' (
  set _all=true
  goto :Parse
)
if /i '%_arg%' == '-v' (
  set _verbose=true
  goto :Parse
)
if /i '%_arg%' == '-t' (
  set _test=true
  goto :Parse
)
if /i '%_arg%' == '-n' (
  set _dryRun=true
  goto :Parse
)
if /i '%_arg%' == '--bl' (
  set _binlog=true
  goto :Parse
)
if /i '%_arg%' == '--pp' (
  set _preprocess=true
  goto :Parse
)

if /i '%_arg:~0,1%' == '-' (
  set _error=Unknown option: '%_arg%'.
  goto :Error
)

rem 'PROJECT_NAME'
set _project=%_arg%

goto Parse

:EndParse

if '%_project%' == '' (
  set _error=missing project name.
  goto :Error
)
if not exist "%_project%" (
  set _error=_project not found: [97m%_project%[91m.
  goto :Error
)
if not '!_platforms!' == '' if not '!_customPlatform!' == '' (
  set _error=options [97m--cp[91m and [97m-pPLATFORM[91m are mutually exclusive.
  goto :Error
)

rem extract _project path info
for %%i in ("%_project%") do (
  set _projDir=%%~dpi
  set _projName=%%~ni
  set _projExt=%%~xi
)
rem set defaults
if '!_platforms!' == '' if '!_customPlatform!' == '' (
  set _byRuntime=true
  set _platforms=x86 x64
)
if '%_clean%' == '' if '%_build%' == '' (
  set _clean=true
  set _build=true
)

rem compute msbuild options
set _opts=--nologo -v:m -m
if '%_binlog%'     == 'true' set _opts=%_opts% -bl:%_projDir%%_projName%._binlog
if '%_preprocess%' == 'true' set _opts=%_opts% -pp:%_projDir%%_projName%.pp.xml

rem compute properties
set props=Configuration=Release;MaxCpuCount=0
if '%_byRuntime%' == 'true' set props=%props%;RedistByRuntime=true
if '%_sign%'      == 'true' (set props=%props%;Sign=true) else (set props=%props%;Sign=false)
if '%_all%'       == 'true' set props=%props%;CrossBuild=true
if '%_rome%'      == 'true' set props=%props%;BuildRome=true
if '%_verbose%'   == 'true' set props=%props%;RedistDebug=true;ZouDebug=true
if '%_pkgDir%' neq ''       set props=%props%;PkgDir=%_pkgDir%

if /i '%_projExt%' == '.csproj' (set command=dotnet _build) else (set command=msbuild)
set command=%command% %_opts% %_project% -p:%props%

if '%_test%' == 'true' (
  echo [33m[zou-build][90m _byRuntime  = !_byRuntime![0m
  echo [33m[zou-build][90m _customPlatform = !_customPlatform![0m
  echo [33m[zou-build][90m _platforms  = !_platforms![0m
  echo [33m[zou-build][90m _project    = %_project%[0m
  echo [33m[zou-build][90m _build      = %_build%[0m
  echo [33m[zou-build][90m _clean      = %_clean%[0m
  echo [33m[zou-build][90m _args       = %_args%[0m
  exit /b
)

rem ################## _clean
if '%_clean%' == 'true' (
  for %%x in (x86 x64 Win32 bin pkg %pkgDir%) do (
    rem avoid to delete root folder
    set dirName=%%x
    set _firstChar=!dirName:~0,1!
    set _firstChar=!_firstChar:/=\!
    if '!firstchar!' neq '\' if exist %%x (
      if '%_dryRun%' == '' (
        echo [33m[zou-build] [36mRemoving %%x...[0m
        rmdir /S /Q %%x
      ) else (
        echo [33m[zou-build] [90mRemoving %%x...[0m
      )
    )
  )
  for %%p in (%_platforms%) do (
    echo [33m[zou-build] [36mCleaning %%p...[0m
    echo [33m[zou-build] [90m%command% -p:Platform=%%p -t:Clean %_args%[0m
    if '%_dryRun%' == '' (
      %command% -p:Platform=%%p -t:Clean %_args%
      if !errorlevel! neq 0 exit /b !errorlevel!
    )
  )
)

rem ################## _build
if '%_build%' == 'true' (
  if '%_pkgDir%' == '' set _pkgDir=pkg
  set projNoPack=!_project:pack=!
  if '!_platforms!' == '' (
    set message=Packing to '!_pkgDir!' folder
    if /I '!projNoPack!' == '!_project!' set message=Building
    echo [33m[zou-build][36m !message!...[0m
    echo [33m[zou-build][90m %command% %_args%[0m
    if '%_dryRun%' == '' (
      %command% %_args%
      if !errorlevel! neq 0 exit /b !errorlevel!
    )
  ) else (
    for %%p in (%_platforms%) do (
      set message=Packing %%p to '!_pkgDir!' folder
      if /I '!projNoPack!' == '!_project!' set message=Building %%p
      echo [33m[zou-build][36m !message!...[0m
      echo [33m[zou-build][90m %command% -p:Platform=%%p %_args%[0m
      if '%_dryRun%' == '' (
        %command% -p:Platform=%%p %_args%
        if !errorlevel! neq 0 exit /b !errorlevel!
      )
    )
  )
)
