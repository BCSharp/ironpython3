﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections;
using System.Reflection;

using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

using IronPython.Runtime;
using IronPython.Runtime.Operations;
using IronPython.Runtime.Types;

[assembly: PythonModule("_bisect", typeof(IronPython.Modules.PythonBisectModule))]

namespace IronPython.Modules {

    public class PythonBisectModule {
        public const string __doc__ = @"Bisection algorithms.

This module provides support for maintaining a list in sorted order without
having to sort the list after each insertion. For long lists of items with
expensive comparison operations, this can be an improvement over the more
common approach.
";

        #region Private Implementation Details

        private static int InternalBisectLeft(CodeContext/*!*/ context, PythonList list, object? item, int lo, int hi) {
            int mid;
            object? litem;

            if (lo < 0) {
                throw PythonOps.ValueError("lo must be non-negative");
            }

            if (hi == -1) {
                hi = list.Count;
            }
            IComparer comparer = context.LanguageContext.GetLtComparer(GetComparisonType(list));
            while (lo < hi) {
                mid = (int)(((long)lo + hi) / 2);
                litem = list[mid];
                if (comparer.Compare(litem, item) < 0)
                    lo = mid + 1;
                else
                    hi = mid;
            }
            return lo;
        }

        private static int InternalBisectLeft(CodeContext/*!*/ context, object? list, object? item, int lo, int hi) {
            int mid;
            object litem;

            if (lo < 0) {
                throw PythonOps.ValueError("lo must be non-negative");
            }

            if (hi == -1) {
                hi = PythonOps.Length(list);
            }

            IComparer comparer = context.LanguageContext.GetLtComparer(GetComparisonType(context, list));
            while (lo < hi) {
                mid = (int)(((long)lo + hi) / 2);
                litem = PythonOps.GetIndex(context, list, mid);
                if (comparer.Compare(litem, item) < 0)
                    lo = mid + 1;
                else
                    hi = mid;
            }
            return lo;
        }

        private static int InternalBisectRight(CodeContext/*!*/ context, PythonList list, object? item, int lo, int hi) {
            object? litem;
            int mid;

            if (lo < 0) {
                throw PythonOps.ValueError("lo must be non-negative");
            }

            if (hi == -1) {
                hi = list.Count;
            }
            IComparer comparer = context.LanguageContext.GetLtComparer(GetComparisonType(list));
            while (lo < hi) {
                mid = (int)(((long)lo + hi) / 2);
                litem = list[mid];
                if (comparer.Compare(item, litem) < 0)
                    hi = mid;
                else
                    lo = mid + 1;
            }
            return lo;
        }

        private static int InternalBisectRight(CodeContext/*!*/ context, object list, object? item, int lo, int hi) {
            object litem;
            int mid;

            if (lo < 0) {
                throw PythonOps.ValueError("lo must be non-negative");
            }

            if (hi == -1) {
                hi = PythonOps.Length(list);
            }

            IComparer comparer = context.LanguageContext.GetLtComparer(GetComparisonType(context, list));
            while (lo < hi) {
                mid = (int)(((long)lo + hi) / 2);
                litem = PythonOps.GetIndex(context, list, mid);
                if (comparer.Compare(item, litem) < 0)
                    hi = mid;
                else
                    lo = mid + 1;
            }
            return lo;
        }

        private static Type GetComparisonType(CodeContext/*!*/ context, object? a) {
            if (PythonOps.Length(a) > 0) {
                // use the 1st index to determine the type - we're assuming lists are
                // homogeneous
                return CompilerHelpers.GetType(PythonOps.GetIndex(context, a, 0));
            }
            return typeof(object);
        }

        private static Type GetComparisonType(PythonList a) {
            if (a.Count > 0) {
                // use the 1st index to determine the type - we're assuming lists are
                // homogeneous
                return CompilerHelpers.GetType(a[0]);
            }
            return typeof(object);
        }

        #endregion Private Implementation Details

        #region Public API Surface

        [PythonHidden]
        [Documentation(@"bisect_right(a, x[, lo[, hi]]) -> index

Return the index where to insert item x in list a, assuming a is sorted.

The return value i is such that all e in a[:i] have e <= x, and all e in
a[i:] have e > x.  So if x already appears in the list, i points just
beyond the rightmost x already there

Optional args lo (default 0) and hi (default len(a)) bound the
slice of a to be searched.
")]
        public static object BisectRight(CodeContext/*!*/ context, object a, object x, int lo = 0, int hi = -1) {
            if (a is PythonList l && l.GetType() == typeof(PythonList)) {
                return InternalBisectRight(context, l, x, lo, hi);
            }

            return InternalBisectRight(context, a, x, lo, hi);
        }

        public static object bisect_right = BuiltinFunction.MakeFunction(
            nameof(bisect_right),
            ArrayUtils.ConvertAll(typeof(PythonBisectModule).GetMember(nameof(BisectRight)), x => (MethodBase)x),
            typeof(PythonBisectModule)
        );

        public static object bisect = bisect_right;

        [Documentation(@"bisect_left(a, x[, lo[, hi]]) -> index

Return the index where to insert item x in list a, assuming a is sorted.

The return value i is such that all e in a[:i] have e < x, and all e in
a[i:] have e >= x.  So if x already appears in the list, i points just
before the leftmost x already there.

Optional args lo (default 0) and hi (default len(a)) bound the
slice of a to be searched.
")]
        public static object bisect_left(CodeContext/*!*/ context, object? a, object? x, int lo = 0, int hi = -1) {
            if (a is PythonList l && l.GetType() == typeof(PythonList)) {
                return InternalBisectLeft(context, l, x, lo, hi);
            }

            return InternalBisectLeft(context, a, x, lo, hi);
        }

        [PythonHidden]
        [Documentation(@"insort_right(a, x[, lo[, hi]])

Insert item x in list a, and keep it sorted assuming a is sorted.

If x is already in a, insert it to the right of the rightmost x.

Optional args lo (default 0) and hi (default len(a)) bound the
slice of a to be searched.
")]
        public static void InsortRight(CodeContext/*!*/ context, object a, object x, int lo = 0, int hi = -1) {
            if (a is PythonList l && l.GetType() == typeof(PythonList)) {
                l.Insert(InternalBisectRight(context, l, x, lo, hi), x);
                return;
            }

            PythonOps.Invoke(context, a, "insert", InternalBisectRight(context, a, x, lo, hi), x);
        }

        public static object insort_right = BuiltinFunction.MakeFunction(
            nameof(insort_right),
            ArrayUtils.ConvertAll(typeof(PythonBisectModule).GetMember(nameof(InsortRight)), x => (MethodBase)x),
            typeof(PythonBisectModule)
        );

        public static object insort = insort_right;

        [Documentation(@"insort_left(a, x[, lo[, hi]])

Insert item x in list a, and keep it sorted assuming a is sorted.

If x is already in a, insert it to the left of the leftmost x.

Optional args lo (default 0) and hi (default len(a)) bound the
slice of a to be searched.
")]
        public static void insort_left(CodeContext/*!*/ context, object? a, object? x, int lo = 0, int hi = -1) {
            if (a is PythonList l && l.GetType() == typeof(PythonList)) {
                l.Insert(InternalBisectLeft(context, l, x, lo, hi), x);
                return;
            }

            PythonOps.Invoke(context, a, "insert", InternalBisectLeft(context, a, x, lo, hi), x);
        }

        #endregion Public API Surface
    }
}
