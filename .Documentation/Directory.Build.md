# .NET SDK Project Customization

## Standard bundle layout example (cresus-dev + swissdec):
#### Execution order:
- run any swissdec project
  - import swissdec\Directory.Build.props
    - import swissdec\Version.props
      - define swissdec `AssemblyVersion`
    - import cresus-dev\Directory.Build.props
      - import zou\Directory.Build.Default.props
        - define missing versions (`Version`, `AssemblyFileVersion`, `FileVersion`)
      - define cresus-dev properties (Product, Company, Copyright)
  - define swissdec project properties
  - import cresus-dev\Directory.Build.targets
    - import zou\Directory.Build.Default.targets
      - define missing properties, check version...

### [[cresus-dev]()]
- #### [Directory.Build.props](https://git.epsitec.ch/cresus-suite/cresus-dev/blob/master/Directory.Build.props)
```
    <Project>
      <Import Project="zou\Directory.Build.Default.props" />
      <PropertyGroup>
        <Product>Crésus</Product>
        <Company>Epsitec</Company>
        <Copyright>Copyright © 2018, EPSITEC SA, CH-1400 Yverdon-les-Bains, Switzerland</Copyright>

        <PackageTags>epsitec, cresus</PackageTags>
        <PackageProjectUrl>https://git.epsitec.ch/cresus-suite/cresus-dev</PackageProjectUrl>
        <RepositoryType>git</RepositoryType>

        <LangVersion>latest</LangVersion>
        <Deterministic>true</Deterministic>
      </PropertyGroup>
    </Project>
```
- #### [Directory.Build.targets](https://git.epsitec.ch/cresus-suite/cresus-dev/blob/master/Directory.Build.targets)
```
    <Project>
      <Import Project="zou\Directory.Build.Default.targets" />
    </Project>
```

### [[swissdec](https://git.epsitec.ch/cresus-suite/swissdec)]
    
- #### [Directory.Build.props](https://git.epsitec.ch/cresus-suite/swissdec/blob/master/Directory.Build.props)
```
    <Project>
      <Import Project="Version.props" />
      <Import Project="$([MSBuild]::GetPathOfFileAbove('$(MSBuildThisFile)', '$(MSBuildThisFileDirectory)../'))" />
      <PropertyGroup>
        <Authors>Roger VUISTINER</Authors>
        <Product>Swissdec Transmitter</Product>
        <RootNamespace>Epsitec.Swissdec</RootNamespace>

        <PackageTags>$(PackageTags), swissdec</PackageTags>
        <PackageProjectUrl>https://git.epsitec.ch/cresus-suite/swissdec</PackageProjectUrl>
      </PropertyGroup>
    </Project>
```
- #### [Version.props](https://git.epsitec.ch/cresus-suite/swissdec/blob/master/Version.props)
```
    <Project>
      <PropertyGroup>
        <AssemblyVersion>5.4.0</AssemblyVersion>
      </PropertyGroup>
    </Project>
```

### [[zou](https://git.epsitec.ch/Build/zou)]
- #### [Directory.Build.Default.targets](https://git.epsitec.ch/Build/zou/blob/master/Directory.Build.Default.targets)
  
