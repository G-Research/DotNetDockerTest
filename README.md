# DotNetDockerTest

![.NET Core](https://github.com/LordNibbler274/DotNetDockerTest/workflows/.NET%20Core/badge.svg)

A library which enables the running of dotnet tests in a docker container

## Usage

1. Reference the nuget package. The easiest way to do this for a number of projects is to include a Directory.Build.targets file at the root of the repo containing:

```xml
<Project>
  <ItemGroup>
    <PackageReference Include="DotNetDockerTest" Version="0.0.1">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>build</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

2. Specify the a base docker image to use for testing:

```xml
<DockerTestImage>microsoft/dotnet:2.1.402-sdk-alpine3.7</DockerTestImage>
```

Then When running tests specify:

```powershell
dotnet test /p:TestOnDocker
```
