echo OFF

dotnet build %~dp0\TestDotNetDockerTestPackage.proj && (echo Build successful) || goto buildfailed

dotnet test %~dp0\TestDotNetDockerTestPackage.csproj --no-build --verbosity normal && (echo Test successful) || goto testfailed

echo Completed Successfully

exit 0

:buildfailed
   echo Build Failed
   exit 1

:testsfailed
   echo Tests Failed
   exit 2