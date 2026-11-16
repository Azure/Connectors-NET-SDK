//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Connectors.Sdk.AzureMonitorLogs;
using Azure.Connectors.Sdk.AzureQueues;
using Azure.Connectors.Sdk.AzureTables;
using Azure.Connectors.Sdk.AzureTables.Models;
using Azure.Connectors.Sdk.DocumentDB;
using Azure.Connectors.Sdk.OneDriveForBusiness;
using Azure.Connectors.Sdk.OneNote;
using Azure.Connectors.Sdk.PowerBI;
using Azure.Connectors.Sdk.Teams;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Connectors.Sdk.Tests
{
    [TestClass]
    public class RawStringBodyContractTests
    {
        [TestMethod]
        [DataRow("QuerySchema")]
        [DataRow("PutMessage")]
        [DataRow("CreateTable")]
        [DataRow("ExecuteStoredProcedure")]
        [DataRow("CreatePageInSection")]
        [DataRow("CreatePageInQuickNotes")]
        [DataRow("GoalValueCheckinNote")]
        [DataRow("FlowContinuation")]
        public async Task StringBodyOperations_WithTextPayload_PreserveExactUtf8(string operation)
        {
            var payload = operation switch
            {
                "CreateTable" => "SdkRawBody",
                "ExecuteStoredProcedure" => "[\"caf\u00e9\",{\"nested\":true}]",
                "CreatePageInSection" or "CreatePageInQuickNotes" => "<html><body>caf\u00e9 \"quoted\"</body></html>",
                "FlowContinuation" => "{\"card\":{\"type\":\"AdaptiveCard\",\"body\":[]}}",
                _ => "caf\u00e9 \"quoted\"\nsecond line\\path",
            };
            var setup = ConnectorTestHelpers.CreateContentCapturingClientSetup(
                () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
            var endpoint = new Uri("https://test.azure.com/connection");

            switch (operation)
            {
                case "QuerySchema":
                    using (var client = new AzureMonitorLogsClient(endpoint, setup.Credential, setup.Options))
                    {
                        await client
                            .QuerySchemaAsync(input: payload, subscription: "subscription", resourceGroup: "group", resourceType: "type", resourceName: "resource", cancellationToken: CancellationToken.None)
                            .ConfigureAwait(continueOnCapturedContext: false);
                    }
                    break;
                case "PutMessage":
                    using (var client = new AzureQueuesClient(endpoint, setup.Credential, setup.Options))
                    {
                        await client
                            .PutMessageAsync(storageAccountNameOrQueueEndpoint: "AccountNameFromSettings", queueName: "queue", input: payload, cancellationToken: CancellationToken.None)
                            .ConfigureAwait(continueOnCapturedContext: false);
                    }
                    break;
                case "CreateTable":
                    using (var client = new AzureTablesClient(endpoint, setup.Credential, setup.Options))
                    {
                        await client
                            .CreateTableAsync(storageAccountNameOrTableEndpoint: "AccountNameFromSettings", input: payload, cancellationToken: CancellationToken.None)
                            .ConfigureAwait(continueOnCapturedContext: false);
                    }
                    break;
                case "ExecuteStoredProcedure":
                    using (var client = new DocumentDBClient(endpoint, setup.Credential, setup.Options))
                    {
                        await client
                            .ExecuteStoredProcedureAsync(azureCosmosDBAccountName: "account", databaseId: "database", collectionId: "collection", sprocId: "procedure", input: payload, cancellationToken: CancellationToken.None)
                            .ConfigureAwait(continueOnCapturedContext: false);
                    }
                    break;
                case "CreatePageInSection":
                    using (var client = new OneNoteClient(endpoint, setup.Credential, setup.Options))
                    {
                        await client
                            .CreatePageInSectionAsync(input: payload, notebookKey: "notebook", notebookSection: "section", cancellationToken: CancellationToken.None)
                            .ConfigureAwait(continueOnCapturedContext: false);
                    }
                    break;
                case "CreatePageInQuickNotes":
                    using (var client = new OneNoteClient(endpoint, setup.Credential, setup.Options))
                    {
                        await client
                            .CreatePageInQuickNotesAsync(input: payload, cancellationToken: CancellationToken.None)
                            .ConfigureAwait(continueOnCapturedContext: false);
                    }
                    break;
                case "GoalValueCheckinNote":
                    using (var client = new PowerBIClient(endpoint, setup.Credential, setup.Options))
                    {
                        await client
                            .GoalValueCheckinNoteAsync(workspace: "workspace", scorecardId: "scorecard", goalId: "goal", checkInDate: "2026-01-01", input: payload, cancellationToken: CancellationToken.None)
                            .ConfigureAwait(continueOnCapturedContext: false);
                    }
                    break;
                case "FlowContinuation":
                    using (var client = new TeamsClient(endpoint, setup.Credential, setup.Options))
                    {
                        await client
                            .GetFlowContinuationSubscriptionWithPosterOutputMetadataAsync(postAs: "Flow bot", typeOfTheRecipientOfTheAction: "Chat", input: payload, cancellationToken: CancellationToken.None)
                            .ConfigureAwait(continueOnCapturedContext: false);
                    }
                    break;
                default:
                    throw new ArgumentException(message: "Unknown raw-string test operation.", paramName: nameof(operation));
            }

            CollectionAssert.AreEqual(expected: Encoding.UTF8.GetBytes(payload), actual: setup.Capture.Body);
            var expectedMediaType = string.Equals(operation, "CreatePageInSection", StringComparison.Ordinal)
                ? "text/plain"
                : "application/json";
            Assert.AreEqual(expected: expectedMediaType, actual: setup.Capture.ContentType);
        }

        [TestMethod]
        public async Task StringBodyOperation_WithNullInput_PreservesAbsentBody()
        {
            var setup = ConnectorTestHelpers.CreateContentCapturingClientSetup(
                () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"TableName\":\"SdkNullBody\"}") });
            using var client = new AzureTablesClient(new Uri("https://test.azure.com/connection"), setup.Credential, setup.Options);
            await client
                .CreateTableAsync(storageAccountNameOrTableEndpoint: "AccountNameFromSettings", input: null!, cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.IsNull(setup.Capture.Body);
            Assert.IsNull(setup.Capture.ContentType);
        }

        [TestMethod]
        public async Task StringBodyOperation_WithEmptyInput_UsesZeroBytes()
        {
            var setup = ConnectorTestHelpers.CreateContentCapturingClientSetup(
                () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"TableName\":\"SdkEmptyBody\"}") });
            using var client = new AzureTablesClient(new Uri("https://test.azure.com/connection"), setup.Credential, setup.Options);
            await client
                .CreateTableAsync(storageAccountNameOrTableEndpoint: "AccountNameFromSettings", input: string.Empty, cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            CollectionAssert.AreEqual(expected: Array.Empty<byte>(), actual: setup.Capture.Body);
        }
        [TestMethod]
        public async Task ObjectBodyOperation_WithEntityInput_RemainsJsonSerialized()
        {
            var input = new CreateEntityInput
            {
                AdditionalProperties = new Dictionary<string, JsonElement>
                {
                    ["PartitionKey"] = JsonSerializer.SerializeToElement("partition"),
                    ["RowKey"] = JsonSerializer.SerializeToElement("row"),
                    ["Message"] = JsonSerializer.SerializeToElement("caf\u00e9 \"quoted\""),
                },
            };
            var setup = ConnectorTestHelpers.CreateContentCapturingClientSetup(
                () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"PartitionKey\":\"partition\",\"RowKey\":\"row\"}") });
            using var client = new AzureTablesClient(new Uri("https://test.azure.com/connection"), setup.Credential, setup.Options);
            await client
                .CreateEntityAsync(storageAccountNameOrTableEndpoint: "AccountNameFromSettings", table: "table", input: input, cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            using var body = JsonDocument.Parse(setup.Capture.Body!);
            Assert.AreEqual(expected: JsonValueKind.Object, actual: body.RootElement.ValueKind);
            Assert.AreEqual(expected: "caf\u00e9 \"quoted\"", actual: body.RootElement.GetProperty("Message").GetString());
            Assert.AreEqual(expected: "application/json", actual: setup.Capture.ContentType);
        }

        [TestMethod]
        public async Task BinaryBodyOperation_WithArbitraryBytes_RemainsUnencoded()
        {
            var bytes = new byte[] { 0x00, 0x01, 0x7f, 0x80, 0xff };
            var setup = ConnectorTestHelpers.CreateContentCapturingClientSetup(
                () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"Id\":\"file\",\"Name\":\"sample.bin\"}") });
            using var client = new OneDriveForBusinessClient(new Uri("https://test.azure.com/connection"), setup.Credential, setup.Options);
            await client
                .CreateFileAsync(input: bytes, folderPath: "/", fileName: "sample.bin", cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            CollectionAssert.AreEqual(expected: bytes, actual: setup.Capture.Body);
            Assert.AreEqual(expected: "application/octet-stream", actual: setup.Capture.ContentType);
        }
    }
}
