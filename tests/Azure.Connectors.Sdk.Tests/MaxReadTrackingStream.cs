//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// A pass-through <see cref="Stream"/> that records the largest single read request it receives,
    /// used to verify the reader caps each read to the configured maximum body size.
    /// </summary>
    internal sealed class MaxReadTrackingStream : Stream
    {
        private readonly Stream _inner;

        public MaxReadTrackingStream(Stream inner)
        {
            this._inner = inner;
        }

        public int MaxRequestedCount { get; private set; }

        public override bool CanRead => this._inner.CanRead;

        public override bool CanSeek => this._inner.CanSeek;

        public override bool CanWrite => this._inner.CanWrite;

        public override long Length => this._inner.Length;

        public override long Position
        {
            get => this._inner.Position;
            set => this._inner.Position = value;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            this.MaxRequestedCount = Math.Max(this.MaxRequestedCount, buffer.Length);
            return this._inner.ReadAsync(buffer, cancellationToken);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            this.MaxRequestedCount = Math.Max(this.MaxRequestedCount, count);
            return this._inner.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            this.MaxRequestedCount = Math.Max(this.MaxRequestedCount, count);
            return this._inner.Read(buffer, offset, count);
        }

        public override void Flush() => this._inner.Flush();

        public override long Seek(long offset, SeekOrigin origin) => this._inner.Seek(offset, origin);

        public override void SetLength(long value) => this._inner.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => this._inner.Write(buffer, offset, count);

        protected override void Dispose(bool disposing)
        {
            // Transparent pass-through wrapper: the caller owns the inner stream (tests dispose it
            // via their own using statement), so do not dispose it here to avoid double-disposal.
            // Still let the base Stream implementation mark this wrapper as disposed.
            base.Dispose(disposing);
        }
    }
}
