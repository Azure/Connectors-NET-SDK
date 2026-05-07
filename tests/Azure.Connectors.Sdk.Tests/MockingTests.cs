//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Connectors.Sdk.Office365;
using Azure.Connectors.Sdk.Teams;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests that verify service methods are mockable via Moq.
    /// Validates issue #97: virtual methods + protected parameterless constructors.
    /// </summary>
    [TestClass]
    public class MockingTests
    {
        [TestMethod]
        public async Task Office365Client_MockVirtualMethod_ReturnsSetupValue()
        {
            // Arrange
            var expectedCategories = new List<GraphOutlookCategory>
            {
                new GraphOutlookCategory { Id = "cat-1", DisplayName = "Red Category" }
            };

            var mock = new Mock<Office365Client>();
            mock.Setup(client => client.GetOutlookCategoryNamesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedCategories);

            var client = mock.Object;

            // Act
            var result = await client
                .GetOutlookCategoryNamesAsync(cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Red Category", result[0].DisplayName);
            mock.Verify(client => client.GetOutlookCategoryNamesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task TeamsClient_MockVirtualMethod_ReturnsSetupValue()
        {
            // Arrange
            var expectedTeams = new GetAllTeamsResponse
            {
                TeamsList = new List<object> { "Engineering" }
            };

            var mock = new Mock<TeamsClient>();
            mock.Setup(client => client.GetAllTeamsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedTeams);

            var client = mock.Object;

            // Act
            var result = await client
                .GetAllTeamsAsync(cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.TeamsList.Count);
            Assert.AreEqual("Engineering", result.TeamsList[0]);
        }

        [TestMethod]
        public void Office365Client_MockConnectorName_CanBeOverridden()
        {
            // Arrange — ConnectorName is abstract on the base class, so Moq intercepts it.
            // Setting CallBase = true delegates to the concrete Office365Client override.
            var mock = new Mock<Office365Client>();
            mock.CallBase = true;

            // Act
            var client = mock.Object;

            // Assert
            Assert.IsNotNull(client);
            Assert.AreEqual("office365", client.ConnectorName);
        }

        [TestMethod]
        public async Task Office365Client_MockVoidMethod_DoesNotThrow()
        {
            // Arrange
            var mock = new Mock<Office365Client>();
            mock.Setup(client => client.SendEmailAsync(It.IsAny<SendEmailInput>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var client = mock.Object;

            // Act — should not throw
            await client
                .SendEmailAsync(new SendEmailInput(), cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            mock.Verify(client => client.SendEmailAsync(It.IsAny<SendEmailInput>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public void MockedClient_Dispose_DoesNotThrow()
        {
            // Arrange
            var mock = new Mock<Office365Client>();
            var client = mock.Object;

            // Act & Assert — disposing a mocked client should not throw
            client.Dispose();
        }
    }
}
