//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Connectors.Sdk.Dataflows;
using Azure.Connectors.Sdk.Dataflows.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Azure.Connectors.Sdk.Tests
{
    [TestClass]
    [TestCategory("CheckInTests")]
    [ExcludeFromCodeCoverage]
    public class DataflowsClientTests
    {
        [TestMethod]
        [Owner("swapnilnagar")]
        public async Task RefreshDataflowAsync_AcceptedResponse_UsesEncodedRouteAndReturnsModel()
        {
            var setup = ConnectorTestHelpers.CreateCapturingClientSetup(
                () => new HttpResponseMessage(HttpStatusCode.Accepted)
                {
                    Content = new StringContent("""{"dataflowId":"flow-1","dataflowName":"Sales"}"""),
                });
            using var client = new DataflowsClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: setup.Credential,
                options: setup.Options);

            var dataflow = await client
                .RefreshDataflowAsync(group: "group/one", dataflow: "flow one", groupType: "Workspace")
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: ConnectorNames.PowerQueryDataflows, actual: client.ConnectorName);
            Assert.AreEqual(expected: "flow-1", actual: dataflow.DataflowId);
            Assert.AreEqual(expected: "Sales", actual: dataflow.DataflowName);
            var request = setup.GetLastRequest();
            Assert.IsNotNull(request);
            Assert.AreEqual(expected: HttpMethod.Post, actual: request.Method);
            Assert.AreEqual(
                expected: "https://test.azure.com/connection/api/groups/group%2Fone/dataflows/flow%20one/refreshdataflow?workspaceType=Workspace",
                actual: request.RequestUri!.AbsoluteUri);
        }

        [TestMethod]
        [Owner("swapnilnagar")]
        public async Task RefreshDataflowAsync_ForbiddenResponse_PreservesServiceError()
        {
            var setup = ConnectorTestHelpers.CreateMockedClientSetup(
                () => new HttpResponseMessage(HttpStatusCode.Forbidden)
                {
                    Content = new StringContent("""{"error":{"code":"AccessDenied","message":"Refresh is not permitted."}}"""),
                });
            using var client = new DataflowsClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: setup.Credential,
                options: setup.Options);

            var error = await Assert
                .ThrowsExactlyAsync<ConnectorException>(() => client.RefreshDataflowAsync(
                    group: "group-1",
                    dataflow: "flow-1",
                    groupType: "Workspace"))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 403, actual: error.Status);
            Assert.AreEqual(expected: "AccessDenied", actual: error.ErrorCode);
        }

        [TestMethod]
        [DataRow("[]", 0)]
        [DataRow("[{\"id\":\"group-1\",\"name\":\"Sales\"}]", 1)]
        [Owner("swapnilnagar")]
        public async Task ListGroupsForRefreshDataflowAsync_Response_BindsDiscoveryValues(string values, int count)
        {
            var setup = ConnectorTestHelpers.CreateMockedClientSetup(
                () => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent($"{{\"value\":{values}}}"),
                });
            using var client = new DataflowsClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: setup.Credential,
                options: setup.Options);

            var groups = await client
                .ListGroupsForRefreshDataflowAsync(groupType: "Workspace")
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: count, actual: groups.Value.Count);
            Assert.AreEqual(
                expected: values,
                actual: JsonSerializer.Serialize(groups.Value));
        }

        [TestMethod]
        [DataRow("[]")]
        [DataRow("[{\"objectId\":\"flow-1\",\"name\":\"Sales\"}]")]
        [Owner("swapnilnagar")]
        public async Task ListDataflowsForRefreshDataflowAsync_Response_BindsNamespaceCollisionModel(string values)
        {
            var setup = ConnectorTestHelpers.CreateCapturingClientSetup(
                () => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent($"{{\"value\":{values}}}"),
                });
            using var client = new DataflowsClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: setup.Credential,
                options: setup.Options);

            var dataflows = await client
                .ListDataflowsForRefreshDataflowAsync(group: "group-1", groupType: "Environment")
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: values, actual: JsonSerializer.Serialize(dataflows.Value));
            Assert.AreEqual(
                expected: "/connection/api/group/group-1/listdataflows/forrefreshdataflow?workspaceType=Environment",
                actual: setup.GetLastRequest()!.RequestUri!.PathAndQuery);
        }

        [TestMethod]
        [Owner("swapnilnagar")]
        public void OnRefreshComplete_Callback_DeserializesRefreshStatusAndKeepsRegistrationMetadata()
        {
            var callback = JsonSerializer.Deserialize<TriggerCallbackPayload<RefreshModel>>(
                """{"body":{"dataflowId":"flow-1","dataflowName":"Sales","refreshType":"OnDemand","status":"Success","startTime":"2026-10-27T18:00:00Z","endTime":"2026-10-27T18:01:00Z"}}""");

            var refresh = callback!.Body!.Value!.Single();
            Assert.AreEqual(expected: "flow-1", actual: refresh.DataflowId);
            Assert.AreEqual(expected: "Success", actual: refresh.RefreshStatus);
            Assert.AreEqual(expected: "2026-10-27T18:01:00Z", actual: refresh.EndTime);
            CollectionAssert.AreEquivalent(
                expected: new[] { "workspaceType", "groupIdForOnRefreshComplete", "dataflowIdForOnRefreshComplete" },
                actual: typeof(DataflowsTriggerParameters.OnRefreshComplete)
                    .GetFields()
                    .Select(field => (string)field.GetRawConstantValue()!)
                    .ToArray());
            Assert.IsNull(typeof(DataflowsClient).GetMethod($"{DataflowsTriggerOperations.OnRefreshComplete}Async"));
        }

        [TestMethod]
        [Owner("swapnilnagar")]
        public async Task MockConstructor_VirtualOperation_ReturnsFactoryModel()
        {
            var dataflow = DataflowsModelFactory.DataflowModel(dataflowName: "Sales", dataflowId: "flow-1");
            var client = new Mock<DataflowsClient>();
            client.Setup(instance => instance.RefreshDataflowAsync("group-1", "flow-1", "Workspace", CancellationToken.None))
                .ReturnsAsync(dataflow);

            var response = await client.Object
                .RefreshDataflowAsync(group: "group-1", dataflow: "flow-1", groupType: "Workspace")
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreSame(expected: dataflow, actual: response);
        }
    }
}
