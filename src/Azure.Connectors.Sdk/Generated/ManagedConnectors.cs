// Connectors SDK - Generated Connectors
// Each connector client is used independently:
//
//   using Azure.Connectors.Sdk.Arm;
//   using Azure.Connectors.Sdk.Arm.Models;
//   var client = new ArmClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.AzureBlob;
//   using Azure.Connectors.Sdk.AzureBlob.Models;
//   var client = new AzureBlobClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Azuremonitorlogs;
//   using Azure.Connectors.Sdk.Azuremonitorlogs.Models;
//   var client = new AzuremonitorlogsClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Kusto;
//   using Azure.Connectors.Sdk.Kusto.Models;
//   var client = new KustoClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Mq;
//   using Azure.Connectors.Sdk.Mq.Models;
//   var client = new MqClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.MsGraphGroupsAndUsers;
//   using Azure.Connectors.Sdk.MsGraphGroupsAndUsers.Models;
//   var client = new MsGraphGroupsAndUsersClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Office365;
//   using Azure.Connectors.Sdk.Office365.Models;
//   var client = new Office365Client(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Office365users;
//   using Azure.Connectors.Sdk.Office365users.Models;
//   var client = new Office365usersClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.OneDriveForBusiness;
//   using Azure.Connectors.Sdk.OneDriveForBusiness.Models;
//   var client = new OneDriveForBusinessClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.SharePointOnline;
//   using Azure.Connectors.Sdk.SharePointOnline.Models;
//   var client = new SharePointOnlineClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Smtp;
//   using Azure.Connectors.Sdk.Smtp.Models;
//   var client = new SmtpClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Teams;
//   using Azure.Connectors.Sdk.Teams.Models;
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
