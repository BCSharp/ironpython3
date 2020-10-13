// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;

using Microsoft.Scripting;
using Microsoft.Scripting.Utils;

namespace IronPython.Runtime {
    /// <summary>
    /// Provides a StreamContentProvider for a stream of content backed by a bytes-like object.
    /// </summary>
    internal sealed class BufferProtocolContentProvider : TextContentProvider {
        private readonly PythonContext _context;
        private readonly IBufferProtocol _data;
        private readonly int _index;
        private readonly string _path;

        internal BufferProtocolContentProvider(PythonContext context, IBufferProtocol data, string path)
            : this(context, data, 0, path) { }

        internal BufferProtocolContentProvider(PythonContext context, IBufferProtocol data, int index, string path) {
            ContractUtils.RequiresNotNull(context, nameof(context));
            ContractUtils.RequiresNotNull(data, nameof(data));

            _context = context;
            _data = data;
            _index = index;
            _path = path;
        }

        public override SourceCodeReader GetReader() {
            return _context.GetSourceReader(new PythonBufferStream(_data.GetBuffer(), _index, writable: false), _context.DefaultEncoding, _path);
        }
    }
}
