@echo off
setlocal EnableDelayedExpansion

goto Parse

:Error
  echo.
	echo [31m%error%[0m
  set exitcode=1
  goto :Help

:Help

echo [36m
echo Usage:
echo   pack [OPTIONS] [PROJECT-NAME]
echo.
echo Options:
echo     -h             -- display current help
echo     -n             -- dry run
echo     -p PLATFORM    -- ^(x86^|x64^)
echo     -c             -- clean only
echo     -b             -- build only
echo     -a             -- build for Windows, OSX and Linux
echo     -m             -- build remote components
echo     -s             -- sign components
echo     -v             -- display MSBuild commands
echo [0m
exit /b %exitcode%

:Parse

if '%1' == '' goto EndParse
set arg=%1
shift

:: 'OPTIONS'
set opt=

if /i '%arg:~0,2%' == '-h' goto Help
if /i '%arg:~0,2%' == '/h' goto Help
if /i '%arg:~0,2%' == '-?' goto Help
if /i '%arg:~0,2%' == '/?' goto Help

if /i '%arg:~0,2%' == '-p' (
  set opt=true
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
if /i '%arg%' == '-c' (
  set opt=true
  set clean=true
  goto :Parse
)
if /i '%arg%' == '-b' (
  set opt=true
  set build=true
  goto :Parse
)
if /i '%arg%' == '-s' (
  set opt=true
  set sign=true
  goto :Parse
)
if /i '%arg%' == '-m' (
  set opt=true
  set rome=true
  goto :Parse
)
if /i '%arg%' == '-a' (
  set opt=true
  set all=true
  goto :Parse
)
if /i '%arg%' == '-v' (
  set opt=true
  set verbose=true
  goto :Parse
)
if /i '%arg%' == '-t' (
  set opt=true
  set test=true
  goto :Parse
)
if /i '%arg%' == '-n' (
  set opt=true
  set dry_run=true
  goto :Parse
)
if /i '%arg:~0,1%' == '-' (
  set error=Unknown option: '%arg%%arg%'.
  goto :Error
)

:: 'PROJECT_NAME'
if '%opt%' == '' (
  set project=%arg%
)
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

:: set defaults
if '!platforms!' == '' set platforms=x86 x64
if '%clean%' == '' if '%build%' == '' (
  set clean=true
  set build=true
)
set props=Configuration=Release
if '%all%'     == 'true' set props=%props%;CrossBuild=true
if '%rome%'    == 'true' set props=%props%;BuildRome=true
if '%sign%'    == 'true' set props=%props%;Sign=true
if '%verbose%' == 'true' set props=%props%;DisplayMSBuildCommand=true
set command=msbuild %project% -nologo -v:m -p:%props%

if '%test%' == 'true' (
  echo [90m  platforms = !platforms![0m
  echo [90m  project   = %project%[0m
  echo [90m  build     = %build%[0m
  echo [90m  clean     = %clean%[0m
  exit /b
)

:: clean
if '%clean%' == 'true' (
  for %%x in (bin\ pkg\) do (
    if exist %%x (
      echo [36m  Removing %%x...[0m
      if '%dry_run%' == '' rmdir /Q /S %%x
    )
  )
  for %%p in (%platforms%) do (
	  echo [36m  Cleaning %%p...[0m

    echo [90m  %command% -p:Platform=%%p -t:Clean[0m
	  if '%dry_run%' == '' (
      %command% -p:Platform=%%p -t:Clean
  	  if !errorlevel! neq 0 exit /b !errorlevel!
    )
  )
)

:: build
if '%build%' == 'true' (
  for %%p in (%platforms%) do (
	  echo [36m  Packing %%p to 'pkg' folder...[0m
    echo [90m  %command% -p:Platform=%%p[0m
	  if '%dry_run%' == '' (
      %command% -p:Platform=%%p
	    if !errorlevel! neq 0 exit /b !errorlevel!
    )
  )
)
