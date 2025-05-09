﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace IronPython.Runtime.Operations {
    public static partial class ByteOps {
        internal static byte ToByteChecked(this int item) {
            try {
                return checked((byte)item);
            } catch (OverflowException) {
                throw PythonOps.ValueError("byte must be in range(0, 256)");
            }
        }

        internal static byte ToByteChecked(this BigInteger item) {
            try {
                return checked((byte)item);
            } catch (OverflowException) {
                throw PythonOps.ValueError("byte must be in range(0, 256)");
            }
        }

        internal static byte ToByteChecked(this double item) {
            try {
                return checked((byte)item);
            } catch (OverflowException) {
                throw PythonOps.ValueError("byte must be in range(0, 256)");
            }
        }

        internal static bool IsSign(this byte ch) {
            return ch == '+' || ch == '-';
        }

        internal static byte ToUpper(this byte p) {
            if (p >= 'a' && p <= 'z') {
                p -= ('a' - 'A');
            }
            return p;
        }

        internal static byte ToLower(this byte p) {
            if (p >= 'A' && p <= 'Z') {
                p += ('a' - 'A');
            }
            return p;
        }

        internal static bool IsLower(this byte p) {
            return p >= 'a' && p <= 'z';
        }

        internal static bool IsUpper(this byte p) {
            return p >= 'A' && p <= 'Z';
        }

        internal static bool IsDigit(this byte b) {
            return b >= '0' && b <= '9';
        }

        internal static bool IsLetter(this byte b) {
            return IsLower(b) || IsUpper(b);
        }

        internal static bool IsWhiteSpace(this byte b) {
            return b == ' ' ||
                    b == '\t' ||
                    b == '\n' ||
                    b == '\r' ||
                    b == '\f' ||
                    b == 11;
        }

        internal static void AppendJoin(object? value, int index, List<byte> byteList) {
            if (value is IList<byte> bytesValue) {
                byteList.AddRange(bytesValue);
            } else if (value is IBufferProtocol bp) {
                using var buf = bp.GetBufferNoThrow();
                if (buf is null) throw JoinSequenceError(value, index);
                byteList.AddRange(buf.AsReadOnlySpan().ToArray());
            }
            else {
                throw JoinSequenceError(value, index);
            }
        }

        internal static Exception JoinSequenceError(object? value, int index) {
            return PythonOps.TypeError("sequence item {0}: expected a bytes-like object, {1} found", index.ToString(), PythonOps.GetPythonTypeName(value));
        }

        internal static IList<byte> CoerceBytes(object? obj) {
            if (obj is IList<byte> ret) {
                return ret;
            }
            if (obj is IBufferProtocol bp) {
                using (IPythonBuffer buf = bp.GetBuffer()) {
                    return buf.AsReadOnlySpan().ToArray();
                }
            }
            throw PythonOps.TypeError("a bytes-like object is required, not '{0}'", PythonOps.GetPythonTypeName(obj));
        }

        internal static IList<byte> GetBytes(object? value, bool useHint, CodeContext? context = null) {
            switch (value) {
                case IList<byte> lob when !(lob is ListGenericWrapper<byte>):
                    return lob;
                case IBufferProtocol bp:
                    using (IPythonBuffer buf = bp.GetBuffer()) {
                        return buf.AsReadOnlySpan().ToArray();
                    }
                case ReadOnlyMemory<byte> rom:
                    return rom.ToArray();
                case Memory<byte> mem:
                    return mem.ToArray();
                default:
                    int len = 0;
                    if (useHint) PythonOps.TryInvokeLengthHint(context ?? DefaultContext.Default, value, out len);
                    List<byte> ret = new List<byte>(len);
                    IEnumerator ie = PythonOps.GetEnumerator(context ?? DefaultContext.Default, value);
                    while (ie.MoveNext()) {
                        ret.Add(GetByte(ie.Current));
                    }
                    return ret;
            }
        }

        internal static byte GetByte(object? o) {
            var index = PythonOps.Index(o);

            return index switch {
                int i => i.ToByteChecked(),
                BigInteger bi => bi.ToByteChecked(),
                _ => throw new InvalidOperationException() // unreachable
            };
        }
    }
}
