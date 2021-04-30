@echo off

rem Clean
rmdir /S /Q %~dp0\bin >nul 2>&1
rmdir /S /Q %~dp0\obj >nul 2>&1
rmdir /S /Q %~dp0\pkg >nul 2>&1

rem Rebuild debug version
rem The debug version of Tasks.dll has precedence over the release versions
rem because of the fallback implemented in zou.Tasks.props
dotnet build Tasks.csproj -c Debug

rem Build and deploy the release versions
dotnet build Tasks.deploy.msbuildproj

rem Clean again
rmdir /S /Q %~dp0\bin >nul 2>&1
rmdir /S /Q %~dp0\obj >nul 2>&1
rmdir /S /Q %~dp0\pkg >nul 2>&1

