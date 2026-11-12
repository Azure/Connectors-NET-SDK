# Migrate generated .NET connector names

This preview update corrects product casing and .NET identifier casing across the
shipped generated clients and the `ConnectorNames` catalog. These public API
renames are source-breaking: update `using` directives, DI registrations, client
constructors, model factories, and named arguments, then rebuild your app.

| Before | After |
| --- | --- |
| `using Azure.Connectors.Sdk.AzureIoTCentral;` | `using Azure.Connectors.Sdk.AzureIotCentral;` |
| `using Azure.Connectors.Sdk.AzureIoTCentral.Models;` | `using Azure.Connectors.Sdk.AzureIotCentral.Models;` |
| `AzureIoTCentralClient` / `AzureIoTCentralModelFactory` | `AzureIotCentralClient` / `AzureIotCentralModelFactory` |
| `services.AddAzureIoTCentralClient(configuration)` | `services.AddAzureIotCentralClient(configuration)` |
| `DocumentDbClient` / `DocumentDbModelFactory` | `DocumentDBClient` / `DocumentDBModelFactory` |
| `DocuSignClient` / `DocuSignModelFactory` | `DocusignClient` / `DocusignModelFactory` |
| `MailChimpClient` / `MailChimpModelFactory` | `MailchimpClient` / `MailchimpModelFactory` |
| `MsGraphGroupsAndUsersClient` | `MSGraphGroupsAndUsersClient` |
| `StarrezRestV1Client` | `StarRezRestV1Client` |
| `ConnectorNames.DataFlowsSMS` | `ConnectorNames.DataFlowsSms` |
| `ConnectorNames.MailChimp` | `ConnectorNames.Mailchimp` |
| `ConnectorNames.AzureIoTCentralV3` | `ConnectorNames.AzureIotCentralV3` |

For example, replace:

```csharp
using Azure.Connectors.Sdk.AzureIoTCentral;
using Azure.Connectors.Sdk.AzureIoTCentral.Models;

using var client = new AzureIoTCentralClient(connectionRuntimeUrl);
```

with:

```csharp
using Azure.Connectors.Sdk.AzureIotCentral;
using Azure.Connectors.Sdk.AzureIotCentral.Models;

using var client = new AzureIotCentralClient(connectionRuntimeUrl);
```

Model properties and factory parameter names also change when the old spelling
was inconsistent. For example, Teams `RecordingContentURL` becomes
`RecordingContentUrl`, while its JSON key stays `recordingContentUrl`.
DocumentDB `QueryDocumentsResponse.Documents` becomes `Value`; the serialized
key remains `value`. WordPress's second `Id2` property becomes `Guid`; the
`guid` wire key is unchanged. Check named-argument calls after upgrading;
Jira `ListIssuesAsync(jQLQuery: ...)` becomes `ListIssuesAsync(jqlQuery: ...)`.

Raw connector API IDs (for example `azureiotcentral` and `dataflowssms`),
operation IDs, HTTP routes, JSON/XML field names, and enum wire values are
independent of public identifier spelling. Do not change connection names or
serialized payload keys to match the new C# names. `ServiceBusConnectorClient`
retains its suffix to avoid a dependency type collision. The SDK remains
async-only and does not change `ETag` or URI-valued properties to framework
value types as part of this migration.
