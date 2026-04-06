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
using Microsoft.Azure.Connectors.Sdk;

namespace Microsoft.Azure.Connectors.Sdk
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

## Releasing a New Version

The release workflow (`.github/workflows/release.yml`) builds, tests, packs, and publishes the NuGet package. There is no version file to update — the version comes from the git tag.

### Standard Release (tag push)

Creates a GitHub Release with auto-generated notes, publishes to GitHub Packages, and attempts nuget.org:

```shell
git checkout main
git pull origin main
git tag v1.2.3
git push origin v1.2.3
```

### Pre-release

Use SemVer pre-release suffixes:

```shell
git tag v1.2.3-preview.1
git push origin v1.2.3-preview.1
```

### Manual Dispatch (packages only, no GitHub Release)

Use when you need to publish without creating a tag or GitHub Release:

1. Go to Actions → Release → Run workflow
2. Enter the version (e.g., `1.2.3`)

### Re-releasing a Version

If a release fails midway (e.g., build passed but GitHub Release creation failed):

```shell
gh release delete v1.2.3 --yes     # delete the failed GitHub Release (if one was created)
git push origin --delete v1.2.3    # delete remote tag
git tag -d v1.2.3                  # delete local tag
git tag v1.2.3                     # re-tag on current HEAD
git push origin v1.2.3             # push to trigger release
```

**Note:** `--skip-duplicate` on the NuGet push steps means a re-release will not overwrite a package version that was already published. If the package was pushed successfully but the release failed, re-running will skip the duplicate package and only recreate the GitHub Release. To publish a corrected package, use a new version number.

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
   LogicAppsCompiler <outputDir> --directClient --connectors=<connector-name>
   ```
2. **Copy the generated file** (`{Connector}Extensions.cs`) to `src/Microsoft.Azure.Connectors.Sdk/Generated/`
3. **Update `ConnectorNames.cs`** — add the new connector constant in alphabetical order
4. **Update `ManagedConnectors.cs`** — add the connector name to `AvailableConnectors` and a usage example in the header comment, both in alphabetical order
5. **Add unit tests** — create `{Connector}ClientTests.cs` in `tests/Microsoft.Azure.Connectors.Sdk.Tests/` following the pattern of existing tests (constructor, dispose, mocked API call, error handling, serialization round-trips)
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
