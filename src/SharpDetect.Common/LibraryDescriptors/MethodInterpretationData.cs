// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Common.Runtime.Arguments;

namespace SharpDetect.Common.LibraryDescriptors
{
    public record MethodInterpretationData(MethodInterpretation Interpretation, MethodRewritingFlags Flags, CapturedParameterInfo[] CapturedParams, ResultChecker? Checker = null)
    {
        private bool isEmpty;

        public bool IsEmpty()
            => isEmpty;

        public static MethodInterpretationData CreateEmpty()
        {
            var data = new MethodInterpretationData(
                MethodInterpretation.Regular,
                MethodRewritingFlags.None,
                Array.Empty<CapturedParameterInfo>(),
                static (_, _) => true);
            data.isEmpty = true;
            return data;
        }
    }

    public record struct CapturedParameterInfo(ushort Index, ushort Size, bool IndirectLoad);
    public delegate bool ResultChecker(IValueOrObject? returnValue, (ushort Index, IValueOrObject Argument)[]? args);
}
