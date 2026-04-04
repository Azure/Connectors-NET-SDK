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

## NuGet Package

When publishing:
- Update version in `Directory.Build.props`
- Ensure all tests pass
- Update CHANGELOG.md with release notes

