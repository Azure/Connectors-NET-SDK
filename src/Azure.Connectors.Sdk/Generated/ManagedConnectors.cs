// Connectors SDK - Generated Connectors
// Each connector client is used independently:
//
//   using Azure.Connectors.Sdk.Arm;
//   var client = new ArmClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Azureblob;
//   var client = new AzureblobClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Azuremonitorlogs;
//   var client = new AzuremonitorlogsClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Kusto;
//   var client = new KustoClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Mq;
//   var client = new MqClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Msgraphgroupsanduser;
//   var client = new MsgraphgroupsanduserClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Office365;
//   var client = new Office365Client(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Office365users;
//   var client = new Office365usersClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Onedriveforbusiness;
//   var client = new OnedriveforbusinessClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Sharepointonline;
//   var client = new SharepointonlineClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Smtp;
//   var client = new SmtpClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Teams;
//   var client = new TeamsClient(connectionRuntimeUrl);

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Azure.Connectors.Sdk;

/// <summary>
/// Provides a list of available SDK connectors.
/// </summary>
public static class SdkConnectors
{
    /// <summary>
    /// The list of available connector names.
    /// </summary>
    public static readonly string[] AvailableConnectors = [
        "arm",
        "azureblob",
        "azuremonitorlogs",
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
