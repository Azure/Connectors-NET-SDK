//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure.Connectors.Sdk.Sql;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    [TestClass]
    public class SqlClientTests
    {
        private static readonly Mock<TokenCredential> SharedMockCredential = CreateMockCredential();

        private static Mock<TokenCredential> CreateMockCredential()
        {
            var mock = new Mock<TokenCredential>();
            mock.Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1)));
            return mock;
        }

        private static SqlClient CreateMockedClient(HttpResponseMessage response)
        {
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response)
                .Callback(() => { })
                .Verifiable();

            var options = new ConnectorClientOptions();
            options.Transport = new HttpClientTransport(new HttpClient(mockHandler.Object));
            options.Retry.MaxRetries = 0;

            return new SqlClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object,
                options: options);
        }

        [TestMethod]
        public void Constructor_WithValidUrl_ShouldCreateInstance()
        {
            using var client = new SqlClient("https://test.azure.com/connection");
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNullUrl_ShouldThrowArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new SqlClient((string)null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            var client = new SqlClient("https://test.azure.com/connection");
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            var client = new SqlClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object);
            client.Dispose();
            client.Dispose();
        }

        [TestMethod]
        public async Task GetDatabasesAsync_WithMockedResponse_ReturnsExpected()
        {
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            };

            using var client = CreateMockedClient(responseMessage);

            var result = await client
                .GetDatabasesAsync(serverName: "server1",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task GetDatabasesAsync_WithErrorResponse_ThrowsConnectorException()
        {
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\": \"Bad request\"}")
            };

            using var client = CreateMockedClient(responseMessage);

            await Assert.ThrowsExactlyAsync<ConnectorException>(() =>
                client.GetDatabasesAsync(serverName: "server1",
                    cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        [TestMethod]
        public async Task GetProceduresAsync_TargetsDefaultDatasetRoute()
        {
            var request = await CaptureRequestAsync(
                    responseJson: "{\"value\":[]}",
                    invoke: client => client.GetProceduresAsync(cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(
                expected: "/connection/datasets/default/procedures",
                actual: request.RequestUri!.AbsolutePath,
                message: "The default-dataset discovery route was previously replaced by its server/database-scoped V2 sibling, which shares its version-stripped base name.");
        }

        [TestMethod]
        public async Task GetProceduresV2Async_TargetsServerAndDatabaseScopedRoute()
        {
            var request = await CaptureRequestAsync(
                    responseJson: "{\"value\":[]}",
                    invoke: client => client.GetProceduresV2Async(
                        serverName: "server1",
                        databaseName: "db1",
                        cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(
                expected: "/connection/v2/datasets/server1,db1/procedures",
                actual: request.RequestUri!.AbsolutePath,
                message: "The V2 discovery route keeps its version affix so it stays separately callable from the default-dataset route.");
        }

        [TestMethod]
        public async Task GetTablesAsync_StillTargetsVersionedRoute()
        {
            // NOTE(daviburg): The unversioned GetTables route is marked deprecated in Swagger while
            // GetTables_V2 is not, so collapsing to the V2 route remains correct. This guards against
            // over-correcting the fix.
            var request = await CaptureRequestAsync(
                    responseJson: "{\"value\":[]}",
                    invoke: client => client.GetTablesAsync(
                        serverName: "server1",
                        databaseName: "db1",
                        cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(
                expected: "/connection/v2/datasets/server1,db1/tables",
                actual: request.RequestUri!.AbsolutePath,
                message: "A deprecated unversioned route must still be superseded by its current versioned route.");
        }

        [TestMethod]
        public async Task GetProcedureAsync_TargetsDefaultDatasetRoute()
        {
            var request = await CaptureRequestAsync(
                    responseJson: "{}",
                    invoke: client => client.GetProcedureAsync(procedureName: "sp_who", cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(
                expected: "/connection/$metadata.json/datasets/default/procedures/sp_who",
                actual: request.RequestUri!.AbsolutePath,
                message: "The default-dataset procedure metadata route must be restored.");
        }

        [TestMethod]
        public async Task GetProcedureV2Async_TargetsServerAndDatabaseScopedRoute()
        {
            var request = await CaptureRequestAsync(
                    responseJson: "{}",
                    invoke: client => client.GetProcedureV2Async(
                        serverName: "server1",
                        databaseName: "db1",
                        procedureName: "sp_who",
                        cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(
                expected: "/connection/v2/$metadata.json/datasets/server1,db1/procedures/sp_who",
                actual: request.RequestUri!.AbsolutePath,
                message: "The scoped procedure metadata route keeps its own identity.");
        }

        [TestMethod]
        public async Task GetTableAsync_TargetsServerAndDatabaseScopedRoute()
        {
            var request = await CaptureRequestAsync(
                    responseJson: "{}",
                    invoke: client => client.GetTableAsync(
                        serverName: "server1",
                        databaseName: "db1",
                        tableName: "Orders",
                        cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(
                expected: "/connection/v2/$metadata.json/datasets/server1,db1/tables/Orders",
                actual: request.RequestUri!.AbsolutePath,
                message: "The scoped table metadata route keeps its own identity.");
        }

        [TestMethod]
        public async Task GetPassThroughNativeQueryMetadataAsync_TargetsServerAndDatabaseScopedRoute()
        {
            var request = await CaptureRequestAsync(
                    responseJson: "{}",
                    invoke: client => client.GetPassThroughNativeQueryMetadataAsync(
                        serverName: "server1",
                        databaseName: "db1",
                        input: new Sql.Models.SqlPassThroughNativeQueryBody(),
                        cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(
                expected: "/connection/v2/$metadata.json/datasets/server1,db1/query/sql",
                actual: request.RequestUri!.AbsolutePath,
                message: "The scoped native query metadata route keeps its own identity.");
        }

        [TestMethod]
        public async Task ExecuteProcedureV0Async_TargetsDefaultDatasetRoute()
        {
            var request = await CaptureRequestAsync(
                    responseJson: "{}",
                    invoke: client => client.ExecuteProcedureV0Async(
                        procedureName: "sp_who",
                        input: new Sql.Models.ExecuteProcedureV0Input(),
                        cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(
                expected: "/connection/datasets/default/procedures/sp_who",
                actual: request.RequestUri!.AbsolutePath);
        }

        [TestMethod]
        public async Task ExecuteProcedureAsync_TargetsServerAndDatabaseScopedRoute()
        {
            var request = await CaptureRequestAsync(
                    responseJson: "{}",
                    invoke: client => client.ExecuteProcedureAsync(
                        serverName: "server1",
                        databaseName: "db1",
                        procedureName: "sp_who",
                        input: new Sql.Models.ExecuteProcedureInput(),
                        cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(
                expected: "/connection/v2/datasets/server1,db1/procedures/sp_who",
                actual: request.RequestUri!.AbsolutePath);
        }

        private static async Task<HttpRequestMessage> CaptureRequestAsync(
            string responseJson,
            Func<SqlClient, Task> invoke)
        {
            var clientSetup = ConnectorTestHelpers.CreateCapturingClientSetup(
                () => new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson)
                });

            using var client = new SqlClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: clientSetup.Credential,
                options: clientSetup.Options);

            await invoke(client).ConfigureAwait(continueOnCapturedContext: false);

            var request = clientSetup.GetLastRequest();
            Assert.IsNotNull(request);
            return request!;
        }
    }
}
