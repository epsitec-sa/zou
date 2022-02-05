@echo off
set local

rem Clean build output
for %%x in (bin obj pkg) do (
  rmdir /S /Q %~dp0\%%x >nul 2>&1
)

rem Build debug version
rem The debug version of Tasks.dll has precedence over the release versions
rem because of the fallback implemented in zou.Tasks.props
dotnet build Tasks.csproj -c Debug

rem Clean actual tasks
rmdir /S /Q %~dp0\..\binz\tasks >nul 2>&1

rem Build and deploy the release versions
dotnet build --nologo -v:m Tasks.Pack.msbuildproj -p:Configuration=Release -p:Platform=x64 -p:CrossBuild=true

rem Clean build output
for %%x in (bin obj pkg) do (
  rmdir /S /Q %~dp0\%%x >nul 2>&1
)
