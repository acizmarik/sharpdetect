// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Profiler;

public class KnownGuids
{
    public static readonly Guid SharpDetectProfiler = Guid.Parse("79d2f672-5206-11eb-ae93-0242ac130002");

    public static readonly Guid IUnknown = Guid.Parse("00000000-0000-0000-C000-000000000046");
    public static readonly Guid ClassFactory = Guid.Parse("00000001-0000-0000-C000-000000000046");

    public static readonly Guid IMetaDataImport = Guid.Parse("7DAC8207-D3AE-4c75-9B67-92801A497D44");
    public static readonly Guid IMetaDataImport2 = Guid.Parse("FCE5EFA0-8BBA-4f8e-A036-8F2022B08466");
    public static readonly Guid IMetaDataEmit = Guid.Parse("BA3FEE4C-ECB9-4e41-83B7-183FA41CD859");
    public static readonly Guid IMetaDataEmit2 = Guid.Parse("F5DD9950-F693-42e6-830E-7B833E8146A9");

    public static readonly Guid IMetaDataAssemblyImport = Guid.Parse("EE62470B-E94B-424e-9B7C-2F00C9249F93");
    public static readonly Guid IMetaDataAssemblyEmit = Guid.Parse("211EF15B-5317-4438-B196-DEC87B887693");

    public static readonly Guid ICorProfilerCallback2 = Guid.Parse("8a8cc829-ccf2-49fe-bbae-0f022228071a");

    public static readonly Guid ICorProfilerInfo3 = Guid.Parse("B555ED4F-452A-4E54-8B39-B5360BAD32A0");
}
