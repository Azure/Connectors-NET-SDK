# Contributing to Azure Connectors .NET SDK

This project welcomes contributions and suggestions. Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit <https://cla.opensource.microsoft.com>.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download) pinned by `global.json` (currently `8.0.100`)
- [Git](https://git-scm.com/downloads)

### Building

```bash
dotnet restore
dotnet build
```

### Running Tests

```bash
dotnet test
```

## How to Contribute

1. Fork the repository
2. Create a topic branch from `main` (`git checkout -b feature/my-change`)
3. Make your changes
4. Run tests to ensure nothing is broken (`dotnet test`)
5. Commit your changes (`git commit -m "Add my change"`)
6. Push to your fork (`git push origin feature/my-change`)
7. Open a pull request against `main`

### Pull Request Guidelines

- Keep PRs focused on a single change
- Include tests for new functionality
- Update documentation if behavior changes
- Follow the existing code style (see [copilot-instructions.md](.github/copilot-instructions.md) for conventions)

### Reporting Issues

- Use [GitHub Issues](https://github.com/Azure/Connectors-NET-SDK/issues) to report bugs or request features
- Search existing issues before creating a new one
- Use the provided issue templates when available

## Code Style

This project follows the coding conventions documented in [.github/copilot-instructions.md](.github/copilot-instructions.md). Key points:

- Use `ConfigureAwait(continueOnCapturedContext: false)` on all async calls
- Qualify instance members with `this.`
- Use named arguments for boolean parameters
- Use `StringComparison` for all string comparisons

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
