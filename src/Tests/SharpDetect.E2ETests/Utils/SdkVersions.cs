// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Xunit;

namespace SharpDetect.E2ETests.Utils;

internal static class SdkVersions
{
    private static readonly string[] AllSdks = ["net8.0", "net9.0", "net10.0"];

    public static TheoryData<string> All =>
    [
        "net8.0",
        "net9.0",
        "net10.0",
    ];

    public static TheoryData<string> Net9AndAbove =>
    [
        "net9.0",
        "net10.0",
    ];

    public static TheoryData<string> Net10Only =>
    [
        "net10.0",
    ];

    public static TheoryData<string, string> AllWithBothDataRacePlugins
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (var sdk in AllSdks)
            {
                data.Add(sdk, typeof(TestEraserPlugin).FullName!);
                data.Add(sdk, typeof(TestFastTrackPlugin).FullName!);
            }
            return data;
        }
    }

    public static TheoryData<string, string> AllWithFastTrackOnly
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (var sdk in AllSdks)
                data.Add(sdk, typeof(TestFastTrackPlugin).FullName!);
            return data;
        }
    }

    public static TheoryData<string, string> Net10WithBothDataRacePlugins
    {
        get
        {
            var data = new TheoryData<string, string>();
            data.Add("net10.0", typeof(TestEraserPlugin).FullName!);
            data.Add("net10.0", typeof(TestFastTrackPlugin).FullName!);
            return data;
        }
    }
}
