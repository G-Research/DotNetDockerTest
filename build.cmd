echo OFF
dotnet build /p:Configuration=Release && (echo Build successful) || goto buildfailed

dotnet test /p:Configuration=Release --no-build && (echo Test successful) || goto testsfailed

%~dp0\test\TestDotNetDockerTestPackage\TestDotNetDockerTestPackage.cmd && (echo Integration test successful) || goto testsfailed

echo Completed Successfully

exit 0

:buildfailed
   echo Build Failed
   exit 1

:testsfailed
   echo Tests Failed
   exit 2