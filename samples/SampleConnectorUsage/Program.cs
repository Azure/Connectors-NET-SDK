//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using Microsoft.Azure.Connectors.Sdk;
using Microsoft.Azure.Connectors.Sdk.Authentication;

namespace SampleConnectorUsage;

/// <summary>
/// Sample program demonstrating how to use the Connector SDK with generated connector clients.
/// 
/// This sample shows the usage pattern. For actual usage, you need to:
/// 1. Generate connector code using LogicAppsCompiler CLI (see GENERATION.md)
/// 2. Add the generated code to your project
/// 3. Use the generated typed client classes
/// </summary>
public class Program
{
    /// <summary>
    /// Entry point for the sample application.
    /// </summary>
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Azure Logic Apps Connector SDK - Sample Usage");
        Console.WriteLine("=============================================");
        Console.WriteLine();

        // Example 1: SDK Runtime Components
        Console.WriteLine("Example 1: SDK Runtime Components");
        Console.WriteLine("----------------------------------");
        Console.WriteLine("The SDK provides runtime infrastructure for generated connector clients:");
        Console.WriteLine();
        Console.WriteLine("  Authentication:");
        Console.WriteLine("    - ManagedIdentityTokenProvider: For Azure-hosted apps");
        Console.WriteLine("    - ConnectionStringTokenProvider: For local development");
        Console.WriteLine();
        Console.WriteLine("  HTTP:");
        Console.WriteLine("    - ConnectorHttpClient: HTTP client with retry and auth");
        Console.WriteLine("    - RetryPolicy: Configurable retry behavior");
        Console.WriteLine();
        Console.WriteLine("  Serialization:");
        Console.WriteLine("    - ConnectorJsonSerializer: JSON serialization helpers");
        Console.WriteLine();

        // Example 2: Token Provider Usage
        Console.WriteLine("Example 2: Token Provider Usage");
        Console.WriteLine("-------------------------------");

        try
        {
            // Managed Identity for Azure-hosted scenarios
            Console.WriteLine("  Managed Identity (for Azure-hosted apps):");
            Console.WriteLine("    var tokenProvider = new ManagedIdentityTokenProvider();");
            Console.WriteLine("    var token = await tokenProvider.GetAccessTokenAsync(scopes);");
            Console.WriteLine();

            // Connection String for local development
            var apiKey = Environment.GetEnvironmentVariable("CONNECTOR_API_KEY") ?? "demo-key";
            var connectionTokenProvider = new ConnectionStringTokenProvider(apiKey);
            Console.WriteLine("  Connection String (for local development):");
            Console.WriteLine("    var tokenProvider = new ConnectionStringTokenProvider(apiKey);");
            Console.WriteLine($"    Created with key: {apiKey.Substring(0, Math.Min(4, apiKey.Length))}...");
        }
        catch (Exception ex) when (!ex.IsFatal())
        {
            Console.WriteLine($"  Error: {ex.Message}");
        }

        Console.WriteLine();

        // Example 3: Generated Client Usage Pattern
        Console.WriteLine("Example 3: Generated Client Usage Pattern");
        Console.WriteLine("------------------------------------------");
        Console.WriteLine("After generating connector code with LogicAppsCompiler CLI:");
        Console.WriteLine();
        Console.WriteLine("  // Using generated Office365Client (from Office365Extensions.cs)");
        Console.WriteLine("  using Microsoft.Azure.Connectors.DirectClient.Office365;");
        Console.WriteLine();
        Console.WriteLine("  // Create the client with connection runtime URL");
        Console.WriteLine("  var connectionRuntimeUrl = \"https://...\"; // From Azure Portal");
        Console.WriteLine("  using var client = new Office365Client(connectionRuntimeUrl);");
        Console.WriteLine();
        Console.WriteLine("  // Call typed operations");
        Console.WriteLine("  await client.SendEmailV2Async(new SendEmailV2Input");
        Console.WriteLine("  {");
        Console.WriteLine("      To = \"recipient@example.com\",");
        Console.WriteLine("      Subject = \"Hello from SDK\",");
        Console.WriteLine("      Body = \"<p>Email body</p>\"");
        Console.WriteLine("  });");
        Console.WriteLine();
        Console.WriteLine("  var categories = await client.GetOutlookCategoryNamesAsync();");
        Console.WriteLine();

        // Example 4: Generation Instructions
        Console.WriteLine("Example 4: How to Generate Connector Code");
        Console.WriteLine("------------------------------------------");
        Console.WriteLine("Use the LogicAppsCompiler CLI from the BPM repository:");
        Console.WriteLine();
        Console.WriteLine("  # Build the generator");
        Console.WriteLine("  dotnet build .\\src\\tools\\CodefulSdkGenerator\\LogicAppsCompiler.Cli -c Release");
        Console.WriteLine();
        Console.WriteLine("  # Generate Office365 connector");
        Console.WriteLine("  LogicAppsCompiler.exe ./generated unused --directClient --connectors=office365");
        Console.WriteLine();
        Console.WriteLine("See GENERATION.md for complete documentation.");
        Console.WriteLine();

        // Example 5: Integration with Azure Functions
        Console.WriteLine("Example 5: Azure Functions Integration");
        Console.WriteLine("---------------------------------------");
        Console.WriteLine("The generated clients work well with Azure Functions:");
        Console.WriteLine();
        Console.WriteLine("  [Function(\"SendEmail\")]");
        Console.WriteLine("  public async Task<HttpResponseData> SendEmail(");
        Console.WriteLine("      [HttpTrigger(AuthorizationLevel.Function)] HttpRequestData req)");
        Console.WriteLine("  {");
        Console.WriteLine("      using var client = new Office365Client(_connectionUrl);");
        Console.WriteLine("      await client.SendEmailV2Async(new SendEmailV2Input { ... });");
        Console.WriteLine("      return req.CreateResponse(HttpStatusCode.OK);");
        Console.WriteLine("  }");
        Console.WriteLine();

        Console.WriteLine("Sample completed successfully!");
        Console.WriteLine();
        Console.WriteLine("Next steps:");
        Console.WriteLine("  1. Run LogicAppsCompiler CLI to generate connector code");
        Console.WriteLine("  2. Add generated code to your project");
        Console.WriteLine("  3. Use typed clients with the connection runtime URL from Azure Portal");

        await Task.CompletedTask
            .ConfigureAwait(continueOnCapturedContext: false);
    }
}

