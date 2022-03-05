@echo off
setlocal EnableDelayedExpansion
set _zouCmd=%~n0

rem Display command line
echo [33m[%_zouCmd%] [0;1;4m%0 %*[0m

rem Setup defaults
set _build=true
set _clean=false
set _sign=false

goto Parse

:Error
echo.
echo [91mError: %_error%[0m
set _exitcode=1
goto :Help

:Help

echo [92m
echo Usage:
echo [37m    %_zouCmd% PROJECT-NAME [90m[OPTIONS] [[33m'"[90m-p:KEY=VAL[90m;...[33m"'[90m]
echo.
echo [92mOptions:
echo [37m    -h             [92m display current help
echo [37m    -n             [92m dry run (display commands)
echo [37m    -1             [92m use single processor
echo [37m    -e[90m^|[37m--clean     [92m clean
echo [37m    -x[90m^|[37m--rebuild   [92m rebuild
echo [37m    -c[90m=[37mCONFIG      [92m ^([37mRelease[92m^|[90mDebug[92m^)
echo [37m    -p[90m=[37mPLATFORM    [92m ^([37mx86[92m,[37mx64[92m,[90mWin32[92m,[90mAnyCPU[92m^|[90m-[92m)
echo [96m 1)[37m -p-            [92m do not specify platform
echo [96m 1)[37m -a[90m^|[37m--cross     [92m cross build ^([37mWindows[92m,[90mOSX[92m,[90mLinux^[92m)
echo [96m 1)[37m[37m -g[90m^|[37m--goblin    [92m build goblins
echo [96m 1)[37m[37m    --rm        [92m build remote components
echo [96m 2)[37m -s[90m^|[37m--sign      [92m sign Windows package binaries
echo [96m 2)[37m -k[90m=[37mPKGDIR      [92m packaging directory ([37mpkg[92m)
echo.
echo [96m 1) zou      agent only (     .msbuildproj)
echo [96m 2) zou pack agent only (.pack.msbuildproj)
echo.
echo [92mAdvanced options:
echo [37m    -v             [92m display zou debug info
echo [37m    -#             [92m display %_zouCmd% script internal variables
echo [37m       --boost     [92m update [37mboost[92m nuget packages
echo [37m       --bl        [92m create build log ([37m.binlog[92m)
echo [37m       --pp        [92m create preprocessed XML (does not build)
echo.
echo [92mExamples:
echo [33m    %_zouCmd% [90mfoo[37m.sln                       [92m# build [37mRelease[90m^|[37mx86 [92mand [37mRelease[90m^|[37mx64
echo [33m    %_zouCmd% [90mfoo[37m.sln -px64                 [92m# build [37mRelease[90m^|[37mx64
echo [33m    %_zouCmd% [90mfoo[37m.sln -pAnyCPU              [92m# build [37mRelease[90m^|[37mAny CPU [92m(solution)
echo [33m    %_zouCmd% [90mfoo[37m.csproj -pAnyCPU           [92m# build [37mRelease[90m^|[37mAnyCPU  [92m(project)
echo [33m    %_zouCmd% [90mfoo[37m.msbuildproj -p-           [92m# build [37mRelease[90m^|[37m-       [92m(zou agent)[0m
echo [33m    %_zouCmd% [90mfoo[37m.sln -cDebug -pWin32       [92m# build [37mDebug[90m^|[37mWin32
echo [33m    %_zouCmd% [90mfoo[37m.sln [90m...[37m [33m'"[37m-p:Foo=Bar[33m"'    [92m# define/override MSBuild property [37mFoo[0m
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

rem process option aliases
if /i '%_arg%' == '-e' set _arg=--clean
if /i '%_arg%' == '-x' set _arg=--rebuild
if /i '%_arg%' == '-s' set _arg=--sign
if /i '%_arg%' == '-a' set _arg=--cross
if /i '%_arg%' == '-g' set _arg=--goblin


rem DRY_RUN '-n'
if /i '%_arg%' == '-n' (
  set _dryRun=true
  goto :Parse
)

rem CONFIGURATION : -c CONFIG
if /i '%_arg:~0,2%' == '-c' (
  set val=!_arg:~2!
  if '!val!' == '' (
    shift
    set val=%1
  )
  set _config=!val!
  goto :Parse
)

rem PLATFORMS : (-p PLATFORM | -p- )
if /i '%_arg:~0,2%' == '-p' (
  set val=!_arg:~2!
  if '!val!' == '' (
    shift
    set val=%1
  )
  if /i '!val!' == '-' (
    set _noPlatform=true
  ) else (
    if /i '!val!' == 'AnyCPU' (
      set _anyCpu=true
      if '!_platforms!' neq '' set _byRuntime=true
    ) else (
      if '!_platforms!' == '' (
        set _platforms=!val!
        if '!_anyCpu!' == 'true' set _byRuntime=true
      ) else (
        set _platforms=!_platforms! !val!
        set _byRuntime=true
      )
    )
  )
  goto :Parse
)

rem PKGDIR : -k PKGDIR
if /i '%_arg:~0,2%' == '-k' (
  set val=!_arg:~2!
  if '!val!' == '' (
    shift
    set val=%1
  )
  set _pkgDir=!val!
  goto :Parse
)

rem CLEAN : --clean
if /i '%_arg%' == '--clean' (
  set _clean=true
  set _build=false
  goto :Parse
)

rem REBUILD : -x
if /i '%_arg%' == '--rebuild' (
  set _clean=true
  set _build=true
  goto :Parse
)

rem SIGN : --sign
if /i '%_arg%' == '--sign' (
  set _sign=true
  goto :Parse
)

rem CROSS_BUILD : --cross
if /i '%_arg%' == '--cross' (
  set _crossBuild=true
  set _byRuntime=true
  goto :Parse
)

rem VERBOSE : -v
if /i '%_arg%' == '-v' (
  set _verbose=true
  goto :Parse
)

rem CPU_COUNT : -1
if /i '%_arg%' == '-1' (
  set _cpuCount=%_arg:~1%
  goto :Parse
)
rem DEBUG : -#
if /i '%_arg%' == '-#' (
  set _debug=true
  goto :Parse
)


if /i '%_arg%' == '--goblin' (
  set _goblin=true
  goto :Parse
)
if /i '%_arg%' == '--rm' (
  set _rome=true
  goto :Parse
)
if /i '%_arg%' == '--boost' (
  set _boost=true
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
  set _error=project not found: [97m%_project%[91m.
  goto :Error
)
if not '!_platforms!' == '' if not '!_noPlatform!' == '' (
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
if '%_config%'   == '' set _config=Release
if '%_build%'    == '' set _build=true
if '%_cpuCount%' == '' set _cpuCount=0
if '!_platforms!' == '' if '!_noPlatform!' == '' if '!_anyCpu!' neq 'true' (
  set _byRuntime=true
  set _platforms=x86 x64
)
rem Use "Any CPU" for solution
if '!_anyCpu!' == 'true' (
  if /i '!_projExt!' == '.sln' (set _anyCpu="Any CPU") else (set _anyCpu=AnyCPU)
  if '!_platforms!' == '' (set _platforms=!_anyCpu!) else (set _platforms=!_platforms! !_anyCpu!)
)

rem create options
set _opts=--nologo -v:m -nr:false
if /i '%_cpuCount%'   == '0'   (set _opts=%_opts% -m) else (set _opts=%_opts% -m:%_cpuCount%)
if /i '%_binlog%'     == 'true' set _opts=%_opts% -bl:%_projDir%%_projName%.binlog
if /i '%_preprocess%' == 'true' set _opts=%_opts% -pp:%_projDir%%_projName%.pp.xml

rem create properties
set _props=MaxCpuCount=%_cpuCount%

if '%_config%'     neq '' set _props=%_props%;Configuration=%_config%
if '%_sign%'       neq '' set _props=%_props%;Sign=%_sign%
if '%_byRuntime%'  neq '' set _props=%_props%;RedistByRuntime=%_byRuntime%
if '%_crossBuild%' neq '' set _props=%_props%;CrossBuild=%_crossBuild%
if '%_goblin%'     neq '' set _props=%_props%;BuildGoblins=%_goblin%
if '%_rome%'       neq '' set _props=%_props%;BuildRome=%_rome%
if '%_verbose%'    neq '' set _props=%_props%;RedistDebug=%_verbose%;ZouDebug=%_verbose%
if '%_pkgDir%'     neq '' set _props=%_props%;PkgDir=%_pkgDir%
if '%_boost%'      neq '' set _props=%_props%;BoostUpdate=%_boost%

rem create msbuild command
if /i '%_projExt%' == '.csproj' (set command=dotnet build) else (set command=msbuild)
set command=%command% %_opts% %_project% -p:%_props%

if '%_debug%' == 'true' (
  echo [33m[%_zouCmd%][90m _project    = %_project%[0m
  echo [33m[%_zouCmd%][90m _cpuCount   = %_cpuCount%[0m
  echo [33m[%_zouCmd%][90m _build      = %_build%[0m
  echo [33m[%_zouCmd%][90m _clean      = %_clean%[0m
  echo [33m[%_zouCmd%][90m _sign       = %_sign%[0m
  echo [33m[%_zouCmd%][90m _config     = %_config%[0m
  echo [33m[%_zouCmd%][90m _platforms  = !_platforms![0m
  echo [33m[%_zouCmd%][90m _noPlatform = !_noPlatform![0m
  echo [33m[%_zouCmd%][90m _anyCpu     = %_anyCpu%[0m
  echo [33m[%_zouCmd%][90m _byRuntime  = !_byRuntime![0m
  echo [33m[%_zouCmd%][90m _boost      = %_boost%[0m
  echo [33m[%_zouCmd%][90m _binlog     = %_binlog%[0m
  echo [33m[%_zouCmd%][90m _preprocess = %_preprocess%[0m
  echo [33m[%_zouCmd%][90m _args       = %_args%[0m
  echo [33m[%_zouCmd%][90m command     = %command%[0m
  exit /b
)

rem ################## _clean
if '%_clean%' == 'true' (
  for %%x in (x86 x64 Win32 bin obj pkg %pkgDir%) do (
    rem avoid to delete root folder
    set dirName=%%x
    set _firstChar=!dirName:~0,1!
    set _firstChar=!_firstChar:/=\!
    if '!firstchar!' neq '\' if exist %%x (
      if '%_dryRun%' == '' (
        echo [33m[%_zouCmd%] [96mRemoving %%x...[0m
        rmdir /S /Q %%x
      ) else (
        echo [33m[%_zouCmd%] [90mRemoving %%x...[0m
      )
    )
  )
  if '!_platforms!' == '' (
    echo [33m[%_zouCmd%] [96mCleaning...[0m
    echo [33m[%_zouCmd%] [90m%command% -t:Clean %_args%[0m
    if '%_dryRun%' == '' (
      %command% -t:Clean %_args%
      if !errorlevel! neq 0 exit /b !errorlevel!
    )
  ) else (
    for %%p in (%_platforms%) do (
      echo [33m[%_zouCmd%] [96mCleaning %%p...[0m
      echo [33m[%_zouCmd%] [90m%command% -p:Platform=%%p -t:Clean %_args%[0m
      if '%_dryRun%' == '' (
        %command% -p:Platform=%%p -t:Clean %_args%
        if !errorlevel! neq 0 exit /b !errorlevel!
      )
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
    echo [33m[%_zouCmd%][96m !message!...[0m
    echo [33m[%_zouCmd%][90m %command% -t:Build %_args%[0m
    if '%_dryRun%' == '' (
      %command% -t:Build %_args%
      if !errorlevel! neq 0 exit /b !errorlevel!
    )
  ) else (
    for %%p in (%_platforms%) do (
      set message=Packing %%p to '!_pkgDir!' folder
      if /I '!projNoPack!' == '!_project!' set message=Building %%p
      echo [33m[%_zouCmd%][96m !message!...[0m
      echo [33m[%_zouCmd%][90m %command% -t:Build -p:Platform=%%p %_args%[0m
      if '%_dryRun%' == '' (
        %command% -t:Build -p:Platform=%%p %_args%
        if !errorlevel! neq 0 exit /b !errorlevel!
      )
    )
  )
)
echo.[0m
