// Connectors SDK - Generated Connectors
// Each connector client is used independently:
//
//   using Microsoft.Azure.Connectors.Sdk.Arm;
//   using Microsoft.Azure.Connectors.Sdk.Arm.Models;
//   var client = new ArmClient(connectionRuntimeUrl);
//   using Microsoft.Azure.Connectors.Sdk.AzureBlob;
//   using Microsoft.Azure.Connectors.Sdk.AzureBlob.Models;
//   var client = new AzureBlobClient(connectionRuntimeUrl);
//   using Microsoft.Azure.Connectors.Sdk.Azuremonitorlogs;
//   using Microsoft.Azure.Connectors.Sdk.Azuremonitorlogs.Models;
//   var client = new AzuremonitorlogsClient(connectionRuntimeUrl);
//   using Microsoft.Azure.Connectors.Sdk.Kusto;
//   using Microsoft.Azure.Connectors.Sdk.Kusto.Models;
//   var client = new KustoClient(connectionRuntimeUrl);
//   using Microsoft.Azure.Connectors.Sdk.Mq;
//   using Microsoft.Azure.Connectors.Sdk.Mq.Models;
//   var client = new MqClient(connectionRuntimeUrl);
//   using Microsoft.Azure.Connectors.Sdk.MsGraphGroupsAndUsers;
//   using Microsoft.Azure.Connectors.Sdk.MsGraphGroupsAndUsers.Models;
//   var client = new MsGraphGroupsAndUsersClient(connectionRuntimeUrl);
//   using Microsoft.Azure.Connectors.Sdk.Office365;
//   using Microsoft.Azure.Connectors.Sdk.Office365.Models;
//   var client = new Office365Client(connectionRuntimeUrl);
//   using Microsoft.Azure.Connectors.Sdk.Office365users;
//   using Microsoft.Azure.Connectors.Sdk.Office365users.Models;
//   var client = new Office365usersClient(connectionRuntimeUrl);
//   using Microsoft.Azure.Connectors.Sdk.OneDriveForBusiness;
//   using Microsoft.Azure.Connectors.Sdk.OneDriveForBusiness.Models;
//   var client = new OneDriveForBusinessClient(connectionRuntimeUrl);
//   using Microsoft.Azure.Connectors.Sdk.SharePointOnline;
//   using Microsoft.Azure.Connectors.Sdk.SharePointOnline.Models;
//   var client = new SharePointOnlineClient(connectionRuntimeUrl);
//   using Microsoft.Azure.Connectors.Sdk.Smtp;
//   using Microsoft.Azure.Connectors.Sdk.Smtp.Models;
//   var client = new SmtpClient(connectionRuntimeUrl);
//   using Microsoft.Azure.Connectors.Sdk.Teams;
//   using Microsoft.Azure.Connectors.Sdk.Teams.Models;
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
