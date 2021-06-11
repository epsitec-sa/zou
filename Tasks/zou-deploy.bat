@echo off

rem Clean build output
rmdir /S /Q %~dp0\bin >nul 2>&1
rmdir /S /Q %~dp0\obj >nul 2>&1
rmdir /S /Q %~dp0\pkg >nul 2>&1

rem Build debug version
rem The debug version of Tasks.dll has precedence over the release versions
rem because of the fallback implemented in zou.Tasks.props
dotnet build Tasks.csproj -c Debug

rem Clean actual tasks
rmdir /S /Q %~dp0\..\binz\tasks >nul 2>&1

rem Build and deploy the release versions
dotnet build Tasks.deploy.msbuildproj

rem Clean build output
rmdir /S /Q %~dp0\bin >nul 2>&1
rmdir /S /Q %~dp0\obj >nul 2>&1
rmdir /S /Q %~dp0\pkg >nul 2>&1

