# DotNetDockerTest

[![.NET Core](https://github.com/G-Research/DotNetDockerTest/workflows/.NET%20Core/badge.svg?branch=master&event=push)](https://github.com/G-Research/DotNetDockerTest/actions/workflows/dotnetcore.yml?query=branch%3Amaster+event%3Apush)

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
dotnet test /p:TestOnDocker=true
```

Or specify `TestOnDocker` msbuild property in your project file.
