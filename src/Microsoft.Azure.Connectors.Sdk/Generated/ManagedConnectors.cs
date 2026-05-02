// Connectors SDK - Generated Connectors
// Each connector client is used independently:
//
//   using Microsoft.Azure.Connectors.Sdk.Azureblob;
//   var client = new AzureblobClient(connectionRuntimeUrl);
//   using Microsoft.Azure.Connectors.Sdk.Azureloganalytics;
//   var client = new AzureloganalyticsClient(connectionRuntimeUrl);
//   using Microsoft.Azure.Connectors.Sdk.Kusto;
//   var client = new KustoClient(connectionRuntimeUrl);
//   using Microsoft.Azure.Connectors.Sdk.Mq;
//   var client = new MqClient(connectionRuntimeUrl);
//   using Microsoft.Azure.Connectors.Sdk.Msgraphgroupsanduser;
//   var client = new MsgraphgroupsanduserClient(connectionRuntimeUrl);
//   using Microsoft.Azure.Connectors.Sdk.Office365;
//   var client = new Office365Client(connectionRuntimeUrl);
//   using Microsoft.Azure.Connectors.Sdk.Office365users;
//   var client = new Office365usersClient(connectionRuntimeUrl);
//   using Microsoft.Azure.Connectors.Sdk.Onedriveforbusiness;
//   var client = new OnedriveforbusinessClient(connectionRuntimeUrl);
//   using Microsoft.Azure.Connectors.Sdk.Sharepointonline;
//   var client = new SharepointonlineClient(connectionRuntimeUrl);
//   using Microsoft.Azure.Connectors.Sdk.Smtp;
//   var client = new SmtpClient(connectionRuntimeUrl);
//   using Microsoft.Azure.Connectors.Sdk.Teams;
//   var client = new TeamsClient(connectionRuntimeUrl);

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Microsoft.Azure.Connectors.Sdk;

/// <summary>
/// Provides a list of available SDK connectors.
/// </summary>
public static class SdkConnectors
{
    /// <summary>
    /// The list of available connector names.
    /// </summary>
    public static readonly string[] AvailableConnectors = [
        "azureblob",
        "azureloganalytics",
        "kusto",
        "mq",
        "msgraphgroupsanduser",
        "office365",
        "office365users",
        "onedriveforbusiness",
        "sharepointonline",
        "smtp",
        "teams",
    ];
}
