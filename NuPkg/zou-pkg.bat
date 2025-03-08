@echo off

for %%x in (net6 net8 net9) do (
  echo [0;1;4mRestoring [92m%%x[37m packages[0m
  
  dotnet restore NuPkg.csproj --tl:off /p:CoreTargetFramework=%%x
  dotnet list NuPkg.csproj package --format json >pkg-%%x.json
  echo   Generated resolved versions listing -^> [92mpkg-%%x.json[0m
  dotnet list NuPkg.csproj package --format json --outdated >pkg-%%x-outdated.json
  echo   Generated outdated versions listing -^> [92mpkg-%%x-outdated.json[0m
)