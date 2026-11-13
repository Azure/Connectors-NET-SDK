# Migrate generated .NET connector names

This preview update corrects product casing and .NET identifier casing across the
shipped generated clients and the `ConnectorNames` catalog. These public API
renames are source-breaking: update `using` directives, DI registrations, client
constructors, model factories, trigger payload types, and named arguments, then
rebuild your app.

## Renamed connector namespaces, clients, model factories, and DI helpers

| Connector (`ConnectorName`) | Namespace & Models `using` (Before → After) | Client & Model Factory (Before → After) | DI registration (Before → After) |
| --- | --- | --- | --- |
| `azurequeues` | `Azure.Connectors.Sdk.Azurequeues(.Models)` → `Azure.Connectors.Sdk.AzureQueues(.Models)` | `AzureQueuesClient` / `AzureQueuesModelFactory` *(unchanged)* | `AddAzureQueuesClient` *(unchanged)* |
| `azuretables` | `Azure.Connectors.Sdk.Azuretables(.Models)` → `Azure.Connectors.Sdk.AzureTables(.Models)` | `AzureTablesClient` / `AzureTablesModelFactory` *(unchanged)* | `AddAzureTablesClient` *(unchanged)* |
| `cloudconvert` | `Azure.Connectors.Sdk.Cloudconvert(.Models)` → `Azure.Connectors.Sdk.CloudConvert(.Models)` | `CloudconvertClient` / `CloudconvertModelFactory` → `CloudConvertClient` / `CloudConvertModelFactory` | *(none)* |
| `commondataservice` | `Azure.Connectors.Sdk.Commondataservice(.Models)` → `Azure.Connectors.Sdk.CommonDataService(.Models)` | `CommondataserviceClient` / `CommondataserviceModelFactory` → `CommonDataServiceClient` / `CommonDataServiceModelFactory` | *(none)* |
| `dataflowssms` | `Azure.Connectors.Sdk.DataflowsSms(.Models)` → `Azure.Connectors.Sdk.DataFlowsSms(.Models)` | `DataflowsSmsClient` / `DataflowsSmsModelFactory` → `DataFlowsSmsClient` / `DataFlowsSmsModelFactory` | *(none)* |
| `docuware` | `Azure.Connectors.Sdk.Docuware(.Models)` → `Azure.Connectors.Sdk.DocuWare(.Models)` | `DocuwareClient` / `DocuwareModelFactory` → `DocuWareClient` / `DocuWareModelFactory` | `AddDocuwareClient` → `AddDocuWareClient` |
| `documentdb` | `Azure.Connectors.Sdk.Documentdb(.Models)` → `Azure.Connectors.Sdk.DocumentDB(.Models)` | `DocumentDbClient` / `DocumentDbModelFactory` → `DocumentDBClient` / `DocumentDBModelFactory` | `AddDocumentDbClient` → `AddDocumentDBClient` |
| `docusign` | `Azure.Connectors.Sdk.DocuSign(.Models)` → `Azure.Connectors.Sdk.Docusign(.Models)` | `DocuSignClient` / `DocuSignModelFactory` → `DocusignClient` / `DocusignModelFactory` | *(none)* |
| `eventhubs` | `Azure.Connectors.Sdk.Eventhubs(.Models)` → `Azure.Connectors.Sdk.EventHubs(.Models)` | `EventHubsClient` / `EventHubsModelFactory` *(unchanged)* | `AddEventHubsClient` *(unchanged)* |
| `freshservice` | `Azure.Connectors.Sdk.FreshService(.Models)` → `Azure.Connectors.Sdk.Freshservice(.Models)` | `FreshServiceClient` / `FreshServiceModelFactory` → `FreshserviceClient` / `FreshserviceModelFactory` | `AddFreshServiceClient` → `AddFreshserviceClient` |
| `jedoxodatahub` | `Azure.Connectors.Sdk.JedoxOdataHub(.Models)` → `Azure.Connectors.Sdk.JedoxODataHub(.Models)` | `JedoxOdataHubClient` / `JedoxOdataHubModelFactory` → `JedoxODataHubClient` / `JedoxODataHubModelFactory` | `AddJedoxOdataHubClient` → `AddJedoxODataHubClient` |
| `mailchimp` | `Azure.Connectors.Sdk.MailChimp(.Models)` → `Azure.Connectors.Sdk.Mailchimp(.Models)` | `MailChimpClient` / `MailChimpModelFactory` → `MailchimpClient` / `MailchimpModelFactory` | *(none)* |
| `mq` | `Azure.Connectors.Sdk.Mq(.Models)` → `Azure.Connectors.Sdk.MQ(.Models)` | `MqClient` / `MqModelFactory` → `MQClient` / `MQModelFactory` | `AddMqClient` → `AddMQClient` |
| `msgraphgroupsanduser` | `Azure.Connectors.Sdk.MsGraphGroupsAndUsers(.Models)` → `Azure.Connectors.Sdk.MSGraphGroupsAndUsers(.Models)` | `MsGraphGroupsAndUsersClient` / `MsGraphGroupsAndUsersModelFactory` → `MSGraphGroupsAndUsersClient` / `MSGraphGroupsAndUsersModelFactory` | `AddMsGraphGroupsAndUsersClient` → `AddMSGraphGroupsAndUsersClient` |
| `onenote` | `Azure.Connectors.Sdk.Onenote(.Models)` → `Azure.Connectors.Sdk.OneNote(.Models)` | `OnenoteClient` / `OnenoteModelFactory` → `OneNoteClient` / `OneNoteModelFactory` | `AddOnenoteClient` → `AddOneNoteClient` |
| `revai` | `Azure.Connectors.Sdk.Revai(.Models)` → `Azure.Connectors.Sdk.RevAI(.Models)` | `RevaiClient` / `RevaiModelFactory` → `RevAIClient` / `RevAIModelFactory` | `AddRevaiClient` → `AddRevAIClient` |
| `servicebus` | `Azure.Connectors.Sdk.Servicebus(.Models)` → `Azure.Connectors.Sdk.ServiceBus(.Models)` | `ServiceBusConnectorClient` / `ServiceBusModelFactory` *(unchanged)* | `AddServiceBusConnectorClient` *(unchanged)* |
| `starrezrestv1` | `Azure.Connectors.Sdk.StarrezRestV1(.Models)` → `Azure.Connectors.Sdk.StarRezRestV1(.Models)` | `StarrezRestV1Client` / `StarrezRestV1ModelFactory` → `StarRezRestV1Client` / `StarRezRestV1ModelFactory` | `AddStarrezRestV1Client` → `AddStarRezRestV1Client` |
| `waywedo` | `Azure.Connectors.Sdk.Waywedo(.Models)` → `Azure.Connectors.Sdk.WayWeDo(.Models)` | `WaywedoClient` / `WaywedoModelFactory` → `WayWeDoClient` / `WayWeDoModelFactory` | `AddWaywedoClient` → `AddWayWeDoClient` |
| `zeptomail` | `Azure.Connectors.Sdk.Zeptomail(.Models)` → `Azure.Connectors.Sdk.ZeptoMail(.Models)` | `ZeptomailClient` / `ZeptomailModelFactory` → `ZeptoMailClient` / `ZeptoMailModelFactory` | `AddZeptomailClient` → `AddZeptoMailClient` |

For example, replace:

```csharp
using Azure.Connectors.Sdk.Documentdb;
using Azure.Connectors.Sdk.Documentdb.Models;

using var client = new DocumentDbClient(connectionRuntimeUrl);
```

with:

```csharp
using Azure.Connectors.Sdk.DocumentDB;
using Azure.Connectors.Sdk.DocumentDB.Models;

using var client = new DocumentDBClient(connectionRuntimeUrl);
```

## `ConnectorNames` constant renames and additions

`ConnectorNames` constants now apply the same .NET acronym and closed-word rules
as generated types (3+ letter acronyms use PascalCase such as `Http`, `Json`,
`Xml`, `Pdf`, `Url`, `Uri`, `Sql`, `Api`, `Rest`, `Sms`, `Smtp`, `Rss`, `Ocr`,
`Dns`, `Tls`, `Tcp`, `Csv`, `Xls`, `Xlsx`, and `Id`, while 2-letter initialisms
such as `AI`, `AD`, `DB`, `IO`, `IP`, `MQ`, and `MS` remain uppercase) and
include `ConnectorNames.Docusign` (`"docusign"`) and `ConnectorNames.Jira`
(`"jira"`). Key renames for shipped clients include:

| Before | After |
| --- | --- |
| `ConnectorNames.DataFlowsSMS` | `ConnectorNames.DataFlowsSms` |
| `ConnectorNames.MailChimp` | `ConnectorNames.Mailchimp` |
| `ConnectorNames.MsGraphGroupsAndUsers` | `ConnectorNames.MSGraphGroupsAndUsers` |
| `ConnectorNames.ClickSendSMS` | `ConnectorNames.ClickSendSms` |
| `ConnectorNames.PlivoSMS` | `ConnectorNames.PlivoSms` |
| `ConnectorNames.PDFco` | `ConnectorNames.Pdfco` |
| `ConnectorNames.RSS` | `ConnectorNames.Rss` |
| `ConnectorNames.SMTP` | `ConnectorNames.Smtp` |
| `ConnectorNames.SQLServer` | `ConnectorNames.SqlServer` |
| `ConnectorNames.StarRezRESTV1` | `ConnectorNames.StarRezRestV1` |
| `ConnectorNames.MicrosoftDefenderATP` | `ConnectorNames.MicrosoftDefenderAtp` |

The complete list of 327 renamed constants follows. It is generated by comparing the
`ConnectorNames.cs` constants on `main` with this release, keyed by connector name, so it can be
regenerated whenever the generator changes. Connector name string values are unchanged.

<details>
<summary>All 327 renamed <code>ConnectorNames</code> constants</summary>

| Before | After | Connector name |
| --- | --- | --- |
| `ConnectorNames.ABNLookupIndependentPublisher` | `ConnectorNames.AbnLookupIndependentPublisher` | `"abnlookup"` |
| `ConnectorNames.AbstractIBANValidatorIndependentPublisher` | `ConnectorNames.AbstractIbanValidatorIndependentPublisher` | `"abstractibanvalidato"` |
| `ConnectorNames.AbstractVATValidatorIndependentPublisher` | `ConnectorNames.AbstractVatValidatorIndependentPublisher` | `"abstractvatvalidator"` |
| `ConnectorNames.AdobePDFServices` | `ConnectorNames.AdobePdfServices` | `"adobepdftools"` |
| `ConnectorNames.ADOBoards` | `ConnectorNames.AdoBoards` | `"adoboards"` |
| `ConnectorNames.AfricasTalkingSMS` | `ConnectorNames.AfricasTalkingSms` | `"africastalkingsms"` |
| `ConnectorNames.AIHWMyHospitalsIndependentPublisher` | `ConnectorNames.AihwMyHospitalsIndependentPublisher` | `"myhospitalsbyaihwip"` |
| `ConnectorNames.AirtableIndependentPublisherDEPRECATED` | `ConnectorNames.AirtableIndependentPublisherDeprecated` | `"airtable"` |
| `ConnectorNames.AlembaITSM` | `ConnectorNames.AlembaItsm` | `"alembaitsm"` |
| `ConnectorNames.ALVAO` | `ConnectorNames.Alvao` | `"alvao"` |
| `ConnectorNames.AmazonSQS` | `ConnectorNames.AmazonSqs` | `"amazonsqs"` |
| `ConnectorNames.AMEEOpenBusinessIndependentPublisher` | `ConnectorNames.AmeeOpenBusinessIndependentPublisher` | `"ameeopenbusinessip"` |
| `ConnectorNames.ANYRUNThreatIntelligence` | `ConnectorNames.AnyrunThreatIntelligence` | `"anyrunthreatintellig"` |
| `ConnectorNames.APITemplateIndependentPublisher` | `ConnectorNames.ApiTemplateIndependentPublisher` | `"apitemplateip"` |
| `ConnectorNames.ApptigentPowerToolsLITE` | `ConnectorNames.ApptigentPowerToolsLite` | `"apptigentpowertoolslite"` |
| `ConnectorNames.AquaforestPDF` | `ConnectorNames.AquaforestPdf` | `"aquaforest"` |
| `ConnectorNames.ArcGIS` | `ConnectorNames.ArcGis` | `"arcgis"` |
| `ConnectorNames.ArcGISEnterprise` | `ConnectorNames.ArcGisEnterprise` | `"arcgisenterprise"` |
| `ConnectorNames.ArcGISPaaS` | `ConnectorNames.ArcGisPaaS` | `"arcgispaas"` |
| `ConnectorNames.AsiteKSA` | `ConnectorNames.AsiteKsa` | `"asiteksa"` |
| `ConnectorNames.AsiteUAE` | `ConnectorNames.AsiteUae` | `"asiteuae"` |
| `ConnectorNames.ASPSMS` | `ConnectorNames.Aspsms` | `"aspsms"` |
| `ConnectorNames.AzureCommunicationServicesSMS` | `ConnectorNames.AzureCommunicationServicesSms` | `"azurecommunicationservicessms"` |
| `ConnectorNames.AzureCommunicationServicesSMSEvents` | `ConnectorNames.AzureCommunicationServicesSmsEvents` | `"acssmsevents"` |
| `ConnectorNames.AzureDatabaseForMySQL` | `ConnectorNames.AzureDatabaseForMySql` | `"azuremysql"` |
| `ConnectorNames.AzureSQLDataWarehouse` | `ConnectorNames.AzureSqlDataWarehouse` | `"sqldw"` |
| `ConnectorNames.BBCNewsIndependentPublisher` | `ConnectorNames.BbcNewsIndependentPublisher` | `"bbcnews"` |
| `ConnectorNames.BillsPLS` | `ConnectorNames.BillsPls` | `"billspls"` |
| `ConnectorNames.BINCheckerIndependentPublisher` | `ConnectorNames.BinCheckerIndependentPublisher` | `"bincheckerip"` |
| `ConnectorNames.BKKFutarIndependentPublisher` | `ConnectorNames.BkkFutarIndependentPublisher` | `"bkkfutarip"` |
| `ConnectorNames.BlackbaudCRMConstituent` | `ConnectorNames.BlackbaudCrmConstituent` | `"blackbaudcrmconstitu"` |
| `ConnectorNames.BlackbaudCRMProspect` | `ConnectorNames.BlackbaudCrmProspect` | `"blackbaudcrmprospect"` |
| `ConnectorNames.BlackbaudFENXTQuery` | `ConnectorNames.BlackbaudFenxtQuery` | `"blackbaudfenxtquery"` |
| `ConnectorNames.BlackbaudRaisersEdgeNXT` | `ConnectorNames.BlackbaudRaisersEdgeNxt` | `"blackbaudraisersedge"` |
| `ConnectorNames.BlackbaudRaisersEdgeNXTConstituents` | `ConnectorNames.BlackbaudRaisersEdgeNxtConstituents` | `"blackbaudconstituent"` |
| `ConnectorNames.BlackbaudRaisersEdgeNXTDocuments` | `ConnectorNames.BlackbaudRaisersEdgeNxtDocuments` | `"blackbauddocuments"` |
| `ConnectorNames.BlackbaudRaisersEdgeNXTEvents` | `ConnectorNames.BlackbaudRaisersEdgeNxtEvents` | `"blackbaudevents"` |
| `ConnectorNames.BlackbaudRaisersEdgeNXTFundraising` | `ConnectorNames.BlackbaudRaisersEdgeNxtFundraising` | `"blackbaudfundraising"` |
| `ConnectorNames.BlackbaudRaisersEdgeNXTInteractions` | `ConnectorNames.BlackbaudRaisersEdgeNxtInteractions` | `"blackbaudinteraction"` |
| `ConnectorNames.BlackbaudRaisersEdgeNXTLists` | `ConnectorNames.BlackbaudRaisersEdgeNxtLists` | `"blackbaudlists"` |
| `ConnectorNames.BlackbaudRaisersEdgeNXTProspects` | `ConnectorNames.BlackbaudRaisersEdgeNxtProspects` | `"blackbaudprospects"` |
| `ConnectorNames.BlackbaudRENXTGifts` | `ConnectorNames.BlackbaudRenxtGifts` | `"blackbaudgifts"` |
| `ConnectorNames.BlackbaudRENXTQuery` | `ConnectorNames.BlackbaudRenxtQuery` | `"blackbaudrenxtquery"` |
| `ConnectorNames.BlackbaudRENXTReports` | `ConnectorNames.BlackbaudRenxtReports` | `"blackbaudrenxtreport"` |
| `ConnectorNames.BlackbaudSKYAddIns` | `ConnectorNames.BlackbaudSkyAddIns` | `"blackbaudskyaddins"` |
| `ConnectorNames.BoxMCPServer` | `ConnectorNames.BoxMcpServer` | `"boxmcpserver"` |
| `ConnectorNames.BttnONE` | `ConnectorNames.BttnOne` | `"bttnone"` |
| `ConnectorNames.BulkSMS` | `ConnectorNames.BulkSms` | `"bulksms"` |
| `ConnectorNames.CapsuleCRM` | `ConnectorNames.CapsuleCrm` | `"capsulecrm"` |
| `ConnectorNames.CDCContentServicesIndependentPublisher` | `ConnectorNames.CdcContentServicesIndependentPublisher` | `"cdccontentservicesip"` |
| `ConnectorNames.CDKDriveCustomer` | `ConnectorNames.CdkDriveCustomer` | `"cdkdrivecustomer"` |
| `ConnectorNames.CDKDriveServiceVehicles` | `ConnectorNames.CdkDriveServiceVehicles` | `"cdkdriveservicevehicles"` |
| `ConnectorNames.CelonisMCPServer` | `ConnectorNames.CelonisMcpServer` | `"celonismcpserver"` |
| `ConnectorNames.CloudBOT` | `ConnectorNames.CloudBot` | `"cloudbot"` |
| `ConnectorNames.CloudmersiveCDR` | `ConnectorNames.CloudmersiveCdr` | `"cloudmersivecdr"` |
| `ConnectorNames.CloudmersiveNLP` | `ConnectorNames.CloudmersiveNlp` | `"cloudmersivenlp"` |
| `ConnectorNames.CloudmersivePDF` | `ConnectorNames.CloudmersivePdf` | `"cloudmersivepdf"` |
| `ConnectorNames.CloudPKIManagement` | `ConnectorNames.CloudPkiManagement` | `"cloudpkimanagement"` |
| `ConnectorNames.CMI` | `ConnectorNames.Cmi` | `"cmi"` |
| `ConnectorNames.CohesityGaiaMCP` | `ConnectorNames.CohesityGaiaMcp` | `"cohesitygaiamcp"` |
| `ConnectorNames.ComputerVisionAPI` | `ConnectorNames.ComputerVisionApi` | `"cognitiveservicescomputervision"` |
| `ConnectorNames.ConnectWisePSAIndependentPublisher` | `ConnectorNames.ConnectWisePsaIndependentPublisher` | `"connectwisepsa"` |
| `ConnectorNames.CornerstoneLearningVILT` | `ConnectorNames.CornerstoneLearningVilt` | `"cornerstonelearningv"` |
| `ConnectorNames.COSMOBot` | `ConnectorNames.CosmoBot` | `"cosmobot"` |
| `ConnectorNames.COVID19JHUCSSEIndependentPublisher` | `ConnectorNames.Covid19JhucsseIndependentPublisher` | `"covid19jhucsseip"` |
| `ConnectorNames.CPQSync` | `ConnectorNames.CpqSync` | `"cpqsync"` |
| `ConnectorNames.CPSCRecallsRetrievalIndependentPublisher` | `ConnectorNames.CpscRecallsRetrievalIndependentPublisher` | `"cpscrecallsretrievalip"` |
| `ConnectorNames.CQCDataIndependentPublisher` | `ConnectorNames.CqcDataIndependentPublisher` | `"cqcdata"` |
| `ConnectorNames.CraftMyPDFIndependentPublisher` | `ConnectorNames.CraftMyPdfIndependentPublisher` | `"craftmypdfip"` |
| `ConnectorNames.CRMBot` | `ConnectorNames.CrmBot` | `"crmbot"` |
| `ConnectorNames.CronofyMCP` | `ConnectorNames.CronofyMcp` | `"cronofymcp"` |
| `ConnectorNames.CSCCorptax` | `ConnectorNames.CscCorptax` | `"corptaxsandbox"` |
| `ConnectorNames.CSVConverterByPower2Apps` | `ConnectorNames.CsvConverterByPower2Apps` | `"csvconverterbypower2"` |
| `ConnectorNames.D7SMS` | `ConnectorNames.D7Sms` | `"d7sms"` |
| `ConnectorNames.DataFlowsSMS` | `ConnectorNames.DataFlowsSms` | `"dataflowssms"` |
| `ConnectorNames.DayforceHCM` | `ConnectorNames.DayforceHcm` | `"dayforcehcm"` |
| `ConnectorNames.DBF2XML` | `ConnectorNames.Dbf2Xml` | `"dbftoxmlconverter"` |
| `ConnectorNames.DeepLIPIndependentPublisher` | `ConnectorNames.DeepLipIndependentPublisher` | `"deeplipip"` |
| `ConnectorNames.DerdackSIGNL4` | `ConnectorNames.DerdackSignl4` | `"signl4"` |
| `ConnectorNames.DigiLEANConnect` | `ConnectorNames.DigiLeanConnect` | `"digileanconnect"` |
| `ConnectorNames.DocToPDF` | `ConnectorNames.DocToPdf` | `"doctopdf"` |
| `ConnectorNames.DocusignMCPDemo` | `ConnectorNames.DocusignMcpDemo` | `"docusignmcpdemo"` |
| `ConnectorNames.DokobitUniversalAPI` | `ConnectorNames.DokobitUniversalApi` | `"dokobituniversalapi"` |
| `ConnectorNames.DPIRDRadarWestAustraliaIndependentPublisher` | `ConnectorNames.DpirdRadarWestAustraliaIndependentPublisher` | `"dpirdradarip"` |
| `ConnectorNames.DPIRDScienceWestAustraliaIndependentPublisher` | `ConnectorNames.DpirdScienceWestAustraliaIndependentPublisher` | `"dpirdscienceip"` |
| `ConnectorNames.DPIRDWeatherWestAustraliaIndependentPublisher` | `ConnectorNames.DpirdWeatherWestAustraliaIndependentPublisher` | `"dpirdweatherip"` |
| `ConnectorNames.DraupMCPServer` | `ConnectorNames.DraupMcpServer` | `"draupmcpserver"` |
| `ConnectorNames.DVLAVehicleEnquiryServiceIndependentPublisher` | `ConnectorNames.DvlaVehicleEnquiryServiceIndependentPublisher` | `"dvlavehicleenquiryse"` |
| `ConnectorNames.Dynamics365ERPAnalyticsMCP` | `ConnectorNames.Dynamics365ErpAnalyticsMcp` | `"d365erpmcpserver"` |
| `ConnectorNames.Dynamics365SalesMCPServer` | `ConnectorNames.Dynamics365SalesMcpServer` | `"d365salesmcpserver"` |
| `ConnectorNames.Dynamics365ServiceMCP` | `ConnectorNames.Dynamics365ServiceMcp` | `"d365customerservicemcpserver"` |
| `ConnectorNames.DynamicsNAV` | `ConnectorNames.DynamicsNav` | `"dynamicsnavision"` |
| `ConnectorNames.DynamicsNAV2016` | `ConnectorNames.DynamicsNaV2016` | `"dynamicsnav2016"` |
| `ConnectorNames.EBMS` | `ConnectorNames.Ebms` | `"ebms"` |
| `ConnectorNames.ECFRIndependentPublisher` | `ConnectorNames.EcfrIndependentPublisher` | `"ecfr"` |
| `ConnectorNames.EmailVeritasURLChecker` | `ConnectorNames.EmailVeritasUrlChecker` | `"emailveritasurlcheck"` |
| `ConnectorNames.EMTATLASAIMS` | `ConnectorNames.Emtatlasaims` | `"emtatlasaims"` |
| `ConnectorNames.EncodianPDF` | `ConnectorNames.EncodianPdf` | `"encodianpdf"` |
| `ConnectorNames.EnlyftMCP` | `ConnectorNames.EnlyftMcp` | `"enlyftmcp"` |
| `ConnectorNames.EONETByNASAIndependentPublisher` | `ConnectorNames.EonetByNasaIndependentPublisher` | `"eonetbynasaip"` |
| `ConnectorNames.EWayCRM` | `ConnectorNames.EWayCrm` | `"ewaycrm"` |
| `ConnectorNames.ExperlogixCPQ` | `ConnectorNames.ExperlogixCpq` | `"experlogixcpq"` |
| `ConnectorNames.EXPOCAD` | `ConnectorNames.Expocad` | `"expocad"` |
| `ConnectorNames.EzekiaMCP` | `ConnectorNames.EzekiaMcp` | `"ezekiamcp"` |
| `ConnectorNames.FaceAPI` | `ConnectorNames.FaceApi` | `"faceapi"` |
| `ConnectorNames.FarsightDNSDB` | `ConnectorNames.FarsightDnsDB` | `"farsightdnsdb"` |
| `ConnectorNames.FBIMostWantedIndependentPublisher` | `ConnectorNames.FbiMostWantedIndependentPublisher` | `"fbimostwanted"` |
| `ConnectorNames.FCAIndependentPublisher` | `ConnectorNames.FcaIndependentPublisher` | `"financialconductauth"` |
| `ConnectorNames.FDICIndependentPublisher` | `ConnectorNames.FdicIndependentPublisher` | `"fdic"` |
| `ConnectorNames.FEMAIndependentPublisher` | `ConnectorNames.FemaIndependentPublisher` | `"fema"` |
| `ConnectorNames.FHIRBase` | `ConnectorNames.FhirBase` | `"fhirbase"` |
| `ConnectorNames.FHIRClinical` | `ConnectorNames.FhirClinical` | `"fhirclinical"` |
| `ConnectorNames.FHIRlink` | `ConnectorNames.FhiRlink` | `"fhirlink"` |
| `ConnectorNames.FinnishBISIndependentPublisher` | `ConnectorNames.FinnishBisIndependentPublisher` | `"finnishbisip"` |
| `ConnectorNames.FINRAIndependentPublisher` | `ConnectorNames.FinraIndependentPublisher` | `"finra"` |
| `ConnectorNames.FlotiqHeadlessCMS` | `ConnectorNames.FlotiqHeadlessCms` | `"flotiqheadlesscms"` |
| `ConnectorNames.FORCAMFORCEBridge` | `ConnectorNames.ForcamforceBridge` | `"forcamforcebridge"` |
| `ConnectorNames.ForceManagerCRM` | `ConnectorNames.ForceManagerCrm` | `"forcemanagercrm"` |
| `ConnectorNames.FTP` | `ConnectorNames.Ftp` | `"ftp"` |
| `ConnectorNames.GieniTSServerMCP` | `ConnectorNames.GieniTSServerMcp` | `"gienitsservermcp"` |
| `ConnectorNames.GIPHYIndependentPublisher` | `ConnectorNames.GiphyIndependentPublisher` | `"giphyip"` |
| `ConnectorNames.GISCloud` | `ConnectorNames.GisCloud` | `"giscloud"` |
| `ConnectorNames.GitHubEnterpriseMSFT` | `ConnectorNames.GitHubEnterpriseMsft` | `"githubenterprise"` |
| `ConnectorNames.GMOSign` | `ConnectorNames.GmoSign` | `"gmosign"` |
| `ConnectorNames.GSAAnalyticsIndependentPublisher` | `ConnectorNames.GsaAnalyticsIndependentPublisher` | `"gsaanalytics"` |
| `ConnectorNames.GSAPerDiemIndependentPublisher` | `ConnectorNames.GsaPerDiemIndependentPublisher` | `"gsaperdiem"` |
| `ConnectorNames.GSAPublicCommentIndependentPublisher` | `ConnectorNames.GsaPublicCommentIndependentPublisher` | `"gsapubliccomment"` |
| `ConnectorNames.GSASiteScanningIndependentPublisher` | `ConnectorNames.GsaSiteScanningIndependentPublisher` | `"gsasitescanning"` |
| `ConnectorNames.HashtagAPIIndependentPublisher` | `ConnectorNames.HashtagApiIndependentPublisher` | `"hashtagapiip"` |
| `ConnectorNames.HHSMediaServicesIndependentPublisher` | `ConnectorNames.HhsMediaServicesIndependentPublisher` | `"hhsmediaservices"` |
| `ConnectorNames.HighspotMCP` | `ConnectorNames.HighspotMcp` | `"highspotmcptestjan20"` |
| `ConnectorNames.HiveCPQProductConfigurator` | `ConnectorNames.HiveCpqProductConfigurator` | `"hivecpqproductconfig"` |
| `ConnectorNames.HTMLToPDFByPascalcase` | `ConnectorNames.HtmlToPdfByPascalcase` | `"htmltopdfconverter"` |
| `ConnectorNames.HTTPWithMicrosoftEntraId` | `ConnectorNames.HttpWithMicrosoftEntraId` | `"webcontentsv2"` |
| `ConnectorNames.HTTPWithMicrosoftEntraIdPreauthorized` | `ConnectorNames.HttpWithMicrosoftEntraIdPreauthorized` | `"webcontents"` |
| `ConnectorNames.HubSpotCMSIndependentPublisher` | `ConnectorNames.HubSpotCmsIndependentPublisher` | `"hubspotcms"` |
| `ConnectorNames.HubSpotCMSV2IndependentPublisher` | `ConnectorNames.HubSpotCmsV2IndependentPublisher` | `"hubspotcmsv2"` |
| `ConnectorNames.HubSpotCRMIndependentPublisher` | `ConnectorNames.HubSpotCrmIndependentPublisher` | `"hubspotcrm"` |
| `ConnectorNames.HubSpotCRMV2IndependentPublisher` | `ConnectorNames.HubSpotCrmV2IndependentPublisher` | `"hubspotcrmv2"` |
| `ConnectorNames.HUEDatagate` | `ConnectorNames.HueDatagate` | `"huedatagate"` |
| `ConnectorNames.HVIVehicleInspectionV12` | `ConnectorNames.HviVehicleInspectionV12` | `"hvivehicleinspection"` |
| `ConnectorNames.HYASInsight` | `ConnectorNames.HyasInsight` | `"hyasinsight"` |
| `ConnectorNames.IAConnectJML` | `ConnectorNames.IAConnectJml` | `"iaconnectjml"` |
| `ConnectorNames.IAConnectSAPGUI` | `ConnectorNames.IAConnectSapgui` | `"iaconnectsapgui"` |
| `ConnectorNames.IBM3270` | `ConnectorNames.Ibm3270` | `"si3270"` |
| `ConnectorNames.IBMWatsonAssistantIndependentPublisher` | `ConnectorNames.IbmWatsonAssistantIndependentPublisher` | `"ibmwatsonassistantip"` |
| `ConnectorNames.IBMWatsonTextToSpeechIndependentPublisher` | `ConnectorNames.IbmWatsonTextToSpeechIndependentPublisher` | `"ibmwatsontexttospeip"` |
| `ConnectorNames.ILovePDF` | `ConnectorNames.ILovePdf` | `"ilovepdf"` |
| `ConnectorNames.IMIS` | `ConnectorNames.Imis` | `"imis"` |
| `ConnectorNames.ImpowerERP` | `ConnectorNames.ImpowerErp` | `"impower"` |
| `ConnectorNames.Imprezian360CRM` | `ConnectorNames.Imprezian360Crm` | `"imprezian"` |
| `ConnectorNames.INDAadhaarNumberMasking` | `ConnectorNames.IndAadhaarNumberMasking` | `"indaadhaarnm"` |
| `ConnectorNames.INDFaceMatch` | `ConnectorNames.IndFaceMatch` | `"indfacematch"` |
| `ConnectorNames.INDInsuranceICD10CPT` | `ConnectorNames.IndInsuranceIcd10Cpt` | `"indinsurance"` |
| `ConnectorNames.INDInvoiceDataCapture` | `ConnectorNames.IndInvoiceDataCapture` | `"indinvoicedatacapture"` |
| `ConnectorNames.INDKYCIndia` | `ConnectorNames.IndkycIndia` | `"indkycindia"` |
| `ConnectorNames.INDPayables` | `ConnectorNames.IndPayables` | `"indpayables"` |
| `ConnectorNames.IntegrablePDF` | `ConnectorNames.IntegrablePdf` | `"integrablepdf"` |
| `ConnectorNames.IntelixIOCAnalysisMCP` | `ConnectorNames.IntelixIocAnalysisMcp` | `"intelixiocanalysismc"` |
| `ConnectorNames.IP2LOCATIONIndependentPublisher` | `ConnectorNames.IP2LocationIndependentPublisher` | `"ip2locationip"` |
| `ConnectorNames.IP2WHOISIndependentPublisher` | `ConnectorNames.IP2WhoisIndependentPublisher` | `"ip2whoisip"` |
| `ConnectorNames.IPQSFraudAndRiskScoring` | `ConnectorNames.IpqsFraudAndRiskScoring` | `"ipqsfraudandriskscor"` |
| `ConnectorNames.ISOPlanner` | `ConnectorNames.IsoPlanner` | `"isoplanner"` |
| `ConnectorNames.JIRASearchIndependentPublisher` | `ConnectorNames.JiraSearchIndependentPublisher` | `"jirasearch"` |
| `ConnectorNames.KhalibreLMS` | `ConnectorNames.KhalibreLms` | `"khalibrelms"` |
| `ConnectorNames.KORTOV2` | `ConnectorNames.KortoV2` | `"korto"` |
| `ConnectorNames.LatinShareSHPManagement` | `ConnectorNames.LatinShareShpManagement` | `"latinshareshpmanagement"` |
| `ConnectorNames.LatinShareSHPPermissions` | `ConnectorNames.LatinShareShpPermissions` | `"latinshareshppermissions"` |
| `ConnectorNames.LCPICordis` | `ConnectorNames.LcpiCordis` | `"lcpicordis"` |
| `ConnectorNames.LettriaGDPRCompliance` | `ConnectorNames.LettriaGdprCompliance` | `"lettriagdprcompliance"` |
| `ConnectorNames.LIFX` | `ConnectorNames.Lifx` | `"lifx"` |
| `ConnectorNames.LINKMobility` | `ConnectorNames.LinkMobility` | `"linkmobility"` |
| `ConnectorNames.LMS365` | `ConnectorNames.Lms365` | `"lms365"` |
| `ConnectorNames.LSEG` | `ConnectorNames.Lseg` | `"lseg"` |
| `ConnectorNames.LSEGFinancialAnalytics` | `ConnectorNames.LsegFinancialAnalytics` | `"lsegfinancialanalyti"` |
| `ConnectorNames.LucidMCPServer` | `ConnectorNames.LucidMcpServer` | `"lucidmcpserver"` |
| `ConnectorNames.LUIS` | `ConnectorNames.Luis` | `"luis"` |
| `ConnectorNames.MailChimp` | `ConnectorNames.Mailchimp` | `"mailchimp"` |
| `ConnectorNames.MaximizerCRM` | `ConnectorNames.MaximizerCrm` | `"maximizercrm"` |
| `ConnectorNames.MCPHiveTIntegration` | `ConnectorNames.McpHiveTIntegration` | `"mcphivetintegration"` |
| `ConnectorNames.MessageBirdSMSIndependentPublisher` | `ConnectorNames.MessageBirdSmsIndependentPublisher` | `"messagebirdsmsip"` |
| `ConnectorNames.Microsoft365AdminCenterMCP` | `ConnectorNames.Microsoft365AdminCenterMcp` | `"a365adminmcp"` |
| `ConnectorNames.MicrosoftD365CEV9OnPremIndependentPublisher` | `ConnectorNames.MicrosoftD365CeV9OnPremIndependentPublisher` | `"microsoftd365cev9ip"` |
| `ConnectorNames.MicrosoftDefenderATP` | `ConnectorNames.MicrosoftDefenderAtp` | `"wdatp"` |
| `ConnectorNames.MicrosoftLearnDocsMCP` | `ConnectorNames.MicrosoftLearnDocsMcp` | `"microsoftlearndocsmcpserver"` |
| `ConnectorNames.MicrosoftMCPServers` | `ConnectorNames.MicrosoftMcpServers` | `"a365mcpservers"` |
| `ConnectorNames.MicrosoftSentinelMCP` | `ConnectorNames.MicrosoftSentinelMcp` | `"sentinelmcp"` |
| `ConnectorNames.MiniSoupHTMLParserIndependentPublisher` | `ConnectorNames.MiniSoupHtmlParserIndependentPublisher` | `"minisouphtmlparser"` |
| `ConnectorNames.MintNFTIndependentPublisher` | `ConnectorNames.MintNftIndependentPublisher` | `"nftmaniaip"` |
| `ConnectorNames.MobileTextAlertsMCPServer` | `ConnectorNames.MobileTextAlertsMcpServer` | `"mobiletextalertsmcps"` |
| `ConnectorNames.MOBSIMSendSMS` | `ConnectorNames.MobsimSendSms` | `"mobsimsendsms"` |
| `ConnectorNames.MonsterAPIIndependentPublisher` | `ConnectorNames.MonsterApiIndependentPublisher` | `"monsterapiip"` |
| `ConnectorNames.MSNWeather` | `ConnectorNames.MsnWeather` | `"msnweather"` |
| `ConnectorNames.MtargetSMS` | `ConnectorNames.MtargetSms` | `"mtarget"` |
| `ConnectorNames.MuhimbiPDF` | `ConnectorNames.MuhimbiPdf` | `"muhimbipdf"` |
| `ConnectorNames.MURAL` | `ConnectorNames.Mural` | `"mural"` |
| `ConnectorNames.MySQL` | `ConnectorNames.MySql` | `"mysql"` |
| `ConnectorNames.NameAPIIndependentPublisher` | `ConnectorNames.NameApiIndependentPublisher` | `"nameapi"` |
| `ConnectorNames.NASAFIRMSIndependentPublisher` | `ConnectorNames.NasafirmsIndependentPublisher` | `"nasafirms"` |
| `ConnectorNames.NASAImageAndVideoLibraryIndependentPublisher` | `ConnectorNames.NasaImageAndVideoLibraryIndependentPublisher` | `"nasaivlibraryip"` |
| `ConnectorNames.NCEIClimateDataIndependentPublisher` | `ConnectorNames.NceiClimateDataIndependentPublisher` | `"nceiclimatedata"` |
| `ConnectorNames.NEOWsIndependentPublisher` | `ConnectorNames.NeoWsIndependentPublisher` | `"nearearthobjectwebip"` |
| `ConnectorNames.NHTSAVPICIndependentPublisher` | `ConnectorNames.NhtsavpicIndependentPublisher` | `"nhtsavpicip"` |
| `ConnectorNames.NISTNVDIndependentPublisher` | `ConnectorNames.NistnvdIndependentPublisher` | `"nistnationalvulnerip"` |
| `ConnectorNames.NitroPDFServices` | `ConnectorNames.NitroPdfServices` | `"nitropdfservices"` |
| `ConnectorNames.NRELIndependentPublisher` | `ConnectorNames.NrelIndependentPublisher` | `"nrelip"` |
| `ConnectorNames.NumlookupAPIIndependentPublisher` | `ConnectorNames.NumlookupApiIndependentPublisher` | `"numlookupapiip"` |
| `ConnectorNames.NutrientConvertToPDF` | `ConnectorNames.NutrientConvertToPdf` | `"nutrientconverttopdf"` |
| `ConnectorNames.NutrientExtractFromPDF` | `ConnectorNames.NutrientExtractFromPdf` | `"nutrientextractfromp"` |
| `ConnectorNames.NutrientPDFOCR` | `ConnectorNames.NutrientPdfOcr` | `"nutrientpdfocr"` |
| `ConnectorNames.NutrientWatermarkToPDF` | `ConnectorNames.NutrientWatermarkToPdf` | `"nutrientwatermarktop"` |
| `ConnectorNames.ODBCDataSource` | `ConnectorNames.OdbcDataSource` | `"odbc"` |
| `ConnectorNames.OpenAIGPTIndependentPublisher` | `ConnectorNames.OpenAigptIndependentPublisher` | `"openaigpt4ip"` |
| `ConnectorNames.OpenLegacyIBMIAS400` | `ConnectorNames.OpenLegacyIbmias400` | `"openlegacyibmias400"` |
| `ConnectorNames.OpenLegacyIBMMainframe` | `ConnectorNames.OpenLegacyIbmMainframe` | `"openlegacyibmmainframe"` |
| `ConnectorNames.OpenNEMIndependentPublisher` | `ConnectorNames.OpenNemIndependentPublisher` | `"opennemip"` |
| `ConnectorNames.OpenPLZIndependentPublisher` | `ConnectorNames.OpenPlzIndependentPublisher` | `"openplz"` |
| `ConnectorNames.OpenTextEDOCS` | `ConnectorNames.OpenTextEdocs` | `"opentextedocsbyonefox"` |
| `ConnectorNames.OpenTextExtendedECM` | `ConnectorNames.OpenTextExtendedEcm` | `"opentextcsbyonefox"` |
| `ConnectorNames.OptiAPI` | `ConnectorNames.OptiApi` | `"optiapi"` |
| `ConnectorNames.OQSHA` | `ConnectorNames.Oqsha` | `"oqsha"` |
| `ConnectorNames.ORBIntelligenceIndependentPublisher` | `ConnectorNames.OrbIntelligenceIndependentPublisher` | `"orbintelligenceip"` |
| `ConnectorNames.PDF4me` | `ConnectorNames.Pdf4me` | `"pdf4me"` |
| `ConnectorNames.PDF4meAI` | `ConnectorNames.Pdf4meAI` | `"pdf4meai"` |
| `ConnectorNames.PDF4meBarcode` | `ConnectorNames.Pdf4meBarcode` | `"pdf4mebarcode"` |
| `ConnectorNames.PDF4meConnect` | `ConnectorNames.Pdf4meConnect` | `"pdf4meconnect"` |
| `ConnectorNames.PDF4meConvert` | `ConnectorNames.Pdf4meConvert` | `"pdf4meconvert"` |
| `ConnectorNames.PDF4meExcel` | `ConnectorNames.Pdf4meExcel` | `"pdf4meexcel"` |
| `ConnectorNames.PDF4meImage` | `ConnectorNames.Pdf4meImage` | `"pdf4meimage"` |
| `ConnectorNames.PDF4mePDF` | `ConnectorNames.Pdf4mePdf` | `"pdf4mepdf"` |
| `ConnectorNames.PDF4meSwissQR` | `ConnectorNames.Pdf4meSwissQR` | `"pdf4meswissqr"` |
| `ConnectorNames.PDF4meWord` | `ConnectorNames.Pdf4meWord` | `"pdf4meword"` |
| `ConnectorNames.PDFBlocks` | `ConnectorNames.PdfBlocks` | `"pdfblocks"` |
| `ConnectorNames.PDFTools` | `ConnectorNames.PdfTools` | `"pdftools"` |
| `ConnectorNames.PDFToolsByTachytelicIndependentPublisher` | `ConnectorNames.PdfToolsByTachytelicIndependentPublisher` | `"pdftoolsbytachytelic"` |
| `ConnectorNames.PhilipsHUEIndependentPublisher` | `ConnectorNames.PhilipsHueIndependentPublisher` | `"philipshueip"` |
| `ConnectorNames.PipelinerCRM` | `ConnectorNames.PipelinerCrm` | `"pipelinercrm"` |
| `ConnectorNames.PIPwareKPIs` | `ConnectorNames.PIPwareKpis` | `"pipwarekpis"` |
| `ConnectorNames.PokeAPICoreIndependentPublisher` | `ConnectorNames.PokeApiCoreIndependentPublisher` | `"pokeapicore"` |
| `ConnectorNames.PokeAPIWorldIndependentPublisher` | `ConnectorNames.PokeApiWorldIndependentPublisher` | `"pokeapiworld"` |
| `ConnectorNames.PolarisPSA` | `ConnectorNames.PolarisPsa` | `"polarispsa"` |
| `ConnectorNames.PostgreSQL` | `ConnectorNames.PostgreSql` | `"postgresql"` |
| `ConnectorNames.PPMExpress` | `ConnectorNames.PpmExpress` | `"ppmexpress"` |
| `ConnectorNames.PriorityMatrixHIPAA` | `ConnectorNames.PriorityMatrixHipaa` | `"prioritymatrixhipaa"` |
| `ConnectorNames.ProcessStreetMCPServer` | `ConnectorNames.ProcessStreetMcpServer` | `"processstreetmcpserv"` |
| `ConnectorNames.PROSAI` | `ConnectorNames.Prosai` | `"prosaiforsalescopilo"` |
| `ConnectorNames.PUGGamifiedEngagement` | `ConnectorNames.PugGamifiedEngagement` | `"puggamifiedengagement"` |
| `ConnectorNames.QPPNextGen` | `ConnectorNames.QppNextGen` | `"qppngintegrationhub"` |
| `ConnectorNames.RAPIdPlatform` | `ConnectorNames.RapIdPlatform` | `"rapidplatform"` |
| `ConnectorNames.RegoLinkForClarityPPM` | `ConnectorNames.RegoLinkForClarityPpm` | `"regolink"` |
| `ConnectorNames.RESTCountriesIndependentPublisher` | `ConnectorNames.RestCountriesIndependentPublisher` | `"restcountriesip"` |
| `ConnectorNames.RetarusSMS` | `ConnectorNames.RetarusSms` | `"smsforapplications"` |
| `ConnectorNames.RSS` | `ConnectorNames.Rss` | `"rss"` |
| `ConnectorNames.SAP` | `ConnectorNames.Sap` | `"sap"` |
| `ConnectorNames.SAPOData` | `ConnectorNames.SapoData` | `"sapodata"` |
| `ConnectorNames.SASDecisioning` | `ConnectorNames.SasDecisioning` | `"sasdecisioning"` |
| `ConnectorNames.SearchAPIGoogleSearchIndependentPublisher` | `ConnectorNames.SearchApiGoogleSearchIndependentPublisher` | `"searchapigooglesearch"` |
| `ConnectorNames.SECIB` | `ConnectorNames.Secib` | `"secib"` |
| `ConnectorNames.Serverless360BAMTracking` | `ConnectorNames.Serverless360BamTracking` | `"serverless360"` |
| `ConnectorNames.SerwerSMS` | `ConnectorNames.SerwerSms` | `"serwersms"` |
| `ConnectorNames.SFTPSSH` | `ConnectorNames.Sftpssh` | `"sftpwithssh"` |
| `ConnectorNames.ShortURL` | `ConnectorNames.ShortUrl` | `"shorturl"` |
| `ConnectorNames.ShortySMSIndependentPublisher` | `ConnectorNames.ShortySmsIndependentPublisher` | `"shortysmsip"` |
| `ConnectorNames.SHRTCODEIndependentPublisher` | `ConnectorNames.ShrtcodeIndependentPublisher` | `"shrtcodeip"` |
| `ConnectorNames.SignatureAPI` | `ConnectorNames.SignatureApi` | `"signatureapi"` |
| `ConnectorNames.SimpleEDI` | `ConnectorNames.SimpleEdi` | `"simpleedi"` |
| `ConnectorNames.SmartCOMMDocGen` | `ConnectorNames.SmartCommDocGen` | `"smartcommondemanddoc"` |
| `ConnectorNames.SMSAPI` | `ConnectorNames.SmsApi` | `"smsapi"` |
| `ConnectorNames.SMSLink` | `ConnectorNames.SmsLink` | `"smslink"` |
| `ConnectorNames.SMSWirelessServicesIndependentPublisher` | `ConnectorNames.SmsWirelessServicesIndependentPublisher` | `"smswirelessserviceip"` |
| `ConnectorNames.SMTP` | `ConnectorNames.Smtp` | `"smtp"` |
| `ConnectorNames.SoftoneWebCRM` | `ConnectorNames.SoftoneWebCrm` | `"softonewebcrm"` |
| `ConnectorNames.SoloSignHMACHashCreator` | `ConnectorNames.SoloSignHmacHashCreator` | `"solosign"` |
| `ConnectorNames.SOSInventoryIndependentPublisher` | `ConnectorNames.SosInventoryIndependentPublisher` | `"sosinventoryip"` |
| `ConnectorNames.SQLServer` | `ConnectorNames.SqlServer` | `"sql"` |
| `ConnectorNames.StarRezRESTV1` | `ConnectorNames.StarRezRestV1` | `"starrezrestv1"` |
| `ConnectorNames.TabscannerReceiptOCRIndependentPublisher` | `ConnectorNames.TabscannerReceiptOcrIndependentPublisher` | `"tabscannerreceiptocr"` |
| `ConnectorNames.TAGGUNReceiptOCRScanningIndependentPublisher` | `ConnectorNames.TaggunReceiptOcrScanningIndependentPublisher` | `"taggunreceiptocrscip"` |
| `ConnectorNames.TALXISDataFeed` | `ConnectorNames.TalxisDataFeed` | `"talxisdatafeed"` |
| `ConnectorNames.TavilyMCP` | `ConnectorNames.TavilyMcp` | `"tavilymcp"` |
| `ConnectorNames.TegolySIGN` | `ConnectorNames.TegolySign` | `"tegolysign"` |
| `ConnectorNames.TeleSignSMS` | `ConnectorNames.TeleSignSms` | `"telesignsms"` |
| `ConnectorNames.TheGoodAPIIndependentPublisher` | `ConnectorNames.TheGoodApiIndependentPublisher` | `"thegoodapiip"` |
| `ConnectorNames.TheSMSWorksIndependentPublisher` | `ConnectorNames.TheSmsWorksIndependentPublisher` | `"thesmsworksip"` |
| `ConnectorNames.TimeAPIIndependentPublisher` | `ConnectorNames.TimeApiIndependentPublisher` | `"timeapi"` |
| `ConnectorNames.TLDR` | `ConnectorNames.Tldr` | `"tldrip"` |
| `ConnectorNames.TLYIndependentPublisher` | `ConnectorNames.TlyIndependentPublisher` | `"tly"` |
| `ConnectorNames.TouchSMS` | `ConnectorNames.TouchSms` | `"touchsmsv2documentat"` |
| `ConnectorNames.TPCPortal` | `ConnectorNames.TpcPortal` | `"tpcportal"` |
| `ConnectorNames.TribalSITS` | `ConnectorNames.TribalSits` | `"tribalsits"` |
| `ConnectorNames.TRIGGERcmd` | `ConnectorNames.TriggeRcmd` | `"triggercmd"` |
| `ConnectorNames.TrueDialogSMS` | `ConnectorNames.TrueDialogSms` | `"truedialogsms"` |
| `ConnectorNames.TyntecSMSBusiness` | `ConnectorNames.TyntecSmsBusiness` | `"tyntecsms"` |
| `ConnectorNames.UKCheckVATIndependentPublisher` | `ConnectorNames.UKCheckVatIndependentPublisher` | `"ukgovtcheckvatip"` |
| `ConnectorNames.UKGProHCM` | `ConnectorNames.UkgProHcm` | `"ukgprohcm"` |
| `ConnectorNames.UKGPROWFMAuthentication` | `ConnectorNames.UkgprowfmAuthentication` | `"prowfmauthentication"` |
| `ConnectorNames.UKGProWFMEmployee` | `ConnectorNames.UkgProWfmEmployee` | `"ukgprowfmemployee"` |
| `ConnectorNames.UKGProWFMPeople` | `ConnectorNames.UkgProWfmPeople` | `"ukgprowfmpeople"` |
| `ConnectorNames.UKGProWFMTimekeeping` | `ConnectorNames.UkgProWfmTimekeeping` | `"ukgprowfmtimekeeping"` |
| `ConnectorNames.UPS` | `ConnectorNames.Ups` | `"ups"` |
| `ConnectorNames.URLDevIndependentPublisher` | `ConnectorNames.UrlDevIndependentPublisher` | `"urldevip"` |
| `ConnectorNames.USAJOBSIndependentPublisher` | `ConnectorNames.UsajobsIndependentPublisher` | `"usajobs"` |
| `ConnectorNames.USB4SAP` | `ConnectorNames.Usb4Sap` | `"usb4sap"` |
| `ConnectorNames.USCongressCRSIndependentPublisher` | `ConnectorNames.USCongressCrsIndependentPublisher` | `"uscongresscrs"` |
| `ConnectorNames.VATCheckApi` | `ConnectorNames.VatCheckApi` | `"vatcheckapiip"` |
| `ConnectorNames.VIESIndependentPublisher` | `ConnectorNames.ViesIndependentPublisher` | `"viesip"` |
| `ConnectorNames.VoiceRSSIndependentPubisher` | `ConnectorNames.VoiceRssIndependentPubisher` | `"voicerss"` |
| `ConnectorNames.WebhoodURLScanner` | `ConnectorNames.WebhoodUrlScanner` | `"webhoodurlscanner"` |
| `ConnectorNames.WMATAIndependentPublisher` | `ConnectorNames.WmataIndependentPublisher` | `"wmata"` |
| `ConnectorNames.WorkdayHCM` | `ConnectorNames.WorkdayHcm` | `"workdayhcm"` |
| `ConnectorNames.WorkIQCalendarMCP` | `ConnectorNames.WorkIQCalendarMcp` | `"a365outlookcalendarmcp"` |
| `ConnectorNames.WorkIQCopilotMCP` | `ConnectorNames.WorkIQCopilotMcp` | `"a365copilotchatmcp"` |
| `ConnectorNames.WorkIQMailMCP` | `ConnectorNames.WorkIQMailMcp` | `"a365outlookmailmcp"` |
| `ConnectorNames.WorkIQOneDriveMCP` | `ConnectorNames.WorkIQOneDriveMcp` | `"workiqonedrive"` |
| `ConnectorNames.WorkIQSharePointMCP` | `ConnectorNames.WorkIQSharePointMcp` | `"workiqsharepoint"` |
| `ConnectorNames.WorkIQTeamsMCP` | `ConnectorNames.WorkIQTeamsMcp` | `"a365teamsmcp"` |
| `ConnectorNames.WorkIQUserMCP` | `ConnectorNames.WorkIQUserMcp` | `"a365memcp"` |
| `ConnectorNames.WorkIQWordMCP` | `ConnectorNames.WorkIQWordMcp` | `"a365wordmcp"` |
| `ConnectorNames.WPFormsByReenhancedLLC` | `ConnectorNames.WPFormsByReenhancedLlc` | `"wpformsbyreenhancedl"` |
| `ConnectorNames.WQRMRiskForecastServices` | `ConnectorNames.WqrmRiskForecastServices` | `"wqrmriskforecastserv"` |
| `ConnectorNames.XSOARIndependentPublisher` | `ConnectorNames.XsoarIndependentPublisher` | `"xsoarip"` |
| `ConnectorNames.XSSPDFSolutionsIntegrations` | `ConnectorNames.XsspdfSolutionsIntegrations` | `"xsspdfsolutionsinteg"` |
| `ConnectorNames.XSSQRCodeSolutions` | `ConnectorNames.XssqrCodeSolutions` | `"xssqrcodesolutions"` |
| `ConnectorNames.ZapierMCP` | `ConnectorNames.ZapierMcp` | `"zapiermcp"` |
| `ConnectorNames.ZapierNLAIndependentPublisher` | `ConnectorNames.ZapierNlaIndependentPublisher` | `"zapiernlaip"` |
| `ConnectorNames.ZIPPYDOC` | `ConnectorNames.Zippydoc` | `"zippydoc"` |
| `ConnectorNames.ZoomInfoGTMMCP` | `ConnectorNames.ZoomInfoGtmmcp` | `"zoominfogtm"` |

</details>

## Trigger callback payload and trigger metadata renames

Trigger operation and parameter container classes follow the updated connector
prefixes (`CommonDataServiceTriggerOperations`, `DataFlowsSmsTriggerOperations`,
`DocusignTriggerOperations`, `FreshserviceTriggerOperations`, `OneNoteTriggerOperations`,
`WayWeDoTriggerOperations`, `ServiceBusTriggerOperations`, and `ServiceBusTriggerParameters`), and trigger
callback payload types strip a redundant trailing `Trigger` token before
`TriggerPayload`:

| Before | After |
| --- | --- |
| `AzureIoTCentralOnWorkflowCreateTriggerTriggerPayload` | `AzureIoTCentralOnWorkflowCreateTriggerPayload` |
| `CommondataserviceOnDeletedItemsTriggerPayload` / `OnNewItems` / `OnUpdatedItems` | `CommonDataServiceOnDeletedItemsTriggerPayload` / `OnNewItems` / `OnUpdatedItems` |
| `DataflowsSmsOnRegisterWebhookTriggerPayload` | `DataFlowsSmsOnRegisterWebhookTriggerPayload` |
| `DocuSignOnCreateHookEnvelopeTriggerPayload` / `OnCreateOrgHookEnvelope` | `DocusignOnCreateHookEnvelopeTriggerPayload` / `OnCreateOrgHookEnvelope` |
| `ElfsquadDataOnCreateTriggerTriggerPayload` | `ElfsquadDataOnCreateTriggerPayload` |
| `FreshServiceOnTicketCreatedTriggerPayload` | `FreshserviceOnTicketCreatedTriggerPayload` |
| `GitHubOnWebhookPullRequestTriggerTriggerPayload` | `GitHubOnWebhookPullRequestTriggerPayload` |
| `JiraOnNewIssueJQLTriggerPayload` / `OnNewIssueJQLDatacenterTriggerPayload` | `JiraOnNewIssueJqlTriggerPayload` / `OnNewIssueJqlDatacenterTriggerPayload` |
| `PlumsailOnFlowV1ProcessesFlowTriggersPostTriggerPayload` | `PlumsailOnFlowV1ProcessesFlowTriggersV2PostTriggerPayload` *(refreshed to non-deprecated V2 trigger)* |
| `RepliconOnWebhookSubscriptionsRestAPITriggerPayload` | `RepliconOnWebhookSubscriptionsRestApiTriggerPayload` |
| `ServicebusOnGetMessagesFromQueueTriggerPayload` / `FromTopic` / `WithPeekLock` | `ServiceBusOnGetMessagesFromQueueTriggerPayload` / `FromTopic` / `WithPeekLock` |
| `TeamsOnRecordingTriggerTriggerPayload` / `OnTranscriptTriggerTriggerPayload` | `TeamsOnRecordingTriggerPayload` / `OnTranscriptTriggerPayload` |
| `TeamsOnWebhookAtMentionTriggerTriggerPayload` / `ChatMessage` / `Keyword` / `MessageReaction` / `NewMessage` | `TeamsOnWebhookAtMentionTriggerPayload` / `ChatMessage` / `Keyword` / `MessageReaction` / `NewMessage` |
| `OnenoteOnNewPageInSectionTriggerPayload` / `OnNewSectionGroupInNotebook` / `OnNewSectionInNotebook` | `OneNoteOnNewPageInSectionTriggerPayload` / `OnNewSectionGroupInNotebook` / `OnNewSectionInNotebook` |
| `WaywedoOnChecklistCreateWebHookTriggerPayload` / `OnChecklistStepCompletedWebHook` / `OnFinishChecklistWebHook` / `OnInviteSupervisorWebHook` / `OnNewCommentWebHook` | `WayWeDoOnChecklistCreateWebHookTriggerPayload` / `OnChecklistStepCompletedWebHook` / `OnFinishChecklistWebHook` / `OnInviteSupervisorWebHook` / `OnNewCommentWebHook` |
| `WaywedoOnGenerateAcceptancePDFWebHookTriggerPayload` | `WayWeDoOnGenerateAcceptancePdfWebHookTriggerPayload` |

## Model property, `Id` → `Guid`, and parameter renames

Model properties and factory parameter names also change when the old spelling
was inconsistent or when a wire `guid` field was previously rewritten to `Id`:

- **`Id` / `Id2` → `Guid`**: Wire fields named `guid` now preserve `Guid` on the
  generated model instead of collapsing to `Id`:
  - `DocuWare.Models.PlaceAStampResponse.Id` → `PlaceAStampResponse.Guid` (wire key `"Guid"`)
  - `WordPress.Models.PostResponse.Id2` → `PostResponse.Guid` (wire key `"guid"`, alongside `PostResponse.Id`)
  - `WordPress.Models.PostModel.Id2` → `PostModel.Guid` (wire key `"guid"`, alongside `PostModel.Id`)
- **Acronym & plural-acronym casing**: Teams `RecordingContentURL` becomes
  `RecordingContentUrl`, Docusign `GetAllWorkflowIDsAsync` / `WorkflowIDs`
  becomes `GetAllWorkflowIdsAsync` / `WorkflowIds`, and Jira
  `ListIssuesAsync(jQLQuery: ...)` becomes `ListIssuesAsync(jqlQuery: ...)`.
- **Possessive `'s` no longer creates an `S` word**: names derived from summaries such as
  "Sender's name" or "User's ID" drop the possessive. Docusign
  `SenderSName` / `SenderSEmailAddress` / `SenderSUserId` / `SenderSAccountId` become
  `SenderName` / `SenderEmailAddress` / `SenderUserId` / `SenderAccountId`; Etsy
  `BuyerSEmailAddress` / `SellerSEmailAddress` become `BuyerEmailAddress` /
  `SellerEmailAddress`; Mailchimp `SenderSName` / `SenderSEmailAddress` become
  `SenderName` / `SenderEmailAddress`; Teams `UserSId` and `TeamSWebUrl` become
  `UserId` and `TeamWebUrl`; TextRequest parameters
  `contactSPhoneNumberToSearchBy` / `whetherContactSConversationIsResolved` become
  `contactPhoneNumberToSearchBy` / `whetherContactConversationIsResolved`; and the
  MeetingRoomMap search parameter drops `UserS` for `User`. The wire name decides
  ambiguous cases: Teams `ShowInTeamSSearchAndSuggestions` becomes
  `ShowInTeamsSearchAndSuggestions`, because its wire name
  `showInTeamsSearchAndSuggestions` names the Teams product even though the summary
  reads "team's".
- **Separator word boundaries**: uppercase runs separated in the source keep their
  boundary. GitHub parameter `ldapdn` becomes `ldapDN`, Plumsail `EnUSPOSIX`
  becomes `EnUSPosix`, and Infusionsoft `DueDateFormatYYYYMMDdThhMmSsFffZ`
  becomes `DueDateFormatYyyyMMDdThhMmSsFffZ`.
- **Page-envelope collection property**: DocumentDB
  `QueryDocumentsResponse.Documents` becomes `Value` (wire key `"value"`).

Raw connector API IDs (for example `azureiotcentral` and `dataflowssms`),
operation IDs, HTTP routes, JSON/XML field names, and enum wire values are
independent of public identifier spelling. Do not change connection names or
serialized payload keys to match the new C# names. `ServiceBusConnectorClient`
retains its suffix to avoid a dependency type collision. The SDK remains
async-only and does not change `ETag` or URI-valued properties to framework
value types as part of this migration.

## Automatic pagination (`AsyncPageable<T>`) and page-envelope metadata

In alignment with the [Azure SDK Design Guidelines for .NET](https://azure.github.io/azure-sdk/dotnet_introduction.html#dotnet-pagination),
32 additional list/query operations across 14 connectors now return
`Azure.AsyncPageable<TItem>` instead of `Task<TPage>`:

| Connector | Operations converted from `Task<TPage>` to `AsyncPageable<TItem>` | Page-model collection property |
| --- | --- | --- |
| `AzureBlob` | `ListFolderAsync` (`AsyncPageable<DataWithSensitivityLabelInfo>`) | `ListOfBlobsWithSensitivityLabels.Value` |
| `AzureTables` | `GetEntitiesAsync` (`AsyncPageable<EntityItem>`) | `GetEntitiesResponse.Value` (previously `ListOfEntities`) |
| `DocumentDB` | `QueryDocumentsAsync` (`AsyncPageable<ObjectWithoutType>`) | `QueryDocumentsResponse.Value` (previously `Documents`) |
| `ExcelOnline` | `GetItemsAsync` (`AsyncPageable<Item>`) | `ItemsList.Value` |
| `ExcelOnlineBusiness` | `GetItemsAsync` (`AsyncPageable<Item>`) | `ItemsList.Value` |
| `Insightly` | `ListProjectsAsync` (`AsyncPageable<Project>`) | `ListProjectsResponse.Value` |
| `Jira` | `ListFiltersAsync`, `ListProjectsAsync` (`AsyncPageable<JsonElement?>`) | `ListFiltersResponse.Value`, `ListProjectsResponse.Value` |
| `Office365` | `ContactGetItemsAsync` (`AsyncPageable<ContactResponse>`) | `EntityListResponseContactResponse.Value` |
| `OneNote` | `GetPagesInSectionAsync` (`AsyncPageable<JsonElement?>`) | `GetPagesInSectionResponse.Value` (previously `PagesInSectionValueObject`) |
| `Salesforce` | `GetTablesAsync`, `GetItemsAsync`, `GetItemsTableAccountAsync`, `GetItemsTableUserAsync`, `GetItemsTableCaseAsync`, `GetItemsTableOpportunityAsync`, `GetItemsTableProduct2Async`, `GetItemsTableContactAsync` | `TablesList.Value`, `ItemsList.Value` |
| `SharePointOnline` | `GetFileItemsAsync`, `GetItemsAsync` (`AsyncPageable<Item>`) | `ItemsList.Value` |
| `Sql` | `GetItemsAsync` (`AsyncPageable<SqlItem>`), `GetTablesForDeleteItemAsync`, `GetTablesForGetItemAsync`, `GetTablesForGetOnNewItemsAsync`, `GetTablesForGetOnUpdatedItemsAsync`, `GetTablesForPatchItemAsync`, `GetTablesForPostItemAsync` (`AsyncPageable<Table>`) | `GetItemsResponse.Value`, `TablesList.Value` |
| `Yammer` | `GetAllMessagesAsync`, `GetMessagesFollowingAsync`, `GetMessagesInGroupAsync`, `GetMessagesInThreadAsync` (`AsyncPageable<Message>`) | `PageableMessageList.Value` |
| `Zendesk` | `GetItemsAsync` (`AsyncPageable<Item>`) | `ItemsList.Value` |

Replace single-page `await client.GetItemsAsync(...)` calls with `await foreach`:

```csharp
await foreach (var item in client.GetItemsAsync("OneDrive", "book.xlsx", "Table1", "me"))
{
    // Process each row across all pages
}
```

### Accessing page-level sibling properties (`SensitivityLabelInfo`, `RequestCharge`, `TableMetadataLocation`)

When a paginated response envelope carries sibling properties alongside `Value`
and `NextLink` (such as `ExcelOnlineBusiness.Models.ItemsList.SensitivityLabelInfo`,
`DocumentDB.Models.QueryDocumentsResponse.RequestCharge`, or
`AzureTables.Models.GetEntitiesResponse.TableMetadataLocation`), enumerate pages
with `AsPages()`. Each yielded `Page<TItem>` is a
`ConnectorPage<TPage, TItem>` that exposes both the strongly typed
`PageModel` and a buffered `GetRawResponse()`:

```csharp
await foreach (var page in client.GetItemsAsync(
    "OneDrive",
    "book.xlsx",
    "Table1",
    "me",
    extractSensitivityLabel: true,
    sensitivityLabelMetadata: true).AsPages())
{
    if (page is ConnectorPage<ItemsList, Item> connectorPage)
    {
        IReadOnlyList<SensitivityLabelMetadata> labels = connectorPage.PageModel.SensitivityLabelInfo;
    }

    // Or via the standard Azure Core raw response:
    ItemsList? envelope = page.GetRawResponse().Content.ToObjectFromJson<ItemsList>();
}
```

## Typed dictionary properties (`Dictionary<string, TValue>`)

Properties whose Swagger schema declares an object map (`additionalProperties`
with a value schema and no `x-ms-dynamic-schema` marker) are now strongly typed
as `Dictionary<string, string>` or `Dictionary<string, ObjectEntity>` instead of
`JsonElement?`:

| Connector | Affected properties | New type |
| --- | --- | --- |
| `Arm` | `Properties`, `Tags` | `Dictionary<string, string>` |
| `AzureAD` | `CustomProperties` | `Dictionary<string, string>` |
| `AzureDataFactory` | `Tags` | `Dictionary<string, string>` |
| `CommonDataService` | `DynamicProperties` | `Dictionary<string, ObjectEntity>` / `Dictionary<string, string>` |
| `EventHubs` | `Properties` | `Dictionary<string, ObjectEntity>` |
| `ExcelOnline` / `ExcelOnlineBusiness` | `DynamicProperties` | `Dictionary<string, ObjectEntity>` |
| `Outlook` | `DynamicProperties` | `Dictionary<string, ObjectEntity>` |
| `Salesforce` | `DynamicProperties`, `QueryParameters` | `Dictionary<string, ObjectEntity>`, `Dictionary<string, string>` |
| `ServiceBus` | `Properties` | `Dictionary<string, string>` |
| `SharePointOnline` | `DynamicProperties`, `Headers` | `Dictionary<string, ObjectEntity>`, `Dictionary<string, string>` |
| `Sql` | `DynamicProperties`, `FormalParameters` | `Dictionary<string, ObjectEntity>`, `Dictionary<string, string>` |
| `Zendesk` | `DynamicProperties` | `Dictionary<string, ObjectEntity>` |
