// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.IO;

namespace IronPython.Runtime {
    internal class PythonBufferStream : Stream {
        private IPythonBuffer? _buffer;
        private readonly int _origin;
        private int _position;
        private readonly int _length;
        private readonly bool _writable;

        /// <summary>
        /// Create a stream object with the given buffer as the backing storage.
        /// The stream owns the buffer; the buffer is disposed when the stream closes.
        /// </summary>
        /// <param name="buffer">binary data of the stream</param>
        /// <param name="index">optional offset within the buffer where the stream starts</param>
        /// <param name="writable">whether write operations are supported; by default the stream is writable if the underlying buffer is writable</param>
        public PythonBufferStream(IPythonBuffer buffer, int index = 0, bool? writable = default) {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (!buffer.IsCContiguous()) throw new NotSupportedException("non-contiguous buffers cannot be used as a data for a stream");

            _buffer = buffer;
            _length = _buffer.NumBytes();

            if (index < 0 || index > _length)
                throw new ArgumentOutOfRangeException(nameof(index));
            _origin = _position = index;

            if (writable.GetValueOrDefault() && _buffer.IsReadOnly)
                throw new ArgumentException("cannot create a writable stream with a readonly buffer", nameof(writable));
            _writable = writable ?? !_buffer.IsReadOnly;
        }

        protected override void Dispose(bool disposing) {
            if (_buffer != null) {
                if (disposing) {
                    _buffer.Dispose();
                } else {
                    try { _buffer.Dispose(); } catch { }
                }
                _buffer = null; 
            }
            base.Dispose(disposing);
        }

        public override bool CanRead => _buffer != null;

        public override bool CanSeek => _buffer != null;

        public override bool CanWrite => _buffer != null && _writable;

        public override long Length {
            get {
                if (_buffer == null) throw new ObjectDisposedException(nameof(PythonBufferStream));
                return _length - _origin;
            }
        }

        public override long Position {
            get {
                if (_buffer == null) throw new ObjectDisposedException(nameof(PythonBufferStream));
                return _position - _origin;
            }
            set {
                if (_buffer == null) throw new ObjectDisposedException(nameof(PythonBufferStream));
                if (value < 0 || value > _length - _origin) throw new ArgumentOutOfRangeException(nameof(value));

                _position = _origin + (int)value;
            }
        }

        public override void Flush() { }

        public override void SetLength(long value) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) {
            if (_buffer == null) throw new ObjectDisposedException(nameof(PythonBufferStream));

            switch (origin) {
                case SeekOrigin.Begin:
                    if (offset < 0 || offset > _length - _origin)
                        throw new IOException("seek outside the stream");
                    _position = _origin + (int)offset;
                    break;

                case SeekOrigin.Current:
                    if (offset < _origin - _position || offset > _length - _position)
                        throw new IOException("seek outside the stream");
                    _position += (int)offset;
                    break;

                case SeekOrigin.End:
                    if (offset > 0 || offset < _origin - _length)
                        throw new IOException("seek outside the stream");
                    _position = _length + (int)offset;
                    break;

                default:
                    throw new ArgumentException(nameof(origin));
            }
            return _position; // rather than _position + _origin to match MemoryStream
        }

        public override int Read(byte[] dest, int offset, int count)
            => Read(dest.AsSpan(offset, count));

        public override void Write(byte[] src, int offset, int count)
            => Write(src.AsSpan(offset, count));

#if NETCOREAPP
        public override int Read(Span<byte> dest) {
#else
        public int Read(Span<byte> dest) {
#endif
            if (_buffer == null) throw new ObjectDisposedException(nameof(PythonBufferStream));

            int numRead = Math.Min(_length - _position, dest.Length);
            if (numRead <= 0) return 0;

            _buffer.AsReadOnlySpan().Slice(_position, numRead).CopyTo(dest);
            _position += numRead;

            return numRead;
        }

#if NETCOREAPP
        public override void Write(ReadOnlySpan<byte> src) {
#else
        public void Write(ReadOnlySpan<byte> src) {
#endif
            if (_buffer == null) throw new ObjectDisposedException(nameof(PythonBufferStream));
            if (!_writable) throw new NotSupportedException("stream is not writable");
            if (_length - _position < src.Length) throw new IOException("cannot write past buffer");

            src.CopyTo(_buffer.AsSpan().Slice(_position));
            _position += src.Length;
        }
    }
}
