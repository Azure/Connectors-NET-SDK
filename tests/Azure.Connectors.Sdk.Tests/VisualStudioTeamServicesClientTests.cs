//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Connectors.Sdk.VisualStudioTeamServices;
using Azure.Connectors.Sdk.VisualStudioTeamServices.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Azure.Connectors.Sdk.Tests
{
    [TestClass]
    [TestCategory("CheckInTests")]
    [ExcludeFromCodeCoverage]
    public class VisualStudioTeamServicesClientTests
    {
        [TestMethod]
        [Owner("swapnilnagar")]
        public void ModelFactory_UrlAndUriProperties_RoundTripOriginalWireKeys()
        {
            const string address = "https://dev.azure.com/example/resource";
            var workItem = VisualStudioTeamServicesModelFactory.WorkItemResult(url: address);
            var account = VisualStudioTeamServicesModelFactory.Account(accountUri: address);
            var member = VisualStudioTeamServicesModelFactory.TeamMember(memberUri: address, imageUri: address);

            var workItemJson = JsonSerializer.SerializeToElement(workItem);
            var accountJson = JsonSerializer.SerializeToElement(account);
            var memberJson = JsonSerializer.SerializeToElement(member);

            Assert.AreEqual(expected: address, actual: workItemJson.GetProperty("url").GetString());
            Assert.AreEqual(expected: address, actual: accountJson.GetProperty("accountUri").GetString());
            Assert.AreEqual(expected: address, actual: memberJson.GetProperty("url").GetString());
            Assert.AreEqual(expected: address, actual: memberJson.GetProperty("imageUrl").GetString());
            Assert.AreEqual(expected: address, actual: JsonSerializer.Deserialize<WorkItemResult>(workItemJson)!.Url);
            Assert.AreEqual(expected: address, actual: JsonSerializer.Deserialize<Account>(accountJson)!.AccountUri);
            var roundTripMember = JsonSerializer.Deserialize<TeamMember>(memberJson)!;
            Assert.AreEqual(expected: address, actual: roundTripMember.MemberUri);
            Assert.AreEqual(expected: address, actual: roundTripMember.ImageUri);
        }

        [TestMethod]
        [DataRow("[]", 0)]
        [DataRow("[{\"id\":\"project-1\",\"name\":\"SDK\",\"url\":\"https://dev.azure.com/example/project-1\"}]", 1)]
        [Owner("swapnilnagar")]
        public async Task ListProjectsAsync_Response_UsesOrganizationQueryAndBindsProjects(string values, int count)
        {
            var setup = ConnectorTestHelpers.CreateCapturingClientSetup(
                () => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent($"{{\"value\":{values}}}"),
                });
            using var client = new VisualStudioTeamServicesClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: setup.Credential,
                options: setup.Options);

            var projects = await client
                .ListProjectsAsync(organizationName: "organization & team")
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: ConnectorNames.AzureDevOps, actual: client.ConnectorName);
            Assert.AreEqual(expected: count, actual: projects.Value.Count);
            Assert.AreEqual(expected: values, actual: JsonSerializer.Serialize(projects.Value));
            var request = setup.GetLastRequest();
            Assert.IsNotNull(request);
            Assert.AreEqual(expected: HttpMethod.Get, actual: request.Method);
            Assert.AreEqual(
                expected: "/connection/_apis/projects?account=organization%20%26%20team",
                actual: request.RequestUri!.PathAndQuery);
        }

        [TestMethod]
        [DataRow("[]", 0)]
        [DataRow("[{\"System.Id\":41}]", 1)]
        [Owner("swapnilnagar")]
        public async Task GetQueryResultsAsync_Continuation_ReturnsLaterItemsAndStops(string firstValues, int firstCount)
        {
            const string nextLink = "https://test.azure.com/connection/query/continuation?page=2";
            var responses = new Queue<string>(new[]
            {
                $"{{\"value\":{firstValues},\"@odata.nextLink\":\"{nextLink}\"}}",
                """{"value":[{"System.Id":42,"System.Title":"Later item","Custom.Owner":"SDK"}]}""",
            });
            var setup = ConnectorTestHelpers.CreateCapturingClientSetup(
                () => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responses.Dequeue()),
                });
            using var client = new VisualStudioTeamServicesClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: setup.Credential,
                options: setup.Options);

            var pageable = client.GetQueryResultsAsync(
                projectName: "SDK",
                queryId: "query-1",
                organizationName: "example",
                numberOfWorkItemsToReturn: 2);
            Assert.IsNull(setup.GetLastRequest());

            var requestedUris = new List<string>();
            var items = new List<QueryResultWorkItemResponse>();
            await foreach (var page in pageable.AsPages().ConfigureAwait(continueOnCapturedContext: false))
            {
                requestedUris.Add(setup.GetLastRequest()!.RequestUri!.AbsoluteUri);
                items.AddRange(page.Values);
            }

            CollectionAssert.AreEqual(
                expected: new[]
                {
                    "https://test.azure.com/connection/v2/SDK/queryResults/query-1?account=example&workItemsCount=2",
                    nextLink,
                },
                actual: requestedUris);
            CollectionAssert.AreEqual(
                expected: firstCount == 0 ? new[] { 42 } : new[] { 41, 42 },
                actual: items.Select(item => item.Id!.Value).ToArray());
            Assert.AreEqual(
                expected: "SDK",
                actual: items.Single(item => item.Id == 42).AdditionalProperties["Custom.Owner"].GetString());
            Assert.AreEqual(expected: 0, actual: responses.Count);
        }

        [TestMethod]
        [Owner("swapnilnagar")]
        public async Task QueueNewBuildAsync_Request_SerializesSwaggerKeysAndBindsBuild()
        {
            var setup = ConnectorTestHelpers.CreateContentCapturingClientSetup(
                () => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"id":42,"buildNumber":"20261027.1","status":"notStarted","sourceBranch":"refs/heads/main"}"""),
                });
            using var client = new VisualStudioTeamServicesClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: setup.Credential,
                options: setup.Options);

            var build = await client
                .QueueNewBuildAsync(
                    projectName: "SDK",
                    input: new QueueBuildRequest
                    {
                        SourceBranch = "refs/heads/main",
                        Parameters = """{"configuration":"Release"}""",
                    },
                    organizationName: "example",
                    buildDefinitionId: "7")
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 42, actual: build.BuildId);
            Assert.AreEqual(expected: "20261027.1", actual: build.BuildNumber);
            Assert.AreEqual(expected: "notStarted", actual: build.Status);
            Assert.AreEqual(expected: "application/json", actual: setup.Capture.ContentType);
            using var body = JsonDocument.Parse(setup.Capture.Body!);
            CollectionAssert.AreEquivalent(
                expected: new[] { "sourceBranch", "parameters" },
                actual: body.RootElement.EnumerateObject().Select(property => property.Name).ToArray());
            Assert.AreEqual(
                expected: "refs/heads/main",
                actual: body.RootElement.GetProperty("sourceBranch").GetString());
            Assert.AreEqual(
                expected: """{"configuration":"Release"}""",
                actual: body.RootElement.GetProperty("parameters").GetString());
        }

        [TestMethod]
        [Owner("swapnilnagar")]
        public async Task QueueNewBuildAsync_Request_EncodesProjectAndDefinitionQuery()
        {
            var setup = ConnectorTestHelpers.CreateCapturingClientSetup(
                () => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"id":42}"""),
                });
            using var client = new VisualStudioTeamServicesClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: setup.Credential,
                options: setup.Options);

            await client
                .QueueNewBuildAsync(
                    projectName: "SDK/project",
                    input: new QueueBuildRequest { SourceBranch = "refs/heads/main" },
                    organizationName: "example",
                    buildDefinitionId: "7 & 8")
                .ConfigureAwait(continueOnCapturedContext: false);

            var request = setup.GetLastRequest();
            Assert.IsNotNull(request);
            Assert.AreEqual(expected: HttpMethod.Post, actual: request.Method);
            Assert.AreEqual(
                expected: "/connection/SDK%2Fproject/_apis/build/builds?account=example&buildDefId=7%20%26%208",
                actual: request.RequestUri!.PathAndQuery);
        }

        [TestMethod]
        [Owner("swapnilnagar")]
        public async Task ListProjectsAsync_UnauthorizedResponse_PreservesServiceError()
        {
            var setup = ConnectorTestHelpers.CreateMockedClientSetup(
                () => new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("""{"error":{"code":"Unauthorized","message":"Authorization is required."}}"""),
                });
            using var client = new VisualStudioTeamServicesClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: setup.Credential,
                options: setup.Options);

            var error = await Assert
                .ThrowsExactlyAsync<ConnectorException>(() => client.ListProjectsAsync(organizationName: "example"))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 401, actual: error.Status);
            Assert.AreEqual(expected: "Unauthorized", actual: error.ErrorCode);
        }

        [TestMethod]
        [DataRow("""{"body":{"id":42,"buildNumber":"20261027.1","result":"succeeded"}}""")]
        [DataRow("""{"body":{"value":[{"id":42,"buildNumber":"20261027.1","result":"succeeded"}]}}""")]
        [Owner("swapnilnagar")]
        public void OnBuildCompleted_Callback_DeserializesSingleAndBatchPayloads(string json)
        {
            var callback = JsonSerializer.Deserialize<VisualStudioTeamServicesOnBuildCompletedTriggerPayload>(json);

            var build = callback!.Body!.Value!.Single();
            Assert.AreEqual(expected: 42, actual: build.BuildId);
            Assert.AreEqual(expected: "succeeded", actual: build.Result);
            Assert.AreEqual(
                expected: typeof(VisualStudioTeamServicesOnBuildCompletedTriggerPayload),
                actual: VisualStudioTeamServicesTriggers.Operations[VisualStudioTeamServicesTriggerOperations.OnBuildCompleted]);
            Assert.IsNull(typeof(VisualStudioTeamServicesClient).GetMethod("OnBuildCompletedAsync"));
        }

        [TestMethod]
        [Owner("swapnilnagar")]
        public async Task SearchWorkItemsAsync_NonemptyResponse_PreservesScalarAndStructuredFields()
        {
            var setup = ConnectorTestHelpers.CreateCapturingClientSetup(
                () => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                        {"count":1,"results":[{"fields":{"system.id":"2","system.title":"Rest Api User Story","custom.number":3,"custom.flag":true,"custom.owner":{"name":"SDK"},"custom.empty":null}}]}
                        """),
                });
            using var client = new VisualStudioTeamServicesClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: setup.Credential,
                options: setup.Options);

            var response = await client.SearchWorkItemsAsync(
                input: new WorkItemSearchRequest(),
                organizationName: "example",
                projectName: "SDK").ConfigureAwait(continueOnCapturedContext: false);

            var fields = response.Results.Single().Fields;
            Assert.AreEqual(expected: "2", actual: fields["system.id"]!.Value.GetString());
            Assert.AreEqual(expected: "Rest Api User Story", actual: fields["system.title"]!.Value.GetString());
            Assert.AreEqual(expected: 3, actual: fields["custom.number"]!.Value.GetInt32());
            Assert.IsTrue(fields["custom.flag"]!.Value.GetBoolean());
            Assert.AreEqual(expected: "SDK", actual: fields["custom.owner"]!.Value.GetProperty("name").GetString());
            Assert.IsNull(fields["custom.empty"]);
        }

        [TestMethod]
        [DataRow(false, "squash")]
        [DataRow(true, "futureStrategy")]
        [Owner("swapnilnagar")]
        public void OnGitPullClosed_PopulatedCallback_BindsBooleanAndStringStrategy(bool truncated, string strategy)
        {
            var json = JsonSerializer.Serialize(new
            {
                body = new
                {
                    pullRequestId = 42,
                    commits = new[] { new { commentTruncated = truncated } },
                    completionOptions = new { mergeStrategy = strategy },
                },
            });
            var callback = JsonSerializer.Deserialize<VisualStudioTeamServicesOnGitPullClosedTriggerPayload>(json);
            var pullRequest = callback!.Body!.Value!.Single();

            Assert.AreEqual(expected: truncated, actual: pullRequest.Commits.Single().CommitCommentTruncated);
            Assert.AreEqual(expected: strategy, actual: pullRequest.CompletionOptions.MergeStrategy);
            var roundTrip = JsonSerializer.Deserialize<VisualStudioTeamServicesOnGitPullClosedTriggerPayload>(JsonSerializer.Serialize(callback))!;
            Assert.AreEqual(expected: strategy, actual: roundTrip.Body!.Value!.Single().CompletionOptions.MergeStrategy);
        }

        [TestMethod]
        [Owner("swapnilnagar")]
        public async Task GetWorkItemDetailsAsync_ProductionRevision_BindsProductionModelAndRoute()
        {
            var setup = ConnectorTestHelpers.CreateCapturingClientSetup(
                () => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"id":42,"fields":{"System_Id":42,"System_Title":"Production work item"}}"""),
                });
            using var client = new VisualStudioTeamServicesClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: setup.Credential,
                options: setup.Options);

            DynamicWorkItemResponse response = await client.GetWorkItemDetailsAsync(
                workItemId: "42",
                organizationName: "example",
                projectName: "SDK",
                workItemType: "Task").ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 42, actual: response.Fields!.Value.GetProperty("System_Id").GetInt32());
            Assert.AreEqual(expected: "/connection/_apis/wit/workitems/42", actual: setup.GetLastRequest()!.RequestUri!.AbsolutePath);
            Assert.IsNull(typeof(VisualStudioTeamServicesClient).GetMethod("GetWorkItemDetailsV2Async"));
        }

        [TestMethod]
        [Owner("swapnilnagar")]
        public async Task MockConstructor_VirtualOperation_ReturnsFactoryModel()
        {
            var build = VisualStudioTeamServicesModelFactory.BuildResult(buildId: 42);
            var client = new Mock<VisualStudioTeamServicesClient>();
            client.Setup(instance => instance.GetBuildAsync("SDK", 42, "example", CancellationToken.None))
                .ReturnsAsync(build);

            var response = await client.Object
                .GetBuildAsync(projectName: "SDK", buildId: 42, organizationName: "example")
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreSame(expected: build, actual: response);
        }
    }
}
