# Copilot Instructions for azure-logicapps-connector-sdk

## Overview

This repository contains the lightweight SDK for Azure Logic Apps connectors. Code must follow the team's coding conventions based on BPM repo standards.

## Quick Reference: Coding Style Rules

### File Structure

```csharp
//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using Azure.Connectors.Sdk;

namespace Azure.Connectors.Sdk
{
    public class YourClass
    {
    }
}
```

**Rules:**

- Copyright header: Use `//----` (4 dashes) format with double space before "All rights reserved"
- Usings OUTSIDE namespace (standard C# convention)
- Usings sorted: System.* first, then alphabetically
- No empty lines between using groups

### Naming and Qualification

| Element | Rule | Example |
|---------|------|---------|
| Static members | Qualify with class name | `MyClass.StaticMethod()` |
| Instance members | Qualify with `this.` | `this.instanceField` |
| Private fields | Use `_camelCase` | `private readonly string _connectionString;` |
| Constants | Qualify with class name | `MyClass.DefaultTimeout` |
| Local variables | Use complete English terms | `parameter` not `p`, `method` not `m` |
| Lambda parameters | Use descriptive names | `methods.Where(method => ...)` not `m => ...` |

**Variable naming rules:**

- Use complete, unabbreviated English terms for all identifiers
- No single-letter variable names, even in lambdas (use `arg`, `item`, `method`, `parameter`)
- No placeholder names (`blah`, `foo`, `temp`, `x`) — always use meaningful names

### File Organization

**One type per file:**

- Declare only one class, struct, enum, or interface per file
- File name must match the type name
- Exception: Nested types (e.g., private helper classes) are allowed within the containing type

### Async/Await Format

**ALWAYS use this multi-line format:**

```csharp
var result = await this.httpClient
    .GetAsync(requestUri)
    .ConfigureAwait(continueOnCapturedContext: false);
```

**Rules:**

- Period on NEW line, not end of previous line
- Arguments indented ONE level (4 spaces)
- ALWAYS use `ConfigureAwait(continueOnCapturedContext: false)` with explicit parameter name
- Exception: Skip await for lone return statement (unless inside `using` block)

**DO NOT:**

```csharp
// Wrong: ConfigureAwait without named parameter
.ConfigureAwait(false);

// Wrong: Method call on same line as object
var result = await this.httpClient.GetAsync(requestUri)
    .ConfigureAwait(continueOnCapturedContext: false);

// Wrong: Dot at end of line
var result = await this.httpClient.
    GetAsync(requestUri);
```

### Method Calls with Multiple Parameters

**Single line** if all parameters fit:

```csharp
this.DoSomething(param1, param2);
```

**Boolean parameters MUST always use named arguments:**

```csharp
// Correct
IdentifierNormalizer.Normalize(name, isVariableName: true);
this.CreateNode(schema, isRequired: false);

// Wrong - unnamed boolean is ambiguous
IdentifierNormalizer.Normalize(name, true);
this.CreateNode(schema, false);
```

**Multi-line with named parameters:**

```csharp
return await this
    .ProcessRequestAsync(
        requestUri: uri,
        content: payload,
        cancellationToken: cancellationToken)
    .ConfigureAwait(continueOnCapturedContext: false);
```

**Rules:**

- Method name on new line after object
- Each parameter on its own line with name
- Opening paren stays with method name
- Closing paren aligns with parameter indent

### Comments

**Inline comments - use NOTE format:**

```csharp
// NOTE(username): Explanation of why this code exists.
var result = DoSomething();
```

**Rules:**

- Empty line ABOVE comment (unless first line in block)
- NO empty line between comment and code it describes
- Prefix: `// NOTE(username):` where username is your GitHub username
- Do NOT comment on the 'what' unless the code is obscure; instead comment on the 'why' when appropriate

**XML documentation - required for all public APIs:**

```csharp
/// <summary>
/// Processes the incoming request and returns the result.
/// </summary>
/// <param name="request">The request to process.</param>
public async Task<Response> ProcessAsync(Request request)
```

**Rules:**

- End descriptions with period
- Do NOT document return values (`<returns>` tag)
- Use `<see cref="ClassName"/>` for type references

### Exception Handling

```csharp
try
{
    await this
        .DoWorkAsync()
        .ConfigureAwait(continueOnCapturedContext: false);
}
catch (SpecificException ex)
{
    this.logger.LogError(ex, "Failed: '{Message}'.", ex.Message);
    throw;
}
catch (Exception ex) when (!ex.IsFatal())
{
    throw new InvalidOperationException(message: "Operation failed.", innerException: ex);
}
```

**Rules:**

- Exception variable name: `ex` (not `exception`)
- Use exception filter `when (!ex.IsFatal())` for general catches to avoid catching fatal exceptions
- Wrap inserted values in single quotes in error messages
- End error messages with period
- **All exceptions must have descriptive messages** — never throw exceptions without context

**DO:**

```csharp
throw new ArgumentException(message: "Parameter 'connectionId' cannot be null or empty.", paramName: nameof(connectionId));
throw new InvalidOperationException(message: $"Operation '{operationId}' is not supported.");
```

**DO NOT:**

```csharp
throw new ArgumentException();  // No message
throw new InvalidOperationException("error");  // Non-descriptive
```

### String Comparison

**ALWAYS use StringComparison:**

```csharp
// Correct
string.Equals(str1, str2, StringComparison.OrdinalIgnoreCase)
str.StartsWith(prefix, StringComparison.Ordinal)
```

**DO NOT:**

```csharp
str1 == str2
str1.Equals(str2)
```

### Spacing and Braces

**Empty line after closing brace:**

```csharp
if (condition)
{
    DoSomething();
}

DoSomethingElse();  // Empty line above
```

**NO empty line before closing brace:**

```csharp
// Wrong
if (condition)
{
    DoSomething();

}
```

**Switch statements - empty line between cases:**

```csharp
switch (value)
{
    case "A":
        HandleA();
        break;

    case "B":
        HandleB();
        break;

    default:
        throw new InvalidOperationException();
}
```

### Variable Declaration

**Use `var` when type is obvious:**

```csharp
var items = new List<string>();
var response = await this.GetResponseAsync();
```

**Use explicit type for null initialization:**

```csharp
byte[] buffer = null;  // Not: var buffer = (byte[])null;
```

### Ternary Operators

**Put `?` and `:` at START of new line:**

```csharp
var result = condition
    ? valueIfTrue
    : valueIfFalse;
```

### Logical Operators

**Put `||` and `&&` at END of line:**

```csharp
if (string.IsNullOrEmpty(value1) ||
    string.IsNullOrEmpty(value2) ||
    string.IsNullOrEmpty(value3))
```

**Return statements - first condition on new line:**

```csharp
return
    string.IsNullOrEmpty(value1) ||
    string.IsNullOrEmpty(value2);
```

### Access Modifiers

**ALWAYS explicit - order: access, static, readonly, other:**

```csharp
public static readonly string DefaultValue = "default";
private readonly ILogger _logger;
internal async Task ProcessAsync()
```

### Class Layout Order

1. Constants
2. Static fields
3. Instance fields
4. Constructors
5. Properties
6. Public methods
7. Internal methods
8. Private methods

Within each group: public → internal → private

## Patterns to Avoid

| Anti-Pattern | Correct Pattern |
|--------------|-----------------|
| `.Result` on Task | `await task.ConfigureAwait(continueOnCapturedContext: false)` |
| `.Wait()` on Task | `await task.ConfigureAwait(continueOnCapturedContext: false)` |
| `Task.Run()` for I/O | `await` the async method directly |
| `new Exception("msg.")` | `new SpecificException(message: "msg.")` |
| Magic numbers | Named constants (e.g., `MyClass.DefaultTimeoutSeconds`) |
| Magic strings (e.g., `"type"`, `"object"`) | Named constants (e.g., `SchemaPropertyNames.Type`) |
| `[0]` or `.First()` | `.Single()` (or `.SingleOrDefault()` + explicit validation) |

## Testing

```csharp
[TestMethod]
public async Task MethodName_Scenario_ExpectedResult()
{
    // Arrange
    var input = CreateTestInput();

    // Act
    var result = await this.service
        .ProcessAsync(input)
        .ConfigureAwait(continueOnCapturedContext: false);

    // Assert
    Assert.IsNotNull(result);
}
```

**Rules:**

- Test method naming: `MethodName_Scenario_ExpectedResult`
- Use async/await, never `.Result`
- Use `ConfigureAwait(continueOnCapturedContext: false)` in tests too

## Git Workflow

- Branch naming: `feature/description`, `fix/description`, `docs/description`
- Never push directly to main
- Always create PR for review

## Adding a New Connector

See [GENERATION.md](../GENERATION.md) for how to run the CodefulSdkGenerator.

### Steps

1. Generate: `LogicAppsCompiler <outputDir> unused --directClient --connectors=<connectorName>`
2. Copy generated `{Connector}Extensions.cs` to `src/Azure.Connectors.Sdk/Generated/`
3. Update `ConnectorNames.cs` — add constant in alphabetical order
4. Update `ManagedConnectors.cs` — add entry in alphabetical order
5. Add unit tests following existing pattern (constructor, dispose, mocked API, error handling, serialization round-trips)
6. Run all tests: `dotnet test` must pass with zero failures
   - `ConnectorNames_AllConstantsAreRegistered` test validates sync between ConnectorNames and ManagedConnectors
7. Update the validated connectors table in `README.md`
8. Update the connector names list in `.github/skills/connection-setup/SKILL.md`
9. Update `CHANGELOG.md` under `## [Unreleased]` / `### Added`

### PR checklist for new connector PRs

A complete PR for adding connector client(s) must include:

| File | Change |
|------|--------|
| `src/.../Generated/{Connector}Extensions.cs` | Generated code from CodefulSdkGenerator (never hand-edit) |
| `src/.../ConnectorNames.cs` | New constant(s) in alphabetical order |
| `src/.../ManagedConnectors.cs` | New registration(s) in alphabetical order |
| `tests/.../{Connector}ClientTests.cs` | Unit tests: constructor, dispose, mocked API, error, serialization |
| `README.md` | Connector count updated + new row(s) in validated connectors table |
| `.github/skills/connection-setup/SKILL.md` | Connector API name added to supported names list |
| `CHANGELOG.md` | Entry under `## [Unreleased]` / `### Added` |

**Verification before opening PR:**
```powershell
dotnet build --nologo      # 0 errors, 0 warnings
dotnet test --nologo       # all tests pass (including ConnectorNames_AllConstantsAreRegistered)
dotnet format --verify-no-changes  # clean formatting
```

## Releasing a New Version

The NuGet package version is defined in `eng/build/Version.props` (`VersionPrefix` + `VersionSuffix`). The ADO pipeline infrastructure handles building, signing, and publishing.

### Pipeline architecture

Three pipelines run in sequence in `azfunc/internal`:

| Pipeline | ID | Trigger | Purpose |
|----------|-----|---------|----------|
| `connectors-sdk.code-mirror` | 1717 | Auto on `release/*`, `v*` tags pushed to GitHub | Mirrors GitHub → internal ADO repo |
| `connectors-sdk.official` | 1718 | Auto after mirror lands (on `release/*`, `v*` tags) | Builds, tests, signs, produces `.nupkg` artifact |
| `connectors-sdk.release` | 1719 | **Manual** — run after official-build succeeds | Downloads artifact, validates, publishes to nuget.org |

### Version suffixes and `PublicRelease`

The build appends suffixes based on context (see `eng/build/Version.targets` and `Release.props`):

| Build source | `PublicRelease` | Package version example |
|--------------|----------------|------------------------|
| `refs/tags/v*` | `true` | `0.10.0-preview.1` (clean) |
| `refs/heads/release/*` | `true` (intended) | `0.10.0-preview.1` (clean) |
| Any other CI build | `false` | `0.10.0-preview.1.ci.26261.7` (has `.ci.` suffix) |
| Local dev | N/A | `0.10.0-preview.1.dev` |

**Only clean packages (no `.ci.`/`.dev.`/`.pr.` suffix) pass the release pipeline validation gate.**

### Release steps

1. Create `release/v{version}` branch with version bump in `Version.props`, finalized `CHANGELOG.md`, updated `README.md`
2. Push the release branch: `git push origin release/v{version}`
3. Tag and push: `git tag v{version} && git push origin v{version}`
4. Create GitHub Release: `gh release create v{version} --title "v{version}" --prerelease --notes "..."`
5. Wait for `code-mirror` (1717) to complete for both the branch and tag
6. Verify `official-build` (1718) runs automatically from the tag — check it produced a clean `.nupkg` (no `.ci.` suffix)
7. If the tag build is not the latest, re-queue it: `az pipelines run --org $org --project internal --id 1718 --branch "refs/tags/v{version}"`
8. **Run the release pipeline from `main`:**
   ```powershell
   az pipelines run --org "https://dev.azure.com/azfunc" --project "internal" --id 1719 --branch "main" --parameters "isReleaseBranchOrTag=True" "publishToNugetOrg=True" --output json | ConvertFrom-Json | Select-Object id, status
   ```
   The release pipeline picks up the **latest** `connectors-sdk.official` artifact. It must be the clean tag build.
9. Approve the release gate (approvers: David Burg, Rama Krishna Rayudu, Varad Meru's Team)

### Critical: release pipeline must run from `main`

**DO:** `az pipelines run ... --branch "main" --parameters "isReleaseBranchOrTag=True" "publishToNugetOrg=True"`

**DO NOT:** `az pipelines run ... --branch "release/v{version}"` or `--branch "refs/tags/v{version}"` — these fail with 0 timeline records (template validation failure).

The release pipeline's `self` repo reference doesn't resolve on tag refs, and running from `release/*` branches silently picks up the wrong artifact. The proven pattern (verified across v0.8.0, v0.9.0, v0.10.0) is to **always run from `main`**.

### GitHub authentication for push

The `Azure/Connectors-NET-SDK` repo requires a personal GitHub account with push access to the Azure org (not an EMU `*_microsoft` account). If push fails with 403:
```powershell
gh auth status                      # check which account is active
gh auth switch --user <personal>    # switch to the account with push access
git push origin release/v{version}
git push origin v{version}
```

### ADO Pipeline IDs

| Pipeline | ID | Org/Project | Purpose |
|----------|-----|-------------|---------|
| `connectors-sdk.code-mirror` | 1717 | `azfunc/internal` | Syncs GitHub → ADO mirror (triggers on `main`, `release/*`, and `v*` tags) |
| `connectors-sdk.official` | 1718 | `azfunc/internal` | Official build (triggers on `main`, `release/*`, and `v*` tags) |
| `connectors-sdk.release` | 1719 | `azfunc/internal` | NuGet.org publish (manual trigger, consumes official build artifacts) |
| `connectors-sdk.public` | 1716 | `azfunc/public` | PR validation CI |

Pipeline folder: [`\azure\connectors-sdk`](https://dev.azure.com/azfunc/internal/_build?definitionScope=%5Cazure%5Cconnectors-sdk)

### Release Steps

#### 1. Version bump PR

Create a PR that updates `VersionPrefix` and `CHANGELOG.md`:

```text
eng/build/Version.props   → <VersionPrefix>X.Y.Z</VersionPrefix>
                          → <VersionSuffix>preview.N</VersionSuffix> (if changing suffix)
CHANGELOG.md              → Add ## [X.Y.Z-preview.N] section with changes
                          → Add link definition at bottom
                          → Update [Unreleased] compare link
```

The package version is composed as `VersionPrefix-VersionSuffix.BuildReason[.BuildNumber.BuildCounter]`.
Release builds from tags strip the `BuildReason` segment, producing `X.Y.Z-preview.N`.

#### 2. Create tag and release branch

From the merged `main`:

```shell
git checkout main
git pull origin main
git tag vX.Y.Z-preview.N
git push origin vX.Y.Z-preview.N
git checkout -b release/vX.Y.Z-preview.N
git push origin release/vX.Y.Z-preview.N
```

Both the tag and the release branch are needed. The tag determines the package version (no build-reason suffix). The release branch exists for traceability. The code mirror pipeline triggers on `main`, `release/*` branches, and `v*` tags, so both refs sync automatically.

#### 3. Code mirror syncs to ADO

The code mirror pipeline (1717) triggers on `main`, `release/*` branches, and `v*` tags — so pushing the tag and release branch in step 2 automatically syncs them to the ADO mirror repo. If it doesn't trigger, queue a manual run:

```powershell
az pipelines run --id 1717 --org https://dev.azure.com/azfunc --project internal --branch main
```

#### 4. Official build runs

The official build pipeline (1718) triggers automatically on `v*` tags in the ADO mirror. If it doesn't trigger, queue it manually:

```powershell
az pipelines run --id 1718 --org https://dev.azure.com/azfunc --project internal --branch refs/tags/vX.Y.Z-preview.N
```

Wait for it to complete. The build produces `Azure.Connectors.Sdk.X.Y.Z-preview.N.nupkg`.

#### 5. Run the release pipeline

Queue the release pipeline (1719) with parameters to publish to nuget.org:

```powershell
az pipelines run --id 1719 --org https://dev.azure.com/azfunc --project internal \
  --branch refs/tags/vX.Y.Z-preview.N \
  --parameters isReleaseBranchOrTag=true publishToNugetOrg=true
```

The release pipeline:

1. **Prepare stage** — downloads the `.nupkg` from the official build, validates the version has no `dev`/`ci`/`pr` keywords
2. **Release stage** — requires human approval (resource-based check), then pushes to nuget.org

#### 6. Create GitHub Release

After the NuGet package is published:

```shell
gh release create vX.Y.Z-preview.N --title "vX.Y.Z-preview.N" --notes "Release notes here" --prerelease
```

#### 7. Verify

```powershell
# Check nuget.org (may take a few minutes to propagate)
Invoke-RestMethod "https://api.nuget.org/v3-flatcontainer/microsoft.azure.connectors.sdk/index.json" |
  Select-Object -ExpandProperty versions | Select-Object -Last 3
```

### Version Scheme

| Build source | Package version | Published to |
|-------------|----------------|-------------|
| Dev (local) | `X.Y.Z-preview.N.dev` | Not published |
| CI (`main` push) | `X.Y.Z-preview.N.ci.YYMMDD.N` | Internal feed |
| PR | `X.Y.Z-preview.N.pr.YYMMDD.N` | Not published |
| Tag/release branch | `X.Y.Z-preview.N` | nuget.org |

### Re-releasing

If the release pipeline fails after the official build succeeds, re-run pipeline 1719 pointing to the same build. If the package was already pushed to nuget.org, NuGet's `--skip-duplicate` prevents overwrites — use a new version number for corrections.

### What the Release Workflow Does

1. Builds and tests in Release configuration
2. Packs with `PackageVersion` from the tag (strips the `v` prefix)
3. Uploads `.nupkg` as a build artifact
4. Pushes to GitHub Packages (`nuget.pkg.github.com/Azure`)
5. Attempts push to nuget.org (requires `NUGET_API_KEY` secret, continues on error)
6. Creates a GitHub Release with the `.nupkg` attached (tag push only)

## Adding a New Connector

When adding a new generated connector client to the SDK:

### Steps

1. **Generate the code** using the CodefulSdkGenerator CLI from the BPM repo:

   ```shell
   LogicAppsCompiler <outputDir> unused --directClient --connectors=<connector-name>
   ```

2. **Copy the generated file** (`{Connector}Extensions.cs`) to `src/Azure.Connectors.Sdk/Generated/`
3. **Update `ConnectorNames.cs`** — add the new connector constant in alphabetical order
4. **Update `ManagedConnectors.cs`** — add the connector name to `AvailableConnectors` and a usage example in the header comment, both in alphabetical order
5. **Add unit tests** — create `{Connector}ClientTests.cs` in `tests/Azure.Connectors.Sdk.Tests/` following the pattern of existing tests (constructor, dispose, mocked API call, error handling, serialization round-trips)
6. **Update `ROADMAP.md`** — mark the connector as complete in the appropriate phase
7. **Update the connection setup skill** — add the connector's API name to the supported list in `.github/skills/connection-setup/SKILL.md` (Step 2)
8. **Run all tests** — `dotnet test` must pass with zero failures before committing
9. **Create a PR** — reference the GitHub issue (e.g., `Closes #9`)

### Validation checklist

- [ ] Generated file compiles without errors
- [ ] Existing connector tests still pass (no regressions)
- [ ] `ConnectorNames_AllConstantsAreRegistered` test passes (ConnectorNames ↔ ManagedConnectors sync)
- [ ] New connector tests cover: constructor, null URL, dispose, mocked success, mocked error, exception properties, type serialization round-trips
- [ ] If the connector uses `[DynamicSchema]`, tests verify the attribute is present on the type
