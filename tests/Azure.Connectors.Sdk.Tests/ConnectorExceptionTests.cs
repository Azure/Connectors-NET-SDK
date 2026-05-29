//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using Azure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for <see cref="ConnectorException"/> ErrorCode parsing (#155).
    /// </summary>
    [TestClass]
    public class ConnectorExceptionTests
    {
        [TestMethod]
        public void Constructor_WithTopLevelCode_ParsesErrorCode()
        {
            // Arrange
            var responseBody = """{"code":"Forbidden","message":"The caller does not have permission"}""";

            // Act
            var ex = new ConnectorException("office365", "POST /Mail", 403, responseBody);

            // Assert
            Assert.AreEqual("Forbidden", ex.ErrorCode);
            Assert.AreEqual(403, ex.Status);
            Assert.AreEqual("office365", ex.ConnectorName);
            Assert.AreEqual("POST /Mail", ex.Operation);
            Assert.AreEqual(responseBody, ex.ResponseBody);
        }

        [TestMethod]
        public void Constructor_WithNestedErrorCode_ParsesErrorCode()
        {
            // Arrange — Azure-style error envelope
            var responseBody = """{"error":{"code":"TooManyRequests","message":"Rate limit exceeded"}}""";

            // Act
            var ex = new ConnectorException("teams", "GET /teams", 429, responseBody);

            // Assert
            Assert.AreEqual("TooManyRequests", ex.ErrorCode);
        }

        [TestMethod]
        public void Constructor_WithNoCodeField_ErrorCodeIsNull()
        {
            // Arrange — valid JSON but no "code" property
            var responseBody = """{"message":"Something went wrong"}""";

            // Act
            var ex = new ConnectorException("sharepoint", "GET /items", 500, responseBody);

            // Assert
            Assert.IsNull(ex.ErrorCode);
        }

        [TestMethod]
        public void Constructor_WithInvalidJson_ErrorCodeIsNull()
        {
            // Arrange — not valid JSON
            var responseBody = "This is not JSON";

            // Act
            var ex = new ConnectorException("sharepoint", "GET /items", 500, responseBody);

            // Assert
            Assert.IsNull(ex.ErrorCode);
        }

        [TestMethod]
        public void Constructor_WithNullResponseBody_ErrorCodeIsNull()
        {
            // Act — null is coalesced to string.Empty
            var ex = new ConnectorException("office365", "POST /Mail", 500, null);

            // Assert
            Assert.IsNull(ex.ErrorCode);
            Assert.AreEqual(string.Empty, ex.ResponseBody);
        }

        [TestMethod]
        public void Constructor_WithEmptyResponseBody_ErrorCodeIsNull()
        {
            // Act
            var ex = new ConnectorException("office365", "POST /Mail", 500, string.Empty);

            // Assert
            Assert.IsNull(ex.ErrorCode);
        }

        [TestMethod]
        public void Constructor_WithNonStringCode_ErrorCodeIsNull()
        {
            // Arrange — "code" is a number, not a string
            var responseBody = """{"code":403,"message":"Forbidden"}""";

            // Act
            var ex = new ConnectorException("office365", "POST /Mail", 403, responseBody);

            // Assert
            Assert.IsNull(ex.ErrorCode);
        }

        [TestMethod]
        public void Constructor_IsCatchableAsRequestFailedException()
        {
            // Arrange
            var responseBody = """{"code":"NotFound","message":"Resource not found"}""";

            // Act & Assert — callers can catch with a single catch block
            try
            {
                throw new ConnectorException("sharepoint", "GET /items/123", 404, responseBody);
            }
            catch (RequestFailedException ex)
            {
                Assert.AreEqual("NotFound", ex.ErrorCode);
                Assert.AreEqual(404, ex.Status);
            }
        }

        [TestMethod]
        public void Constructor_LongResponseBody_TruncatesInMessage()
        {
            // Arrange — body longer than 2000 chars
            var responseBody = "{\"code\":\"InternalError\",\"message\":\"" + new string('x', 3000) + "\"}";

            // Act
            var ex = new ConnectorException("office365", "POST /Mail", 500, responseBody);

            // Assert — ErrorCode still parsed despite truncation
            Assert.AreEqual("InternalError", ex.ErrorCode);
            Assert.IsTrue(ex.Message.Contains("...[truncated]", System.StringComparison.Ordinal));
            // Full response body is preserved
            Assert.AreEqual(responseBody, ex.ResponseBody);
        }
    }
}
