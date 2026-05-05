// DirectClient SDK - Generated Connectors
// Each connector client is used independently:
//
//   using Microsoft.Azure.Connectors.DirectClient.Azureblob;
//   var client = new AzureblobClient(connectionRuntimeUrl);
//   using Microsoft.Azure.Connectors.DirectClient.Kusto;
//   var client = new KustoClient(connectionRuntimeUrl);
//   using Microsoft.Azure.Connectors.DirectClient.Mq;
//   var client = new MqClient(connectionRuntimeUrl);
//   using Microsoft.Azure.Connectors.DirectClient.Msgraphgroupsanduser;
//   var client = new MsgraphgroupsanduserClient(connectionRuntimeUrl);
//   using Microsoft.Azure.Connectors.DirectClient.Office365;
//   var client = new Office365Client(connectionRuntimeUrl);
//   using Microsoft.Azure.Connectors.DirectClient.Office365users;
//   var client = new Office365usersClient(connectionRuntimeUrl);
//   using Microsoft.Azure.Connectors.DirectClient.Onedriveforbusiness;
//   var client = new OnedriveforbusinessClient(connectionRuntimeUrl);
//   using Microsoft.Azure.Connectors.DirectClient.Sharepointonline;
//   var client = new SharepointonlineClient(connectionRuntimeUrl);
//   using Microsoft.Azure.Connectors.DirectClient.Smtp;
//   var client = new SmtpClient(connectionRuntimeUrl);
//   using Microsoft.Azure.Connectors.DirectClient.Teams;
//   var client = new TeamsClient(connectionRuntimeUrl);

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Microsoft.Azure.Connectors.DirectClient;

/// <summary>
/// Provides a list of available DirectClient connectors.
/// </summary>
public static class DirectClientConnectors
{
    /// <summary>
    /// The list of available connector names.
    /// </summary>
    public static readonly string[] AvailableConnectors = [
        "azureblob",
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
