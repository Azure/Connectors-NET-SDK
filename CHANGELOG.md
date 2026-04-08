# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.2.0-preview.1] - 2026-04-07

### Added

- Azure Data Explorer (Kusto) generated typed client (#37)
- PR template, governance doc, and CI code coverage (#36)
- Standard Microsoft OSS community files (#27)
- Dependabot version updates for NuGet and GitHub Actions (#26)
- Release instructions in README and copilot-instructions (#25)

### Changed

- Bump Microsoft.Extensions.Http and Microsoft.Extensions.Logging.Abstractions (#33)
- Bump Microsoft.NET.Test.Sdk from 17.14.1 to 18.3.0 (#35)
- Bump coverlet.collector from 6.0.4 to 8.0.1 (#32)
- Bump NuGet minor/patch dependencies (#31)
- Bump GitHub Actions: checkout v6.0.2, setup-dotnet v5.2.0, upload-artifact v7.0.0 (#28, #29, #30)
- Update cross-references to public Connectors-NET-Samples and LSP repos (#40)

## [0.1.0-preview.1] - 2025-12-19

### Added

- Initial SDK release with core abstractions (`ConnectorClientBase`, `IConnectorClient`, `ConnectorClientOptions`)
- Token providers: `ManagedIdentityTokenProvider`, `ConnectionStringTokenProvider`
- HTTP pipeline with configurable retry policies
- Office 365 connector client (generated)
- SharePoint connector client (generated)
- Teams connector client (generated)

[Unreleased]: https://github.com/Azure/Connectors-NET-SDK/compare/v0.2.0-preview.1...HEAD
[0.2.0-preview.1]: https://github.com/Azure/Connectors-NET-SDK/compare/v0.1.0-preview.1...v0.2.0-preview.1
[0.1.0-preview.1]: https://github.com/Azure/Connectors-NET-SDK/releases/tag/v0.1.0-preview.1
