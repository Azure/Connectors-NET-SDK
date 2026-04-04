# Governance

## Overview

Azure Connectors .NET SDK is maintained by the Azure Logic Apps team at Microsoft. This document describes how the project is governed and how decisions are made.

## Roles

### Maintainers

Maintainers have full commit access, can merge pull requests, manage releases, and configure repository settings.

| Role | Team |
|------|------|
| Maintainers | [@Azure/azure-logicapps-team](https://github.com/orgs/Azure/teams/azure-logicapps-team) |

### Contributors

Anyone who submits a pull request or files an issue. Contributors must agree to the [Contributor License Agreement (CLA)](https://cla.opensource.microsoft.com) before their first PR can be merged.

## Decision Making

- **Day-to-day decisions** (bug fixes, minor improvements) are made by any maintainer via PR review.
- **Significant changes** (new connectors, API surface changes, breaking changes) require review and approval from at least one maintainer.
- **Releases** are cut by maintainers using git tags (see [release instructions](README.md#releasing-a-new-version) and [CHANGELOG.md](CHANGELOG.md)).

## Contributions

All contributions follow the process described in [CONTRIBUTING.md](CONTRIBUTING.md). Pull requests require at least one maintainer approval before merging.

## Code of Conduct

This project follows the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).

## Security

Security vulnerabilities should be reported following the process in [SECURITY.md](SECURITY.md).
