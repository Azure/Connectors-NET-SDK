# Generated Connector Code

Pre-generated connector clients are included in the SDK package at:
`src/Microsoft.Azure.Connectors.Sdk/Generated/`

This folder is available for users who want to generate additional connectors or custom connectors locally.

## Generating Additional Connectors

To generate connector SDKs not included in the package:

```powershell
# Build the generator from the BPM repository (internal)
dotnet build .\src\tools\CodefulSdkGenerator\LogicAppsCompiler.Cli\LogicAppsCompiler.Cli.csproj -c Release

# Generate connectors
$generatorPath = ".\src\tools\CodefulSdkGenerator\LogicAppsCompiler.Cli\bin\Release\LogicAppsCompiler.exe"
& $generatorPath "./output" unused --directClient --connectors=servicebus,teams
```

See [GENERATION.md](../../GENERATION.md) for complete documentation.
