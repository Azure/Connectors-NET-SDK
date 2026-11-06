//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System.Net.Http.Headers;

namespace Azure.Connectors.Sdk
{
    /// <summary>
    /// A buffered upload with a filename and media type.
    /// </summary>
    public sealed class MultipartFile
    {
        /// <summary>
        /// Creates a file part without encoding or taking ownership of its content.
        /// </summary>
        /// <param name="content">The unencoded file bytes.</param>
        /// <param name="fileName">The filename sent in the part's Content-Disposition header.</param>
        /// <param name="contentType">The file's media type.</param>
        public MultipartFile(
            byte[] content,
            string fileName = "blob",
            string contentType = "application/octet-stream")
        {
            ArgumentNullException.ThrowIfNull(content);
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
            ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
            if (fileName.Contains('\r') || fileName.Contains('\n'))
            {
                throw new ArgumentException(
                    message: "Parameter 'fileName' must be a single line.",
                    paramName: nameof(fileName));
            }

            MediaTypeHeaderValue.Parse(contentType);
            this.Content = content;
            this.FileName = fileName;
            this.ContentType = contentType;
        }

        /// <summary>Gets the unencoded file bytes.</summary>
        public byte[] Content { get; }

        /// <summary>Gets the filename sent with the file part.</summary>
        public string FileName { get; }

        /// <summary>Gets the file's media type.</summary>
        public string ContentType { get; }

        /// <summary>
        /// Wraps bytes with the default filename and media type.
        /// </summary>
        /// <param name="content">The unencoded file bytes.</param>
        public static implicit operator MultipartFile(byte[] content)
        {
            return new MultipartFile(content);
        }
    }
}
