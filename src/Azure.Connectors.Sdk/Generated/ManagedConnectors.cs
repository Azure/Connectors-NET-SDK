// Connectors SDK - Generated Connectors
// Each connector client is used independently:
//
//   using Azure.Connectors.Sdk.Arm;
//   using Azure.Connectors.Sdk.Arm.Models;
//   var client = new ArmClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.AzureBlob;
//   using Azure.Connectors.Sdk.AzureBlob.Models;
//   var client = new AzureBlobClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.AzureEventGrid;
//   using Azure.Connectors.Sdk.AzureEventGrid.Models;
//   var client = new AzureEventGridClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.AzureMonitorLogs;
//   using Azure.Connectors.Sdk.AzureMonitorLogs.Models;
//   var client = new AzureMonitorLogsClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.ExcelOnline;
//   using Azure.Connectors.Sdk.ExcelOnline.Models;
//   var client = new ExcelOnlineClient(connectionRuntimeUrl);
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
//   using Azure.Connectors.Sdk.Office365Users;
//   using Azure.Connectors.Sdk.Office365Users.Models;
//   var client = new Office365UsersClient(connectionRuntimeUrl);
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
//   using Azure.Connectors.Sdk.UniversalPrint;
//   using Azure.Connectors.Sdk.UniversalPrint.Models;
//   var client = new UniversalPrintClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Wdatp;
//   using Azure.Connectors.Sdk.Wdatp.Models;
//   var client = new WdatpClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Yammer;
//   using Azure.Connectors.Sdk.Yammer.Models;
//   var client = new YammerClient(connectionRuntimeUrl);

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
        "azureeventgrid",
        "azuremonitorlogs",
        "excelonline",
        "kusto",
        "mq",
        "msgraphgroupsanduser",
        "office365",
        "office365users",
        "onedriveforbusiness",
        "sharepointonline",
        "smtp",
        "teams",
        "universalprint",
        "wdatp",
        "yammer",
    ];
}
