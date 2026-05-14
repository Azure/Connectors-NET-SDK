// Connectors SDK - Generated Connectors
// Each connector client is used independently:
//
//   using Azure.Connectors.Sdk.Arm;
//   using Azure.Connectors.Sdk.Arm.Models;
//   var client = new ArmClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.AzureAD;
//   using Azure.Connectors.Sdk.AzureAD.Models;
//   var client = new AzureADClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.AzureBlob;
//   using Azure.Connectors.Sdk.AzureBlob.Models;
//   var client = new AzureBlobClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.AzureEventGrid;
//   using Azure.Connectors.Sdk.AzureEventGrid.Models;
//   var client = new AzureEventGridClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.AzureIoTCentral;
//   using Azure.Connectors.Sdk.AzureIoTCentral.Models;
//   var client = new AzureIoTCentralClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.AzureMonitorLogs;
//   using Azure.Connectors.Sdk.AzureMonitorLogs.Models;
//   var client = new AzureMonitorLogsClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Azurequeues;
//   using Azure.Connectors.Sdk.Azurequeues.Models;
//   var client = new AzureQueuesClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Azuretables;
//   using Azure.Connectors.Sdk.Azuretables.Models;
//   var client = new AzureTablesClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Campfire;
//   using Azure.Connectors.Sdk.Campfire.Models;
//   var client = new CampfireClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.ClickSendSms;
//   using Azure.Connectors.Sdk.ClickSendSms.Models;
//   var client = new ClickSendSmsClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.CloudmersiveConvert;
//   using Azure.Connectors.Sdk.CloudmersiveConvert.Models;
//   var client = new CloudmersiveConvertClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Documentdb;
//   using Azure.Connectors.Sdk.Documentdb.Models;
//   var client = new DocumentDbClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Docuware;
//   using Azure.Connectors.Sdk.Docuware.Models;
//   var client = new DocuwareClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.ElfsquadData;
//   using Azure.Connectors.Sdk.ElfsquadData.Models;
//   var client = new ElfsquadDataClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Etsy;
//   using Azure.Connectors.Sdk.Etsy.Models;
//   var client = new EtsyClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Eventhubs;
//   using Azure.Connectors.Sdk.Eventhubs.Models;
//   var client = new EventHubsClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.ExcelOnline;
//   using Azure.Connectors.Sdk.ExcelOnline.Models;
//   var client = new ExcelOnlineClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.ExcelOnlineBusiness;
//   using Azure.Connectors.Sdk.ExcelOnlineBusiness.Models;
//   var client = new ExcelOnlineBusinessClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.FormstackForms;
//   using Azure.Connectors.Sdk.FormstackForms.Models;
//   var client = new FormstackFormsClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.FreshService;
//   using Azure.Connectors.Sdk.FreshService.Models;
//   var client = new FreshServiceClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Impexium;
//   using Azure.Connectors.Sdk.Impexium.Models;
//   var client = new ImpexiumClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Infusionsoft;
//   using Azure.Connectors.Sdk.Infusionsoft.Models;
//   var client = new InfusionsoftClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Insightly;
//   using Azure.Connectors.Sdk.Insightly.Models;
//   var client = new InsightlyClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.JedoxOdataHub;
//   using Azure.Connectors.Sdk.JedoxOdataHub.Models;
//   var client = new JedoxOdataHubClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Kusto;
//   using Azure.Connectors.Sdk.Kusto.Models;
//   var client = new KustoClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.MeetingRoomMap;
//   using Azure.Connectors.Sdk.MeetingRoomMap.Models;
//   var client = new MeetingRoomMapClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.MicrosoftForms;
//   using Azure.Connectors.Sdk.MicrosoftForms.Models;
//   var client = new MicrosoftFormsClient(connectionRuntimeUrl);
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
//   using Azure.Connectors.Sdk.Orderful;
//   using Azure.Connectors.Sdk.Orderful.Models;
//   var client = new OrderfulClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Outlook;
//   using Azure.Connectors.Sdk.Outlook.Models;
//   var client = new OutlookClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.PdfCo;
//   using Azure.Connectors.Sdk.PdfCo.Models;
//   var client = new PdfCoClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Pipedrive;
//   using Azure.Connectors.Sdk.Pipedrive.Models;
//   var client = new PipedriveClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Plivo;
//   using Azure.Connectors.Sdk.Plivo.Models;
//   var client = new PlivoClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Plumsail;
//   using Azure.Connectors.Sdk.Plumsail.Models;
//   var client = new PlumsailClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Projectplace;
//   using Azure.Connectors.Sdk.Projectplace.Models;
//   var client = new ProjectplaceClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Replicon;
//   using Azure.Connectors.Sdk.Replicon.Models;
//   var client = new RepliconClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Revai;
//   using Azure.Connectors.Sdk.Revai.Models;
//   var client = new RevaiClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.SeismicPlanner;
//   using Azure.Connectors.Sdk.SeismicPlanner.Models;
//   var client = new SeismicPlannerClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Servicebus;
//   using Azure.Connectors.Sdk.Servicebus.Models;
//   var client = new ServiceBusConnectorClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.SharePointOnline;
//   using Azure.Connectors.Sdk.SharePointOnline.Models;
//   var client = new SharePointOnlineClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.SigningHub;
//   using Azure.Connectors.Sdk.SigningHub.Models;
//   var client = new SigningHubClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Smtp;
//   using Azure.Connectors.Sdk.Smtp.Models;
//   var client = new SmtpClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Starmind;
//   using Azure.Connectors.Sdk.Starmind.Models;
//   var client = new StarmindClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.StarrezRestV1;
//   using Azure.Connectors.Sdk.StarrezRestV1.Models;
//   var client = new StarrezRestV1Client(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Tallyfy;
//   using Azure.Connectors.Sdk.Tallyfy.Models;
//   var client = new TallyfyClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Teams;
//   using Azure.Connectors.Sdk.Teams.Models;
//   var client = new TeamsClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.TextRequest;
//   using Azure.Connectors.Sdk.TextRequest.Models;
//   var client = new TextRequestClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Ticketmaster;
//   using Azure.Connectors.Sdk.Ticketmaster.Models;
//   var client = new TicketmasterClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.UniversalPrint;
//   using Azure.Connectors.Sdk.UniversalPrint.Models;
//   var client = new UniversalPrintClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Waywedo;
//   using Azure.Connectors.Sdk.Waywedo.Models;
//   var client = new WaywedoClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Wdatp;
//   using Azure.Connectors.Sdk.Wdatp.Models;
//   var client = new WdatpClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.WordOnlineBusiness;
//   using Azure.Connectors.Sdk.WordOnlineBusiness.Models;
//   var client = new WordOnlineBusinessClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.Yammer;
//   using Azure.Connectors.Sdk.Yammer.Models;
//   var client = new YammerClient(connectionRuntimeUrl);
//   using Azure.Connectors.Sdk.ZohoSign;
//   using Azure.Connectors.Sdk.ZohoSign.Models;
//   var client = new ZohoSignClient(connectionRuntimeUrl);

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
        "azuread",
        "azureautomation",
        "azureblob",
        "azuredatafactory",
        "azuredigitaltwins",
        "azureeventgrid",
        "azureiotcentral",
        "azuremonitorlogs",
        "azurequeues",
        "azuretables",
        "azurevm",
        "campfire",
        "clicksendsms",
        "cloudmersiveconvert",
        "documentdb",
        "docuware",
        "elfsquaddata",
        "etsy",
        "eventhubs",
        "excelonline",
        "excelonlinebusiness",
        "formstackforms",
        "freshservice",
        "impexium",
        "infusionsoft",
        "insightly",
        "jedoxodatahub",
        "kusto",
        "keyvault",
        "meetingroommap",
        "microsoftbookings",
        "microsoftforms",
        "mq",
        "msgraphgroupsanduser",
        "office365",
        "office365groups",
        "office365groupsmail",
        "office365users",
        "onedriveforbusiness",
        "onenote",
        "orderful",
        "outlook",
        "pdfco",
        "pipedrive",
        "planner",
        "plivo",
        "plumsail",
        "powerbi",
        "projectplace",
        "replicon",
        "revai",
        "seismicplanner",
        "servicebus",
        "sharepointonline",
        "shifts",
        "signinghub",
        "smtp",
        "starmind",
        "starrezrestv1",
        "tallyfy",
        "teams",
        "todo",
        "textrequest",
        "ticketmaster",
        "universalprint",
        "waywedo",
        "wdatp",
        "wordonlinebusiness",
        "yammer",
        "zohosign",
    ];
}
