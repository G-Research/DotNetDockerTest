$ErrorActionPreference = 'Stop'

dotnet build /p:Configuration=Release; $? ? { echo "Build successful" } : {echo "Build Failed"; exit 1}

dotnet test /p:Configuration=Release --no-build; $? ? { echo "Test successful" } : {echo "Test Failed"; exit 2}

& $PSScriptRoot\test\TestDotNetDockerTestPackage\TestDotNetDockerTestPackage.cmd; $? ? { echo "Integration test successful" } : {echo "Integration test Failed"; exit 3}

echo "Completed Successfully"

exit 0
